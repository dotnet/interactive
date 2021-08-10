// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Json;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter
{
    public static class Notebook
    {
        public const string MetadataNamespace = "dotnet_interactive";
        private static readonly JsonSerializerOptions _serializerOptions;

        public static Encoding Encoding => new UTF8Encoding(false);

        static Notebook()
        {
            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
            _serializerOptions.Converters.Add(new DataDictionaryConverter());
        }

        public static InteractiveDocument Parse(string content, IDictionary<string, string> kernelLanguageAliases)
        {
            if (kernelLanguageAliases == null)
            {
                throw new ArgumentNullException(nameof(kernelLanguageAliases));
            }

            var jupyter = JsonDocument.Parse(content).RootElement;
            var notebookLanguage = jupyter.GetPropertyFromPath("metadata", "kernelspec", "language")?.GetString() ?? "C#";
            var defaultLanguage = notebookLanguage switch
            {
                "C#" => "csharp",
                "F#" => "fsharp",
                "PowerShell" => "pwsh",
                _ => "csharp"
            };

            var cells = new List<InteractiveDocumentElement>();
            foreach (var cell in jupyter.GetPropertyFromPath("cells").EnumerateArray())
            {
                switch (cell.GetPropertyFromPath("cell_type").GetString())
                {
                    case "code":
                        //
                        // figure out cell language and content
                        //
                        var cellMetadata = cell.GetPropertyFromPath("metadata", MetadataNamespace);

                        var languageFromMetadata = cellMetadata?.GetPropertyFromPath("language").GetString();

                        var sourceLines = GetTextLines(cell.GetPropertyFromPath("source"));

                        var possibleCellLanguage = sourceLines.Count > 0 && sourceLines[0].StartsWith("#!")
                            ? sourceLines[0].Substring(2)
                            : null;

                        var (cellLanguage, cellSourceLines) = possibleCellLanguage is not null && kernelLanguageAliases.TryGetValue(possibleCellLanguage, out var actualCellLanguage)
                            ? (actualCellLanguage, sourceLines.Skip(1))
                            : (languageFromMetadata ?? defaultLanguage, sourceLines);

                        var source = string.Join("\n", cellSourceLines); // normalize all line endings to `\n`

                        //
                        // gather cell outputs
                        //

                        var outputs = Array.Empty<InteractiveDocumentOutputElement>();
                        if (cell.TryGetProperty("outputs", out var cellOutputs))
                        {
                            outputs = cellOutputs.EnumerateArray()
                                .Select<JsonElement, InteractiveDocumentOutputElement>(cellOutput =>
                                {
                                    if (cellOutput.TryGetProperty("output_type", out var cellTypeProperty))
                                    {
                                        return cellTypeProperty.GetString() switch
                                        {
                                            // our concept of a interactive is heavily influenced by VS Code and they don't distinguish between execution results and displayed data
                                            "display_data" or "execute_result" =>
                                                new InteractiveDocumentDisplayOutputElement(
                                                    JsonSerializer.Deserialize<IDictionary<string, object>>(
                                                        cellOutput.GetPropertyFromPath("data").GetRawText(), _serializerOptions)),

                                            "stream" =>
                                                new InteractiveDocumentTextOutputElement(
                                                    GetTextAsSingleString(cellOutput.GetPropertyFromPath("text"))),

                                            "error" =>
                                                new InteractiveDocumentErrorOutputElement(
                                                    cellOutput.GetPropertyFromPath("ename").GetString(),
                                                    cellOutput.GetPropertyFromPath("evalue").GetString(),
                                                    cellOutput.GetPropertyFromPath("traceback").EnumerateArray()
                                                        .Select(s => s.GetString()).ToArray()),

                                            _ => null
                                        };
                                    }

                                    return null;
                                })
                                .Where(x => x is not null)
                                .ToArray();
                        }
                        cells.Add(new InteractiveDocumentElement(cellLanguage, source, outputs));
                        break;
                    case "markdown":
                        var markdown = GetTextAsSingleString(cell.GetPropertyFromPath("source"));
                        cells.Add(new InteractiveDocumentElement("markdown", markdown));
                        break;
                }
            }

            return new InteractiveDocument(cells.ToArray());
        }

        public static InteractiveDocument Read(Stream stream, 
            IDictionary<string, string> kernelLanguageAliases)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = reader.ReadToEnd();
            return Parse(content, kernelLanguageAliases);
        }

        public static async Task<InteractiveDocument> ReadAsync(Stream stream,
            IDictionary<string, string> kernelLanguageAliases)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = await reader.ReadToEndAsync();
            return Parse(content, kernelLanguageAliases);
        }

        private static List<string> GetTextLines(JsonElement? jsonElement)
        {
            var textLines = jsonElement?.ValueKind switch
            {
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(element => element.GetString().TrimNewline()),
                JsonValueKind.String => StringExtensions.SplitAsLines(jsonElement.GetString()),
                _ => Array.Empty<string>()
            };

            return textLines.ToList();
        }

        private static string GetTextAsSingleString(JsonElement? jsonElement)
        {
            var textLines = GetTextLines(jsonElement);
            return string.Join("\n", textLines);
        }

        public static void Write(Documents.InteractiveDocument interactive, string newline, Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding, 1024, true);
            Write(interactive, newline, writer);
            writer.Flush();
        }

        public static string ToIpynbContent(this Documents.InteractiveDocument interactive, string newline = "\n")
        {
            var cells = new List<object>();
            foreach (var element in interactive.Elements)
            {
                switch (element.Language)
                {
                    case "markdown":
                        cells.Add(new
                        {
                            cell_type = "markdown",
                            metadata = new { },
                            source = AddTrailingNewlinesToAllButLast(StringExtensions.SplitAsLines(element.Contents))
                        });
                        break;
                    default:
                        var outputs = element.Outputs.Select<InteractiveDocumentOutputElement, object>(o => o switch
                        {
                            InteractiveDocumentDisplayOutputElement displayOutput => new
                            {
                                output_type = "execute_result",
                                data = displayOutput.Data,
                                execution_count = 1,
                                metadata = new { }
                            },
                            InteractiveDocumentErrorOutputElement errorOutput => new
                            {
                                output_type = "error",
                                ename = errorOutput.ErrorName,
                                evalue = errorOutput.ErrorValue,
                                traceback = errorOutput.StackTrace
                            },
                            InteractiveDocumentTextOutputElement textOutput => new
                            {
                                output_type = "stream",
                                name = "stdout", // n.b., could also be `stderr`, but our representation (and VS Code) don't differentiate the two
                                text = textOutput.Text,
                            },
                            _ => null
                        }).Where(x => x is not null);
                        cells.Add(new
                        {
                            cell_type = "code",
                            execution_count = 1,
                            metadata = new
                            {
                                dotnet_interactive = new
                                {
                                    language = element.Language
                                }
                            },
                            source = AddTrailingNewlinesToAllButLast(StringExtensions.SplitAsLines(element.Contents)),
                            outputs
                        });
                        break;
                }
            }

            var jupyter = new
            {
                cells,
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = ".NET (C#)",
                        language = "C#",
                        name = ".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = "text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };

            // use single space indention as is common with .ipynb

            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };

            var content = JsonSerializer.Serialize(jupyter, options);
            return content;
        }

        public static void Write(Documents.InteractiveDocument interactive, string newline, TextWriter writer)
        {
            var content = interactive.ToIpynbContent(newline);
            writer.Write(content);
        }

        /// <summary>
        /// Ensures each line _except the last_ ends with '\n'.
        /// </summary>
        private static IEnumerable<string> AddTrailingNewlinesToAllButLast(IEnumerable<string> lines)
        {
            var result = lines.Select(l => l.EndsWith("\n") ? l : l + "\n").ToList();
            if (result.Count > 0)
            {
                result[^1] = lines.Last();
            }

            return result;
        }
    }
}
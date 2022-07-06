// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents.ParserServer;

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

        public static InteractiveDocument Parse(string content, KernelNameCollection kernelNames)
        {
            if (kernelNames is null)
            {
                throw new ArgumentNullException(nameof(kernelNames));
            }

            var kernelAliasesToNameMap = kernelNames.ToMapOfKernelNamesByAlias();

            var jupyter = JsonDocument.Parse(content).RootElement;
            var notebookLanguage = jupyter.GetPropertyFromPath("metadata", "kernelspec", "language")?.GetString();

            var defaultTargetKernelName = notebookLanguage switch
            {
                "C#" => "csharp",
                "F#" => "fsharp",
                "PowerShell" => "pwsh",
                _ => kernelNames.DefaultKernelName ?? "csharp"
            };

            var cells = new List<InteractiveDocumentElement>();

            foreach (var cell in jupyter.GetPropertyFromPath("cells").EnumerateArray())
            {
                InteractiveDocumentElement? newCell = new();

                var cell_type = cell.GetPropertyFromPath("cell_type").GetString();

                var id = cell.GetPropertyFromPath("id")?.GetString();

                switch (cell_type)
                {
                    case "code":
                        //
                        // figure out cell language and content
                        //
                        var cellMetadata = cell.GetPropertyFromPath("metadata", MetadataNamespace);

                        var executionCount = cell.GetPropertyFromPath("execution_count").GetValueOrDefault() switch
                        {
                            {ValueKind:JsonValueKind.Number} n => n.GetInt32(),
                            _ => 0
                        };
                        
                        var languageFromMetadata = cellMetadata?.GetPropertyFromPath("language").GetString();

                        var sourceLines = GetTextLines(cell.GetPropertyFromPath("source"));

                        var possibleTargetKernelName = sourceLines.Length > 0 && sourceLines[0].StartsWith("#!")
                                                       ? sourceLines[0].Substring(2)
                                                       : null;

                        var (cellTargetKernelName, cellSourceLines) =
                            possibleTargetKernelName is not null &&
                            kernelAliasesToNameMap.TryGetValue(possibleTargetKernelName, out var targetKernelName)
                                ? (targetKernelName, sourceLines.Skip(1))
                                : (languageFromMetadata ?? defaultTargetKernelName, sourceLines);

                        var source = string.Join("\n", cellSourceLines); // normalize all line endings to `\n`

                        //
                        // gather cell outputs
                        //

                        if (cell.TryGetProperty("outputs", out var cellOutputs))
                        {
                            foreach (var outputElement in cellOutputs
                                                          .EnumerateArray()
                                                          .Select(DeserializeOutputElement)
                                                          .ToArray())
                            {
                                if (outputElement is { })
                                {
                                    newCell.Outputs.Add(outputElement);
                                }
                                else
                                {
                                    // FIX: (Parse) is this a thing?
                                }
                            }
                        }
                        
                        newCell.Language = cellTargetKernelName;
                        newCell.Contents = source;
                        newCell.ExecutionCount = executionCount;

                        InteractiveDocumentOutputElement? DeserializeOutputElement(JsonElement cellOutput)
                        {
                            if (cellOutput.TryGetProperty("output_type", out var cellTypeProperty))
                            {
                                return cellTypeProperty.GetString() switch
                                {
                                    "display_data" =>
                                        new DisplayElement(JsonSerializer.Deserialize<IDictionary<string, object>>(
                                                               cellOutput.GetPropertyFromPath("data").GetRawText(), _serializerOptions)),

                                    "execute_result" =>
                                        new ReturnValueElement(JsonSerializer.Deserialize<IDictionary<string, object>>(
                                                                   cellOutput.GetPropertyFromPath("data").GetRawText(), _serializerOptions)),

                                    "stream" =>
                                        new TextElement(
                                            GetTextAsSingleString(cellOutput.GetPropertyFromPath("text"))),

                                    "error" =>
                                        new ErrorElement(cellOutput.GetPropertyFromPath("evalue").GetString(),
                                                         cellOutput.GetPropertyFromPath("ename").GetString(), cellOutput.GetPropertyFromPath("traceback")
                                                             .EnumerateArray()
                                                             .Select(s => s.GetString() ?? "").ToArray()),

                                    _ => null
                                };
                            }

                            return null;
                        }

                        break;

                    case "markdown":
                        
                        var markdown = GetTextAsSingleString(cell.GetPropertyFromPath("source"));

                        newCell = new InteractiveDocumentElement("markdown", markdown);

                        break;

                    default:
                        throw new ArgumentException($"Unrecognized cell_type: {cell_type}");
                }

                if (id is {} )
                {
                    newCell.Id = id;
                }

                cells.Add(newCell);
            }

            return new InteractiveDocument(cells);

        }

        public static InteractiveDocument Read(
            Stream stream,
            KernelNameCollection kernelNames)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = reader.ReadToEnd();
            return Parse(content, kernelNames);
        }

        public static async Task<InteractiveDocument> ReadAsync(
            Stream stream,
            KernelNameCollection kernelNames)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = await reader.ReadToEndAsync();
            return Parse(content, kernelNames);
        }

        private static string[] GetTextLines(JsonElement? jsonElement)
        {
            var textLines = jsonElement?.ValueKind switch
            {
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(element => element.GetString()?.TrimNewline()).ToArray(),
                JsonValueKind.String => jsonElement.GetString()?.SplitIntoLines(),
                _ => null
            } ?? Array.Empty<string>();

            return textLines!;
        }

        private static string GetTextAsSingleString(JsonElement? jsonElement)
        {
            var textLines = GetTextLines(jsonElement);
            return string.Join("\n", textLines);
        }

        public static void Write(InteractiveDocument document, Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding, 1024, true);
            Write(document, writer);
            writer.Flush();
        }

        public static string ToJupyterNotebookContent(this InteractiveDocument document)
        {
            var cells = new List<object>();
            foreach (var element in document.Elements)
            {
                object? cell = null;

                switch (element.Language)
                {
                    case "markdown":
                        cell = new
                        {
                            cell_type = "markdown",
                            metadata = new { },
                            source = AddTrailingNewlinesToAllButLast(element.Contents.SplitIntoLines())
                        };
                        break;

                    default:
                        cell = new
                        {
                            cell_type = "code",
                            execution_count = element.ExecutionCount,
                            metadata = new
                            {
                                dotnet_interactive = new
                                {
                                    language = element.Language
                                }
                            },
                            outputs = element.Outputs,
                            source = AddTrailingNewlinesToAllButLast(element.Contents.SplitIntoLines())
                        };
                        break;
                }

                cells.Add(cell);
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
            
            var options = new JsonSerializerOptions(ParserServerSerializer.JsonSerializerOptions)
            {
                WriteIndented = true
            };

            var content = JsonSerializer.Serialize(jupyter, options);

            return content;
        }

        public static void Write(InteractiveDocument document, TextWriter writer)
        {
            var content = document.ToJupyterNotebookContent();
            writer.Write(content);
        }

        /// <summary>
        /// Ensures each line _except the last_ ends with '\n'.
        /// </summary>
        private static IEnumerable<string> AddTrailingNewlinesToAllButLast(string[] lines)
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
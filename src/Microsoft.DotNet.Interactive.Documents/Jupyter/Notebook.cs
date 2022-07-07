// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public static Encoding Encoding => new UTF8Encoding(false);

        public static InteractiveDocument Parse(
            string content,
            KernelNameCollection? kernelNames = null)
        {
            var document = new InteractiveDocument();

            var notebook = JsonDocument.Parse(content).RootElement;

            var elements = notebook.GetProperty("cells").Deserialize<InteractiveDocumentElement[]>(ParserServerSerializer.JsonSerializerOptions);

            if (elements is { })
            {
                document.Elements = elements;
            }

            if (notebook.TryGetProperty("metadata", out var metadataJson) && metadataJson.Deserialize<IDictionary<string, object>>(ParserServerSerializer.JsonSerializerOptions) is
                    { } metadataDict)
            {
                document.Metadata = metadataDict;
            }

            if (kernelNames is { })
            {
                document.NormalizeElementLanguages(kernelNames);
            }

            return document;
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
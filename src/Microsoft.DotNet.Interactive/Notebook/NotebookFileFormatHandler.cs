// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public static class NotebookFileFormatHandler
    {
        private const string InteractiveNotebookCellSpecifier = "#!";

        public static NotebookDocument Parse(string fileName, byte[] rawData, string defaultLanguage, IDictionary<string, string> kernelLanguageAliases)
        {
            var extension = Path.GetExtension(fileName);
            switch (extension.ToLowerInvariant())
            {
                case ".dib":
                case ".dotnet-interactive":
                    return ParseInteractiveNotebook(rawData, defaultLanguage, kernelLanguageAliases);
                case ".ipynb":
                    return ParseJupyterNotebook(rawData, kernelLanguageAliases);
                default:
                    throw new NotSupportedException($"Unable to parse a notebook document of type '{extension}'");
            }
        }

        private static NotebookDocument ParseInteractiveNotebook(byte[] rawData, string defaultLanguage, IDictionary<string, string> kernelLanguageAliases)
        {
            // `markdown` and its alias `md` aren't cell languages that the kernel is aware of, but nevertheless something that needs to be parsed like it is
            var allLanguageAliases = ImmutableDictionary<string, string>.Empty
                .AddRange(kernelLanguageAliases)
                .Add("markdown", "markdown")
                .Add("md", "markdown");

            var content = Encoding.UTF8.GetString(rawData);
            var lines = NotebookParsingExtensions.SplitAsLines(content);

            var cells = new List<NotebookCell>();
            var currentLanguage = defaultLanguage;
            var currentCellLines = new List<string>();

            NotebookCell CreateCell(string cellLanguage, IEnumerable<string> cellLines)
            {
                return new NotebookCell(cellLanguage, string.Join("\n", cellLines));
            }

            void AddCell()
            {
                // trim leading blank lines
                while (currentCellLines.Count > 0 && string.IsNullOrEmpty(currentCellLines[0]))
                {
                    currentCellLines.RemoveAt(0);
                }

                // trim trailing blank lines
                while (currentCellLines.Count > 0 && string.IsNullOrEmpty(currentCellLines[^1]))
                {
                    currentCellLines.RemoveAt(currentCellLines.Count - 1);
                }

                if (currentCellLines.Count > 0)
                {
                    cells.Add(CreateCell(currentLanguage, currentCellLines));
                }
            }

            foreach (var line in lines)
            {
                if (line.StartsWith(InteractiveNotebookCellSpecifier))
                {
                    var cellLanguage = line.Substring(InteractiveNotebookCellSpecifier.Length);
                    if (allLanguageAliases.TryGetValue(cellLanguage, out cellLanguage))
                    {
                        // recognized language, finalize the current cell
                        AddCell();

                        // start a new cell
                        currentLanguage = cellLanguage;
                        currentCellLines.Clear();
                    }
                    else
                    {
                        // unrecognized language, probably a magic command
                        currentCellLines.Add(line);
                    }
                }
                else
                {
                    currentCellLines.Add(line);
                }
            }

            // finalize last cell
            AddCell();

            // ensure there's at least one cell available
            if (cells.Count == 0)
            {
                cells.Add(CreateCell(defaultLanguage, Array.Empty<string>()));
            }

            return new NotebookDocument(cells);
        }

        private static NotebookDocument ParseJupyterNotebook(byte[] rawData, IDictionary<string, string> kernelLanguageAliases)
        {
            var content = Encoding.UTF8.GetString(rawData);
            var jupyter = JObject.Parse(content);
            var notebookLanguage = jupyter["metadata"]?["kernelspec"]?["language"]?.ToObject<string>() ?? "C#";
            var defaultLanguage = notebookLanguage switch
            {
                "C#" => "csharp",
                "F#" => "fsharp",
                "PowerShell" => "pwsh",
                _ => "csharp"
            };

            var cells = new List<NotebookCell>();
            foreach (var cell in jupyter["cells"])
            {
                switch (cell["cell_type"]?.ToObject<string>())
                {
                    case "code":
                        //
                        // figure out cell language and content
                        //
                        var sourceLines = GetTextLines(cell["source"]);

                        var possibleCellLanguage = sourceLines.Count > 0 && sourceLines[0].StartsWith("#!")
                            ? sourceLines[0].Substring(2)
                            : null;

                        var (cellLanguage, cellSourceLines) = possibleCellLanguage != null && kernelLanguageAliases.TryGetValue(possibleCellLanguage, out var actualCellLanguage)
                            ? (actualCellLanguage, sourceLines.Skip(1))
                            : (defaultLanguage, sourceLines);

                        var source = string.Join("\n", cellSourceLines); // normalize all line endings to `\n`

                        //
                        // gather cell outputs
                        //
                        var outputs = (cell["outputs"] ?? new JArray()) // might be `null`
                            .Select<JToken, NotebookCellOutput>(cellOutput =>
                                cellOutput["output_type"]?.ToObject<string>() switch
                                {
                                    // our concept of a notebook is heavily influenced by VS Code and they don't distinguish between execution results and displayed data
                                    var type when
                                        type == "display_data" ||
                                        type == "execute_result" => new NotebookCellDisplayOutput(cellOutput["data"]?.ToObject<IDictionary<string, object>>()),

                                    "stream" => new NotebookCellTextOutput(GetTextAsSingleString(cellOutput["text"])),

                                    "error" => new NotebookCellErrorOutput(
                                        cellOutput["ename"]?.ToObject<string>(),
                                        cellOutput["evalue"]?.ToObject<string>(),
                                        cellOutput["traceback"]?.ToObject<string[]>()),

                                    _ => null
                                })
                            .Where(x => x != null);

                        cells.Add(new NotebookCell(cellLanguage, source, outputs));
                        break;
                    case "markdown":
                        var markdown = GetTextAsSingleString(cell["source"]);
                        cells.Add(new NotebookCell("markdown", markdown));
                        break;
                }
            }

            return new NotebookDocument(cells);
        }

        private static List<string> GetTextLines(JToken token)
        {
            var textLines = (token switch
            {
                // can either be a string or an array of string
                JArray arr => arr.Select(l => l.ToObject<string>().TrimNewline()),
                JValue val when val.Type == JTokenType.String => NotebookParsingExtensions.SplitAsLines(val.ToObject<string>()),
                _ => Array.Empty<string>() // null/unknown/unsupported; don't crash
            }).ToList();
            return textLines;
        }

        private static string GetTextAsSingleString(JToken token)
        {
            var textLines = GetTextLines(token);
            return string.Join("\n", textLines);
        }

        public static byte[] Serialize(string fileName, NotebookDocument notebook, string newline)
        {
            var extension = Path.GetExtension(fileName);
            switch (extension.ToLowerInvariant())
            {
                case ".dib":
                case ".dotnet-interactive":
                    return SerializeInteractiveNotebook(notebook, newline);
                case ".ipynb":
                    return SerializeJupyterNotebook(notebook);
                default:
                    throw new NotSupportedException($"Unable to serialize a notebook document of type '{extension}'");
            }
        }

        private static byte[] SerializeInteractiveNotebook(NotebookDocument notebook, string newline)
        {
            var lines = new List<string>();

            foreach (var cell in notebook.Cells)
            {
                var cellLines = NotebookParsingExtensions.SplitAsLines(cell.Contents).SkipWhile(l => l.Length == 0).ToList();
                while (cellLines.Count > 0 && cellLines[^1].Length == 0)
                {
                    cellLines.RemoveAt(cellLines.Count - 1);
                }

                if (cellLines.Count > 0)
                {
                    lines.Add($"{InteractiveNotebookCellSpecifier}{cell.Language}");
                    lines.Add("");
                    lines.AddRange(cellLines);
                    lines.Add("");
                }
            }

            var content = string.Join(newline, lines);
            var rawData = Encoding.UTF8.GetBytes(content);
            return rawData;
        }

        private static byte[] SerializeJupyterNotebook(NotebookDocument notebook)
        {
            var cells = new List<object>();
            foreach (var cell in notebook.Cells)
            {
                switch (cell.Language)
                {
                    case "markdown":
                        cells.Add(new
                        {
                            cell_type = "markdown",
                            metadata = new { },
                            source = AddTrailingNewlinesToAllButLast(NotebookParsingExtensions.SplitAsLines(cell.Contents))
                        });
                        break;
                    default:
                        var cellHeader = cell.Language == "csharp" ? "" : $"{InteractiveNotebookCellSpecifier}{cell.Language}\n";
                        var outputs = cell.Outputs.Select<NotebookCellOutput, object>(o => o switch
                            {
                                NotebookCellDisplayOutput displayOutput => new
                                {
                                    output_type = "execute_result",
                                    data = displayOutput.Data,
                                    execution_count = 1,
                                    metadata = new { }
                                },
                                NotebookCellErrorOutput errorOutput => new
                                {
                                    output_type = "error",
                                    ename = errorOutput.ErrorName,
                                    evalue = errorOutput.ErrorValue,
                                    traceback = errorOutput.StackTrace
                                },
                                NotebookCellTextOutput textOutput => new
                                {
                                    output_type = "stream",
                                    name = "stdout", // n.b., could also be `stderr`, but our representation (and VS Code) don't differentiate the two
                                    text = textOutput.Text,
                                },
                                _ => null
                            }).Where(x => x != null);
                        cells.Add(new
                        {
                            cell_type = "code",
                            execution_count = 1,
                            metadata = new { },
                            source = AddTrailingNewlinesToAllButLast(NotebookParsingExtensions.SplitAsLines(cellHeader + cell.Contents)),
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
            var serializer = JsonSerializer.CreateDefault();
            var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter)
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                IndentChar = ' ',
                Indentation = 1
            };
            serializer.Serialize(jsonWriter, jupyter);
            var content = stringWriter.ToString();

            var rawData = Encoding.UTF8.GetBytes(content);
            return rawData;
        }

        /// <summary>
        /// Ensures each line _except the last_ ends with '\r\n'.
        /// </summary>
        private static IEnumerable<string> AddTrailingNewlinesToAllButLast(IEnumerable<string> lines)
        {
            var result = lines.Select(l => l.EndsWith("\r\n") ? l : l + "\r\n").ToList();
            if (result.Count > 0)
            {
                result[^1] = lines.Last();
            }

            return result;
        }
    }
}

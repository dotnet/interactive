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
using Microsoft.DotNet.Interactive.Documents.Utility;

namespace Microsoft.DotNet.Interactive.Documents
{
    public static class CodeSubmission
    {
        private const string InteractiveNotebookCellSpecifier = "#!";

        public static Encoding Encoding => new UTF8Encoding(false);

        public static InteractiveDocument Parse(
            string content,
            KernelInfoCollection? kernelInfo = default)
        {
            kernelInfo ??= new();
            Dictionary<string, object>? metadata = null;
            var lines = content.SplitIntoLines();

            var document = new InteractiveDocument();
            var currentLanguage = kernelInfo.DefaultKernelName ?? "csharp";
            var currentElementLines = new List<string>();

            // not a kernel language, but still a valid cell splitter
            if (!kernelInfo.Contains("markdown"))
            {
                kernelInfo = kernelInfo.Clone();
                kernelInfo.Add(new KernelInfo("markdown", new[] { "md" }));
            }

            var foundMetadata = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (!foundMetadata &&
                    line.StartsWith("#!meta"))
                {
                    foundMetadata = true;
                    var sb = new StringBuilder();

                    while (!(line = lines[++i]).StartsWith("#!"))
                    {
                        sb.AppendLine(line);
                    }

                    var metadataString = sb.ToString();

                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataString, ParserServerSerializer.JsonSerializerOptions);

                    if (InteractiveDocument.TryGetKernelInfoFromMetadata(metadata, out var kernelInfoFromMetadata))
                    {
                        kernelInfo.AddRange(kernelInfoFromMetadata);
                    }
                }

                if (line.StartsWith(InteractiveNotebookCellSpecifier))
                {
                    var cellLanguage = line.Substring(InteractiveNotebookCellSpecifier.Length);

                    if (kernelInfo.TryGetByAlias(cellLanguage, out var name))
                    {
                        // recognized language, finalize the current element
                        AddElement();

                        // start a new element
                        currentLanguage = name.Name;
                        currentElementLines.Clear();
                    }
                    else
                    {
                        // unrecognized language, probably a magic command
                        currentElementLines.Add(line);
                    }
                }
                else
                {
                    currentElementLines.Add(line);
                }
            }

            // finalize last element
            AddElement();

            // ensure there's at least one element available
            if (document.Elements.Count == 0)
            {
                document.Elements.Add(CreateElement(currentLanguage, Array.Empty<string>()));
            }

            InteractiveDocumentElement CreateElement(string elementLanguage, IEnumerable<string> elementLines)
            {
                return new(string.Join("\n", elementLines), elementLanguage);
            }

            if (metadata is not null)
            {
                document.Metadata.MergeWith(metadata);
            }

            return document;

            void AddElement()
            {
                // trim leading blank lines
                while (currentElementLines.Count > 0 && string.IsNullOrEmpty(currentElementLines[0]))
                {
                    currentElementLines.RemoveAt(0);
                }

                // trim trailing blank lines
                while (currentElementLines.Count > 0 && string.IsNullOrEmpty(currentElementLines[^1]))
                {
                    currentElementLines.RemoveAt(currentElementLines.Count - 1);
                }

                if (currentElementLines.Count > 0)
                {
                    document.Elements.Add(CreateElement(currentLanguage, currentElementLines));
                }
            }
        }

        public static InteractiveDocument Read(
            Stream stream,
            KernelInfoCollection kernelInfos)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = reader.ReadToEnd();
            return Parse(content, kernelInfos);
        }

        public static async Task<InteractiveDocument> ReadAsync(
            Stream stream,
            KernelInfoCollection kernelInfos)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = await reader.ReadToEndAsync();
            return Parse(content, kernelInfos);
        }

        public static string ToCodeSubmissionContent(
            this InteractiveDocument document,
            string newline = "\n")
        {
            // FIX: (ToCodeSubmissionContent) stringbuilderify this

            var lines = new List<string>();

            if (document.Metadata.Count > 0)
            {
                lines.Add($"{InteractiveNotebookCellSpecifier}meta");
                lines.Add("");
                lines.Add(JsonSerializer.Serialize(document.Metadata, ParserServerSerializer.JsonSerializerOptions));
                lines.Add("");
            }

            foreach (var element in document.Elements)
            {
                var elementLines = element.Contents.SplitIntoLines().SkipWhile(l => l.Length == 0).ToList();
                while (elementLines.Count > 0 && elementLines[^1].Length == 0)
                {
                    elementLines.RemoveAt(elementLines.Count - 1);
                }

                if (elementLines.Count > 0)
                {
                    if (element.KernelName is not null)
                    {
                        lines.Add($"{InteractiveNotebookCellSpecifier}{element.KernelName}");
                        lines.Add("");
                    }

                    lines.AddRange(elementLines);
                    lines.Add("");
                }
            }

            var content = string.Join(newline, lines);

            return content;
        }

        public static void Write(InteractiveDocument document, Stream stream, string newline = "\n")
        {
            using var writer = new StreamWriter(stream, Encoding, 1024, true);
            Write(document, writer, newline);
            writer.Flush();
        }

        public static void Write(InteractiveDocument document, TextWriter writer, string newline = "\n")
        {
            var content = document.ToCodeSubmissionContent(newline);
            writer.Write(content);
        }
    }
}
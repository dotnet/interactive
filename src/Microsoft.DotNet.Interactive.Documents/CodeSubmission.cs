﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Documents.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Documents
{
    public static class CodeSubmission
    {
        private const string InteractiveNotebookCellSpecifier = "#!";

        public static Encoding Encoding => new UTF8Encoding(false);

        public static InteractiveDocument Parse(
            string content,
            string defaultLanguage,
            KernelNameCollection kernelNames)
        {
            if (kernelNames == null)
            {
                throw new ArgumentNullException(nameof(kernelNames));
            }

            var lines = content.SplitIntoLines();

            var elements = new List<InteractiveDocumentElement>();
            var currentLanguage = defaultLanguage;
            var currentElementLines = new List<string>();

            InteractiveDocumentElement CreateElement(string elementLanguage, IEnumerable<string> elementLines)
            {
                return new(string.Join("\n", elementLines), elementLanguage);
            }

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
                    elements.Add(CreateElement(currentLanguage, currentElementLines));
                }
            }
            
            // not a kernel language, but still a valid cell splitter
            if (!kernelNames.Contains("markdown"))
            {
                kernelNames = kernelNames.Clone();
                kernelNames.Add(new KernelName("markdown", new[] { "md" }));
            }            
          
            foreach (var line in lines)
            {
                if (line.StartsWith(InteractiveNotebookCellSpecifier))
                {
                    var cellLanguage = line.Substring(InteractiveNotebookCellSpecifier.Length);

                    if (kernelNames.TryGetByAlias(cellLanguage, out var name))
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
            if (elements.Count == 0)
            {
                elements.Add(CreateElement(defaultLanguage, Array.Empty<string>()));
            }

            return new InteractiveDocument(elements);
        }

        public static InteractiveDocument Read(
            Stream stream,
            string defaultLanguage,
            KernelNameCollection kernelNames)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = reader.ReadToEnd();
            return Parse(content, defaultLanguage, kernelNames);
        }

        public static async Task<InteractiveDocument> ReadAsync(
            Stream stream,
            string defaultLanguage,
            KernelNameCollection kernelNames)
        {
            using var reader = new StreamReader(stream, Encoding);
            var content = await reader.ReadToEndAsync();
            return Parse(content, defaultLanguage, kernelNames);
        }

        public static string ToCodeSubmissionContent(this InteractiveDocument interactiveDocument, string newline = "\n")
        {
            var lines = new List<string>();

            foreach (var element in interactiveDocument.Elements)
            {
                var elementLines = element.Contents.SplitIntoLines().SkipWhile(l => l.Length == 0).ToList();
                while (elementLines.Count > 0 && elementLines[^1].Length == 0)
                {
                    elementLines.RemoveAt(elementLines.Count - 1);
                }

                if (elementLines.Count > 0)
                {
                    lines.Add($"{InteractiveNotebookCellSpecifier}{element.Language}");
                    lines.Add("");
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
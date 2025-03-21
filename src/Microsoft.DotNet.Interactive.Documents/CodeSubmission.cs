// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.Utility;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Documents;

/// <summary>
/// This format is used by the .dib file format as well as for multi-kernel code submissions.
/// </summary>
public static class CodeSubmission
{
    private const string MagicCommandPrefix = "#!";

    private static readonly Encoding _encoding = new UTF8Encoding(false);

    public static InteractiveDocument Parse(
        string content,
        KernelInfoCollection? kernelInfos = default)
    {
        kernelInfos ??= new();
        Dictionary<string, object>? metadata = null;
        var lines = content.SplitIntoLines();

        var document = new InteractiveDocument();
        var currentKernelName = kernelInfos.DefaultKernelName ?? "csharp";
        var currentElementLines = new List<string>();

        kernelInfos = WithMarkdownKernel(kernelInfos);

        var foundMetadata = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (!foundMetadata &&
                line.StartsWith("#!meta"))
            {
                foundMetadata = true;
                var sb = new StringBuilder();

                while (i < lines.Length - 1 && !(line = lines[++i]).StartsWith(MagicCommandPrefix))
                {
                    sb.AppendLine(line);
                }

                var metadataString = sb.ToString();

                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataString, InteractiveDocument.JsonSerializerOptions);

                if (InteractiveDocument.TryGetKernelInfosFromMetadata(metadata, out var kernelInfoFromMetadata))
                {
                    InteractiveDocument.MergeKernelInfos(kernelInfos, kernelInfoFromMetadata);
                    document.Metadata["kernelInfo"] = kernelInfoFromMetadata;
                }
            }

            if (line.StartsWith(MagicCommandPrefix))
            {
                var cellKernelName = line[MagicCommandPrefix.Length..];

                if (kernelInfos.TryGetByAlias(cellKernelName, out var name))
                {
                    // recognized language, finalize the current element
                    AddElement();

                    // start a new element
                    currentKernelName = name.Name;
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
        if (document.Elements.Count is 0)
        {
            document.Elements.Add(CreateElement(currentKernelName, Array.Empty<string>()));
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
                document.Elements.Add(CreateElement(currentKernelName, currentElementLines));
            }
        }

        InteractiveDocumentElement CreateElement(string kernelName, IEnumerable<string> elementLines)
        {
            return new(string.Join("\n", elementLines), kernelName);
        }
    }

    private static KernelInfoCollection WithMarkdownKernel(KernelInfoCollection kernelInfo)
    {
        // not a kernel language, but still a valid cell splitter
        if (!kernelInfo.Contains("markdown"))
        {
            kernelInfo = kernelInfo.Clone();
            kernelInfo.Add(new KernelInfo("markdown", languageName: "markdown", aliases: new[] { "md" }));
        }

        return kernelInfo;
    }

    public static InteractiveDocument Read(
        Stream stream,
        KernelInfoCollection kernelInfos)
    {
        using var reader = new StreamReader(stream, _encoding);
        var content = reader.ReadToEnd();
        return Parse(content, kernelInfos);
    }

    public static string ToCodeSubmissionContent(
        this InteractiveDocument document,
        string newline = "\n")
    {
        var lines = new List<string>();

        if (document.Metadata.Count > 0)
        {
            lines.Add($"{MagicCommandPrefix}meta");
            lines.Add("");
            lines.Add(JsonSerializer.Serialize(document.Metadata, InteractiveDocument.JsonSerializerOptions));
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
                    lines.Add($"{MagicCommandPrefix}{element.KernelName}");
                    lines.Add("");
                }

                lines.AddRange(elementLines);
                lines.Add("");
            }
        }

        var content = string.Join(newline, lines);

        return content;
    }

    public static void Write(InteractiveDocument document, Stream stream, KernelInfoCollection kernelInfos, string newline = "\n")
    {
        InteractiveDocument.MergeKernelInfos(document, kernelInfos);
        using var writer = new StreamWriter(stream, _encoding, 1024, true);
        var content = document.ToCodeSubmissionContent(newline);
        writer.Write(content);
        writer.Flush();
    }
}

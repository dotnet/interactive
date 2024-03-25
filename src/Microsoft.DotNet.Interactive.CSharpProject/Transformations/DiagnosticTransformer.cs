// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject.Transformations;

using Diagnostic = CodeAnalysis.Diagnostic;

public static class DiagnosticTransformer
{
    internal static (IReadOnlyCollection<SerializableDiagnostic> DiagnosticsInActiveBuffer, IReadOnlyCollection<SerializableDiagnostic> AllDiagnostics) MapDiagnostics(
        this Workspace workspace,
        BufferId activeBufferId,
        IReadOnlyCollection<Diagnostic> diagnostics)
    {
        if (workspace is null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (diagnostics is null || diagnostics.Count == 0)
        {
            return (Array.Empty<SerializableDiagnostic>(), Array.Empty<SerializableDiagnostic>());
        }
        else
        {
            diagnostics = diagnostics.RemoveSuppressed();
        }
            
        var viewPorts = workspace.ExtractViewPorts().ToList();

        var paddingSize = BufferInliningTransformer.PaddingSize;

        var diagnosticsInBuffer = FilterDiagnosticsForViewport().ToArray();
        var projectDiagnostics = diagnostics.Select(d => d.ToSerializableDiagnostic()).ToArray();

        return (
            diagnosticsInBuffer,
            projectDiagnostics
        );

        IEnumerable<SerializableDiagnostic> FilterDiagnosticsForViewport()
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Location == Location.None)
                {
                    continue;
                }

                var filePath = diagnostic.Location.SourceTree?.FilePath;

                // hide warnings that are not within the visible code
                if (!diagnostic.IsError() &&
                    !string.IsNullOrWhiteSpace(filePath))
                {
                    if (Path.GetFileName(filePath) != Path.GetFileName(activeBufferId?.FileName))
                    {
                        continue;
                    }
                }

                var lineSpan = diagnostic.Location.GetMappedLineSpan();
                var lineSpanPath = lineSpan.Path;

                if (viewPorts.Count == 0 || string.IsNullOrWhiteSpace(activeBufferId?.RegionName))
                {
                    var errorMessage = RelativizeDiagnosticMessage();

                    yield return diagnostic.ToSerializableDiagnostic(errorMessage, activeBufferId);
                }
                else
                {
                    var targetViewports = viewPorts
                        .Where(e => e.BufferId.RegionName != null &&
                                    e.BufferId.RegionName == activeBufferId.RegionName &&
                                    (string.IsNullOrWhiteSpace(lineSpanPath) || AreSameFile(lineSpanPath, e.Destination.Name))).ToList();
                    var target = targetViewports
                        .FirstOrDefault(e => e.Region.Contains(diagnostic.Location.SourceSpan.Start));

                    if (target is { Region.IsEmpty: false })
                    {
                        var processedDiagnostic = AlignDiagnosticLocation(target, diagnostic, paddingSize);
                        if (processedDiagnostic != null)
                        {
                            yield return processedDiagnostic;
                        }
                    }
                }

                string RelativizeDiagnosticMessage()
                {
                    var message = diagnostic.ToString();

                    if (!string.IsNullOrWhiteSpace(lineSpanPath))
                    {
                        var directoryPath = new FileInfo(lineSpanPath).Directory?.FullName ?? "";

                        if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        {
                            directoryPath += Path.DirectorySeparatorChar;
                        }

                        if (message.StartsWith(directoryPath))
                        {
                            return message.Substring(directoryPath.Length);
                        }
                    }

                    return message;
                }
            }
        }
    }

    private static bool AreSameFile(string lineSpanPath, string destinationName)
    {
        var fileName = Path.GetFileName(lineSpanPath);
        var destFileName = Path.GetFileName(destinationName);
        return string.Compare(fileName, destFileName, StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static SerializableDiagnostic AlignDiagnosticLocation(
        Viewport viewport,
        Diagnostic diagnostic,
        int paddingSize)
    {
        // this diagnostics does not apply to viewport
        if (diagnostic.Location!= Location.None
            && !string.IsNullOrWhiteSpace(diagnostic.Location.SourceTree?.FilePath)
            && !AreSameFile(diagnostic.Location.SourceTree.FilePath, viewport.Destination.Name))
        {
            return null;
        }

        // offset of the buffer into the original source file
        var offset = viewport.Region.Start;
        // span of content injected in the buffer viewport
        var selectionSpan = new TextSpan(offset + paddingSize, viewport.Region.Length - (2 * paddingSize));

        // aligned offset of the diagnostic entry
        var start = diagnostic.Location.SourceSpan.Start - selectionSpan.Start;
        var end = diagnostic.Location.SourceSpan.End - selectionSpan.Start;
        // line containing the diagnostic in the original source file
        var line = viewport.Destination.Text.Lines[diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line];

        // first line of the region from the source file
        var lineOffset = 0;
        var sourceText = viewport.Destination.Text.GetSubText(selectionSpan);

        foreach (var regionLine in sourceText.Lines)
        {
            if (regionLine.ToString() == line.ToString())
            {
                var lineText = line.ToString();
                var startCharacter = diagnostic.Location.GetMappedLineSpan().Span.Start.Character;
                if (startCharacter <= lineText.Length)
                {
                    var partToFind = lineText.Substring(startCharacter);
                    var charOffset = sourceText.Lines[lineOffset].ToString().IndexOf(partToFind, StringComparison.Ordinal);

                    var errorMessage = $"({lineOffset + 1},{charOffset + 1}): error {diagnostic.Id}: {diagnostic.GetMessage()}";

                    return new SerializableDiagnostic(
                        start,
                        end,
                        errorMessage,
                        diagnostic.Severity,
                        diagnostic.Id,
                        viewport.BufferId);
                }
            }

            lineOffset++;
        }

        return null;
    }

    private static readonly HashSet<string> _suppressedDiagnosticIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CS7022", // The entry point of the program is global script code; ignoring 'Main()' entry point.
            "CS8019", // unused using directive
        };

    public static IReadOnlyCollection<Diagnostic> RemoveSuppressed(this IEnumerable<Diagnostic> o) =>
        o.Where(d => !_suppressedDiagnosticIds.Contains(d.Id))
            .ToArray();
}
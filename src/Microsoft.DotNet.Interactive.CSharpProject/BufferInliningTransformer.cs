// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using File = System.IO.File;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class BufferInliningTransformer : IWorkspaceTransformer
{
    public static IWorkspaceTransformer Instance { get; } = new BufferInliningTransformer();

    private static readonly string Padding = "\n";

    public static int PaddingSize => Padding.Length;

    public async Task<Workspace> TransformAsync(Workspace source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        var (files, buffers) = await InlineBuffersAsync(source);

        return new Workspace(
            workspaceType: source.WorkspaceType,
            files: files,
            buffers: buffers,
            usings: source.Usings);
    }

    protected async Task<(ProjectFileContent[] files, Buffer[] buffers)> InlineBuffersAsync(Workspace source)
    {
        var files = (source.Files ?? Array.Empty<ProjectFileContent>()).ToDictionary(f => f.Name, f =>
        {
            if (string.IsNullOrEmpty(f.Text) && File.Exists(f.Name))
            {
                return SourceFile.Create(File.ReadAllText(f.Name), f.Name);
            }

            return f.ToSourceFile();
        }, StringComparer.OrdinalIgnoreCase);

        var buffers = new List<Buffer>();
        foreach (var sourceBuffer in source.Buffers)
        {
            var bufferFileName = sourceBuffer.Id.FileName;
            if (!files.ContainsKey(bufferFileName) && File.Exists(bufferFileName))
            {
                var sourceFile = SourceFile.Create(File.ReadAllText(bufferFileName), bufferFileName);
                files[bufferFileName] = sourceFile;
            }

            if (!string.IsNullOrWhiteSpace(sourceBuffer.Id.RegionName))
            {
                var normalizedBufferId = sourceBuffer.Id.GetNormalized();
                var injectionPoint = sourceBuffer.Id.GetInjectionPoint();
                var viewPorts = files.Select(f => f.Value).ExtractViewports();
                if (viewPorts.SingleOrDefault(viewport => viewport.BufferId == normalizedBufferId) is { } viewPort)
                {
                    await InjectBuffer(viewPort, sourceBuffer, buffers, files, injectionPoint);
                }
                else
                {
                    throw new ArgumentException($"Could not find specified buffer: {sourceBuffer.Id}");
                }
            }
            else
            {
                files[sourceBuffer.Id.FileName] = SourceFile.Create(sourceBuffer.Content, sourceBuffer.Id.FileName);
                buffers.Add(sourceBuffer);
            }
        }

        var processedFiles = files.Values.Select(sf => new ProjectFileContent(sf.Name, sf.Text.ToString())).ToArray();
        var processedBuffers = buffers.ToArray();

        return (processedFiles, processedBuffers);
    }
      
    private Task InjectBuffer(Viewport viewPort, Buffer sourceBuffer, ICollection<Buffer> buffers, IDictionary<string, SourceFile> files,
        BufferInjectionPoints bufferIdInjectionPoints)
    {
        TextSpan targetSpan;
        switch (bufferIdInjectionPoints)
        {
            case BufferInjectionPoints.Before:
                targetSpan = CreateTextSpanBefore(viewPort.OuterRegion);
                break;
            case BufferInjectionPoints.After:
                targetSpan = CreateTextSpanAfter(viewPort.OuterRegion);
                break;
            default:
                targetSpan = viewPort.Region;
                break;
        }
        return InjectBufferAtSpan(viewPort, sourceBuffer, buffers, files, targetSpan);
    }

    private static TextSpan CreateTextSpanAfter(TextSpan viewPortRegion)
    {
        return new TextSpan(viewPortRegion.End, 0);
    }

    private static TextSpan CreateTextSpanBefore(TextSpan viewPortRegion)
    {
        return new TextSpan(viewPortRegion.Start, 0);
    }

    private async Task InjectBufferAtSpan(Viewport viewPort, Buffer sourceBuffer, ICollection<Buffer> buffers, IDictionary<string, SourceFile> files, TextSpan span)
    {
        var tree = CSharpSyntaxTree.ParseText(viewPort.Destination.Text.ToString());
        var textChange = new TextChange(
            span,
            $"{Padding}{sourceBuffer.Content}{Padding}");

        var txt = tree.WithChangedText(tree.GetText().WithChanges(textChange));

        var offset = span.Start + PaddingSize;

        var newCode = (await txt.GetTextAsync()).ToString();

        buffers.Add(new Buffer(
            sourceBuffer.Id,
            sourceBuffer.Content,
            sourceBuffer.Position,
            offset));
        files[viewPort.Destination.Name] = SourceFile.Create(newCode, viewPort.Destination.Name);
    }
}
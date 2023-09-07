// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class CodeMergeTransformer : IWorkspaceTransformer
{
    public static IWorkspaceTransformer Instance { get; } = new CodeMergeTransformer();

    private static readonly string Padding = "\n";

    public Task<Workspace> TransformAsync(Workspace source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var files = (source.Files ?? Array.Empty<ProjectFileContent>())
            .GroupBy(file => file.Name)
            .Select(fileGroup => MergeFiles(fileGroup.Key, fileGroup));

        var buffers = (source.Buffers ?? Array.Empty<Buffer>())
            .GroupBy(buffer => buffer.Id)
            .SelectMany(bufferGroup => MergeBuffers(bufferGroup.Key, bufferGroup));

        var workspace = new Workspace(
            workspaceType: source.WorkspaceType,
            usings: source.Usings,
            files: files.ToArray(),
            buffers: buffers.ToArray());

        return Task.FromResult(workspace);
    }

    private ProjectFileContent MergeFiles(string fileName, IEnumerable<ProjectFileContent> files)
    {
        var content = string.Empty;
        var order = 0;
        foreach (var file in files.OrderBy(file => file.Order))
        {
            order = file.Order;
            content = $"{content}{file.Text}{Padding}";

        }
        content = content.Substring(0, content.Length - Padding.Length);

        return new(fileName, content, order: order);
    }

    private IEnumerable<Buffer> MergeBuffers(BufferId id, IEnumerable<Buffer> buffers)
    {
        var position = 0;
        var content = string.Empty;
        var order = 0;

        foreach (var buffer in buffers.OrderBy(buffer => buffer.Order))
        {
            order = buffer.Order;
            if (buffer.Position != 0)
            {
                position = content.Length + buffer.Position;
            }

            content = $"{content}{buffer.Content}{Padding}";
        }

        content = content.Substring(0, content.Length - Padding.Length);

        yield return new Buffer(id, content, position: position, order: order);
    }
}
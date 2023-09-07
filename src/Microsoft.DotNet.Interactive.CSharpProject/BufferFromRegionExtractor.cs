// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class BufferFromRegionExtractor
{
    public Workspace Extract(IReadOnlyCollection<ProjectFileContent> sourceFiles, string workspaceType = null, string[] usings = null)
    {
        var workSpaceType = workspaceType ?? "script";
        var (newFiles, newBuffers) = ProcessSourceFiles(sourceFiles);
        return new Workspace(files: newFiles, buffers: newBuffers, usings: usings, workspaceType: workSpaceType);
    }

    private static (ProjectFileContent[], Buffer[]) ProcessSourceFiles(IEnumerable<ProjectFileContent> sourceFiles)
    {
        var files = new Dictionary<string, ProjectFileContent>(StringComparer.OrdinalIgnoreCase);
        var newBuffers = new List<Buffer>();
        foreach (var sourceFile in sourceFiles)
        {
            var buffers = SourceText.From(sourceFile.Text).ExtractBuffers(sourceFile.Name).ToList();
            if (buffers.Count > 0)
            {
                files[sourceFile.Name] = sourceFile;
                foreach (var buffer in buffers)
                {
                    newBuffers.Add(buffer);
                }
            }
            else
            {
                newBuffers.Add(new Buffer(sourceFile.Name, sourceFile.Text, 0));
            }
        }
        return (files.Values.ToArray(), newBuffers.ToArray());
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models.Execution;

public class WorkspaceFactory
{
    public static Workspace CreateWorkspaceFromDirectory(
        DirectoryInfo directory,
        string workspaceType,
        bool includeInstrumentation = false)
    {
        var filesOnDisk = directory.GetFiles("*.cs", SearchOption.AllDirectories)
            .Where(f => !f.IsBuildOutput())
            .ToArray();

        var files = filesOnDisk.Select(file => new ProjectFileContent(file.Name, file.Read())).ToList();

        return new Workspace(
            files: files.ToArray(),
            buffers: files.Select(f => new Buffer(
                    f.Name,
                    filesOnDisk.Single(fod => fod.Name == f.Name)
                        .Read()))
                .ToArray(),
            workspaceType: workspaceType,
            includeInstrumentation: includeInstrumentation);
    }
}
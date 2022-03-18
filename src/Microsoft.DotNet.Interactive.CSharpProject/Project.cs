// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class Project
{
    public IReadOnlyCollection<ProjectFile> Files { get; }

    public Project(IReadOnlyCollection<ProjectFile> files)
    {
        Files = files;
    }
}
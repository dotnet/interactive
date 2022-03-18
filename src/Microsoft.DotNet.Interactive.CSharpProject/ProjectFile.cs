// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class ProjectFile
{
    public string RelativePath { get; }
    public string Content { get; }

    public ProjectFile(string relativePath, string content)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Only relative paths are allowed.", nameof(relativePath));
        }
        RelativePath = relativePath;
        Content = content;
    }
}
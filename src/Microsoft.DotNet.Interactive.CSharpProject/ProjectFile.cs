// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class ProjectFile
{
    public string Path { get; }
    public string Content { get; }

    public ProjectFile(string path, string content)
    {
        Path = path;
        Content = content;
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class ProjectFile
{
    private readonly RelativeFilePath _relativePath;
    public string RelativePath => _relativePath.ToString();
    
    public string Content { get; }

    [JsonConstructor]
    public ProjectFile(string relativePath, string content): this(new RelativeFilePath(relativePath), content)
    {
    }

    public ProjectFile(RelativeFilePath relativePath, string content)
    {
        _relativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        Content = content;
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class ProjectFile
{
    private readonly RelativeFilePath _relativeFilePath;
    public string RelativeFilePath => _relativeFilePath.ToString();
    
    public string Content { get; }

    [JsonConstructor]
    public ProjectFile(string relativeFilePath, string content): this(new RelativeFilePath(relativeFilePath), content)
    {
    }

    public ProjectFile(RelativeFilePath relativeFilePath, string content)
    {
        _relativeFilePath = relativeFilePath ?? throw new ArgumentNullException(nameof(relativeFilePath));
        Content = content;
    }
}

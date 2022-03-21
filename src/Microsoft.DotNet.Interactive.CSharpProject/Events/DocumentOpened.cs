// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.CSharpProject.Events;

public class DocumentOpened : KernelEvent
{
    private RelativeFilePath _relativePath;

    public string RelativePath => _relativePath.ToString();
    public string RegionName { get; }
    public string Content { get; }

    [JsonConstructor]
    public DocumentOpened(OpenDocument command, string relativePath, string regionName, string content)
        : this(command, new RelativeFilePath(relativePath), regionName, content)
    {
    }

    public DocumentOpened(OpenDocument command, RelativeFilePath relativePath, string regionName, string content)
        : base(command)
    {
        _relativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        RegionName = regionName;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}

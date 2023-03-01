// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.CSharpProject.Events;

public class DocumentOpened : KernelEvent
{
    private readonly RelativeFilePath _relativeFilePath;

    public string RelativeFilePath => _relativeFilePath.ToString();
    public string RegionName { get; }
    public string Content { get; }

    [JsonConstructor]
    public DocumentOpened(OpenDocument command, string relativeFilePath, string regionName, string content)
        : this(command, new RelativeFilePath(relativeFilePath), regionName, content)
    {
    }

    public DocumentOpened(OpenDocument command, RelativeFilePath relativeFilePath, string regionName, string content)
        : base(command)
    {
        _relativeFilePath = relativeFilePath ?? throw new ArgumentNullException(nameof(relativeFilePath));
        RegionName = regionName;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.CSharpProject.Events;

public class DocumentOpened : KernelEvent
{
    public string Path { get; }
    public string RegionName { get; }
    public string Content { get; }

    public DocumentOpened(OpenDocument command, string path, string regionName, string content)
        : base(command)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        RegionName = regionName;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}
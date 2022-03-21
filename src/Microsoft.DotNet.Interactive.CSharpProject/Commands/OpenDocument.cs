// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;

namespace Microsoft.DotNet.Interactive.CSharpProject.Commands;

public class OpenDocument : KernelCommand
{
    private readonly RelativeFilePath _relativePath;
    public string RelativePath => _relativePath.ToString();

    public string RegionName { get; }

    [JsonConstructor]
    public OpenDocument(string relativePath, string regionName = null)
        : this(new RelativeFilePath(relativePath), regionName)
    {
    }

    public OpenDocument(RelativeFilePath relativePath, string regionName)
    {
        _relativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        RegionName = regionName;
    }
}

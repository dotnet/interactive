// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.CSharpProject.Commands;

public class OpenDocument : KernelCommand
{
    private readonly RelativeFilePath _relativeFilePath;
    public string RelativeFilePath => _relativeFilePath.ToString();

    public string RegionName { get; }

    [JsonConstructor]
    public OpenDocument(string relativeFilePath, string regionName = null)
        : this(new RelativeFilePath(relativeFilePath), regionName)
    {
    }

    public OpenDocument(RelativeFilePath relativeFilePath, string regionName)
    {
        _relativeFilePath = relativeFilePath ?? throw new ArgumentNullException(nameof(relativeFilePath));
        RegionName = regionName;
    }
}

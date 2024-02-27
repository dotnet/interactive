// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Directives;

public abstract class KernelDirective
{
    [JsonConstructor]
    protected KernelDirective(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    internal KernelInfo? ParentKernelInfo { get; set; }

    public override string ToString() => Name;
}
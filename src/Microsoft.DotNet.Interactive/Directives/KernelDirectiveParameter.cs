// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
namespace Microsoft.DotNet.Interactive.Directives;

public class KernelDirectiveParameter
{
    public KernelDirectiveParameter(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool AllowImplicitName { get; init; }

    public int MaxOccurrences { get; init; } = 1;

    public bool Required { get; init; } = false;

    public string TypeHint { get; init; } = "text";

    public bool Flag { get; set; }
}
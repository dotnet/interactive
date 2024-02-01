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
    
    public int MaxOccurrences { get; set; } = 1;

    public bool Required { get; set; } = false;
}
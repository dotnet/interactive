// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
namespace Microsoft.DotNet.Interactive.Directives;

public abstract class KernelDirective
{
    public KernelDirective(string name)
    {
        Name = name;
    }

    public string Name { get; init; }
}

public class KernelDirectiveNamedParameter
{
    public KernelDirectiveNamedParameter(string name)
    {
        Name = name;
    }

    public string Name { get; }
    
    public int MaxOccurrences { get; set; } = 1;

    public bool Required { get; set; } = false;
}

public class KernelDirectiveParameter
{
    public KernelDirectiveParameter(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public int MaxOccurrences { get; set; } = 1;
}
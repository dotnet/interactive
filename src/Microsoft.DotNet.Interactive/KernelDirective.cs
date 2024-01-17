// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive;

public abstract class KernelDirective
{
    public KernelDirective(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    
}

public class KernelSpecifierDirective : KernelDirective
{
    public KernelSpecifierDirective(string name) : base(name)
    {
    }

    public string Name { get; init; }
}

public class KernelActionDirective : KernelDirective, IEnumerable
{
    public KernelActionDirective(string name) : base(name)
    {
    }

    public void Add(KernelActionDirective command)
    {
    }

    public void Add(KernelDirectiveNamedParameter parameter)
    {
    }

    public void Add(KernelDirectiveParameter parameter)
    {
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var parameter in NamedParameters)
        {
            yield return parameter;
        }

        foreach (var parameter in Parameters)
        {
            yield return parameter;
        }
    }

    public ICollection<KernelDirectiveNamedParameter> NamedParameters { get; }

    public IList<KernelDirectiveParameter> Parameters { get; }
}

public class KernelDirectiveNamedParameter
{
    public KernelDirectiveNamedParameter(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public class KernelDirectiveParameter
{
    public KernelDirectiveParameter(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
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

public class KernelActionDirective : KernelDirective
{
    private readonly NamedSymbolCollection<KernelDirectiveNamedParameter> _namedParameters = new(directive => directive.Name);
    private readonly NamedSymbolCollection<KernelActionDirective> _subcommands = new(directive => directive.Name);
    private readonly List<KernelDirectiveParameter> _parameters = new();

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

    public ICollection<KernelDirectiveNamedParameter> NamedParameters => _namedParameters;

    public ICollection<KernelActionDirective> Subcommands => _subcommands;

    public IList<KernelDirectiveParameter> Parameters => _parameters;

    internal bool TryGetNamedParameter(string name, out KernelDirectiveNamedParameter value) => _namedParameters.TryGetValue(name, out value);

    internal bool TryGetSubcommand(string name, out KernelActionDirective value) => _subcommands.TryGetValue(name, out value);
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

internal class NamedSymbolCollection<T> : ICollection<T>
{
    private readonly Func<T, string> _getName;
    private readonly List<T> _items = new();
    private readonly Dictionary<string, T> _itemsByName = new();

    public NamedSymbolCollection(Func<T, string> getName)
    {
        _getName = getName;
    }

    public bool TryGetValue(string name, out T item)
    {
        return _itemsByName.TryGetValue(name, out item);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public void Add(T item)
    {
        _itemsByName.Add(_getName(item), item);
        _items.Add(item);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Contains(T item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _items.Remove(item);
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;
}
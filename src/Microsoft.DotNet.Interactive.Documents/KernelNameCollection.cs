// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Documents;

public class KernelNameCollection : IReadOnlyCollection<KernelName>
{
    private readonly List<KernelName> _kernelNames;
    private string? _defaultKernelName;
    private Dictionary<string, KernelName>? _mapOfKernelNamesByAlias;

    public KernelNameCollection()
    {
        _kernelNames = new List<KernelName>();
    }

    public KernelNameCollection(IReadOnlyCollection<KernelName> kernelNames)
    {
        _kernelNames = new List<KernelName>(kernelNames);

        if (_kernelNames.Count == 1)
        {
            DefaultKernelName = _kernelNames[0].Name;
        }
    }

    public int Count => _kernelNames.Count;

    public string? DefaultKernelName
    {
        get => _defaultKernelName ?? (_kernelNames.Count == 1
                                          ? _kernelNames[0].Name
                                          : null);
        set => _defaultKernelName = value;
    }

    public void Add(KernelName kernelName)
    {
        _kernelNames.Add(kernelName);
        _mapOfKernelNamesByAlias = null;
    }

    public bool Contains(string name)
    {
        EnsureIndexIsCreated();

        return _mapOfKernelNamesByAlias!.ContainsKey(name);
    }

    private void EnsureIndexIsCreated()
    {
        if (_mapOfKernelNamesByAlias is null)
        {
            _mapOfKernelNamesByAlias =
                _kernelNames
                    .SelectMany(n => n.Aliases.Select(a => (name: n, alias: a)))
                    .ToDictionary(x => x.alias, x => x.name);
        }
    }

    public IEnumerator<KernelName> GetEnumerator()
    {
        return _kernelNames.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_kernelNames).GetEnumerator();
    }

    public KernelNameCollection Clone()
    {
        var clone = new KernelNameCollection(this)
        {
            _defaultKernelName = _defaultKernelName,
            _mapOfKernelNamesByAlias = _mapOfKernelNamesByAlias
        };
        return clone;
    }

    public bool TryGetByAlias(string alias, out KernelName name)
    {
        EnsureIndexIsCreated();

        return _mapOfKernelNamesByAlias!.TryGetValue(alias, out name);
    }
}
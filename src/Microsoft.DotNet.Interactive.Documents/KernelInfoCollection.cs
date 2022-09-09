// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Documents;

public class KernelInfoCollection : ICollection<KernelInfo>
{
    private readonly List<KernelInfo> _kernelNames = new();
    private string _defaultKernelName;
    private Dictionary<string, KernelInfo> _mapOfKernelNamesByAlias;

    public bool Remove(KernelInfo item)
    {
        return _kernelNames.Remove(item);
    }

    public int Count => _kernelNames.Count;

    public bool IsReadOnly => false;

    public string DefaultKernelName
    {
        get => _defaultKernelName ?? (_kernelNames.Count == 1
                                          ? _kernelNames[0].Name
                                          : null);
        set => _defaultKernelName = value;
    }

    public void Add(KernelInfo kernelInfo)
    {
        _kernelNames.Add(kernelInfo);
        _mapOfKernelNamesByAlias = null;
    }

    public void Clear()
    {
        _kernelNames.Clear();
    }

    public bool Contains(KernelInfo item)
    {
        return _kernelNames.Contains(item);
    }

    public void CopyTo(KernelInfo[] array, int arrayIndex)
    {
        _kernelNames.CopyTo(array, arrayIndex);
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

    public KernelInfoCollection Clone()
    {
        var clone = new KernelInfoCollection
        {
            _defaultKernelName = _defaultKernelName
        };
        clone._kernelNames.AddRange(_kernelNames);
        return clone;
    }

    public bool TryGetByAlias(string alias, out KernelInfo info)
    {
        EnsureIndexIsCreated();

        return _mapOfKernelNamesByAlias!.TryGetValue(alias, out info);
    }

    public IEnumerator<KernelInfo> GetEnumerator()
    {
        return _kernelNames.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
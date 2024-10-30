// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Documents;

[JsonConverter(typeof(KernelInfoCollectionConverter))]
public class KernelInfoCollection : ICollection<KernelInfo>
{
    private readonly HashSet<KernelInfo> _kernelNames = new();
    private readonly Dictionary<string, KernelInfo> _kernelInfoByNameOrAlias = new();
    private string? _defaultKernelName;

    public int Count => _kernelNames.Count;

    public bool IsReadOnly => false;

    public string? DefaultKernelName
    {
        get => _defaultKernelName ?? (_kernelNames.Count == 1
                                          ? _kernelNames.Single().Name
                                          : null);
        set => _defaultKernelName = value;
    }

    public void Add(KernelInfo kernelInfo)
    {
        foreach (var alias in kernelInfo.Aliases.Append(kernelInfo.Name))
        {
            try
            {
                _kernelInfoByNameOrAlias.Add(alias, kernelInfo);
            }
            catch (ArgumentException argumentException)
            {
                throw new ArgumentException($"A {nameof(KernelInfo)} with name or alias '{alias}' is already present in the collection.", argumentException);
            }
        }

        _kernelNames.Add(kernelInfo);
    }

    public void AddRange(IEnumerable<KernelInfo> collection)
    {
        foreach (var info in collection)
        {
            Add(info);
        }
    }

    public void Clear()
    {
        _kernelNames.Clear();
        _kernelInfoByNameOrAlias.Clear();
    }

    public bool Remove(KernelInfo item)
    {
        var removed = _kernelNames.Remove(item);

        if (removed)
        {
            foreach (var alias in item.Aliases.Append(item.Name))
            {
                _kernelInfoByNameOrAlias.Remove(alias);
            }
        }

        return removed;
    }

    public bool Contains(KernelInfo kernelInfo)
    {
        return _kernelNames.Contains(kernelInfo);
    }

    public void CopyTo(KernelInfo[] array, int arrayIndex)
    {
        _kernelNames.CopyTo(array, arrayIndex);
    }

    public bool Contains(string nameOrAlias)
    {
        return _kernelInfoByNameOrAlias!.ContainsKey(nameOrAlias);
    }

    public KernelInfoCollection Clone()
    {
        var clone = new KernelInfoCollection
        {
            _defaultKernelName = _defaultKernelName
        };
        clone.AddRange(this);
        return clone;
    }

    internal bool TryGetByAlias(string alias, out KernelInfo info)
    {
        return _kernelInfoByNameOrAlias!.TryGetValue(alias, out info);
    }

    IEnumerator<KernelInfo> IEnumerable<KernelInfo>.GetEnumerator()
    {
        return _kernelNames.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _kernelNames.GetEnumerator();
    }
}

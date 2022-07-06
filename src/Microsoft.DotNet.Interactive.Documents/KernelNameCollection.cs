// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents;

public class KernelNameCollection : IReadOnlyCollection<KernelName>
{
    private readonly List<KernelName> _kernelNames;
    private string? _defaultKernelName;

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
    }

    public IEnumerator<KernelName> GetEnumerator()
    {
        return _kernelNames.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_kernelNames).GetEnumerator();
    }
}
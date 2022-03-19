// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive;

internal class KernelRegistry : IReadOnlyCollection<Kernel>
{
    private readonly CompositeKernel _compositeKernel;

    private readonly List<Kernel> _kernels = new();
    private readonly Dictionary<Uri, Kernel> _kernelsByLocalUri = new();
    private readonly Dictionary<Uri, Kernel> _kernelsByRemoteUri = new();
    private readonly Dictionary<string, Kernel> _kernelsByNameOrAlias = new();

    public KernelRegistry(CompositeKernel compositeKernel)
    {
        _compositeKernel = compositeKernel;
    }

    public void Add(Kernel kernel)
    {
        if (kernel.KernelInfo.NameAndAliases.FirstOrDefault(a => _kernelsByNameOrAlias.ContainsKey(a)) is { } collidingAlias)
        {
            throw new ArgumentException($"Alias '#!{collidingAlias}' is already in use.");
        }

        UpdateKernelInfoAndIndex(kernel);

        _kernels.Add(kernel);
    }

    public bool TryGetByAlias(string alias, out Kernel kernel)
    {
        if (_kernelsByNameOrAlias.TryGetValue(alias, out kernel))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryGetByUri(Uri uri, out Kernel kernel)
    {
        if (_kernelsByLocalUri.TryGetValue(uri, out kernel) || 
            _kernelsByRemoteUri.TryGetValue(uri, out kernel))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public IEnumerator<Kernel> GetEnumerator() =>
        _kernels.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    public int Count => _kernels.Count;

    public void NotifyHostSet()
    {
        foreach (var kernel in _kernels)
        {
            UpdateKernelInfoAndIndex(kernel);
        }
    }

    private void UpdateKernelInfoAndIndex(
        Kernel kernel,
        IEnumerable<string> aliases = null)
    {
        if (aliases is not null)
        {
            kernel.KernelInfo.NameAndAliases.UnionWith(aliases);
        }

        foreach (var alias in kernel.KernelInfo.NameAndAliases)
        {
            _kernelsByNameOrAlias.TryAdd(alias, kernel);
        }

        if (_compositeKernel.Host is { } host)
        {
            kernel.KernelInfo.Uri = new Uri(host.Uri, kernel.Name);
            _kernelsByLocalUri.TryAdd(kernel.KernelInfo.Uri, kernel);
        }

        if (kernel is ProxyKernel proxyKernel)
        {
            _kernelsByRemoteUri.TryAdd(proxyKernel.KernelInfo.RemoteUri, proxyKernel);
        }
    }
}
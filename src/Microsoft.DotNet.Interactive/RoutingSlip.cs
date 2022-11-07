// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public interface IRoutingSlip
{
    void Stamp(Uri uri);
    Uri[] ToArray();

    bool StartsWith(IRoutingSlip other);

    bool StartsWith(params Uri[] uris);

    void Append(IRoutingSlip other);
}
public class RoutingSlip : IReadOnlyList<Uri>, IRoutingSlip
{
    private readonly List<Uri> _uris;
    private readonly object _lock = new();

    public RoutingSlip(IRoutingSlip source = null)
    {
        _uris = source switch
        {
            { } => new List<Uri>(source.ToArray()),
            _ => new List<Uri>()
        };
    }

    public bool TryAdd(Uri kernelOrKernelHostUri)
    {
        lock (_lock)
        {
            if (_uris.FirstOrDefault(u => u == kernelOrKernelHostUri) is null)
            {
                _uris.Add(kernelOrKernelHostUri);
                return true;
            }
        }

        return false;
    }

    public bool Contains(Uri kernelOrKernelHostUri)
    {
        bool contains;

        lock (_lock)
        {
            contains = _uris.FirstOrDefault(u => u == kernelOrKernelHostUri) is not null;
        }

        return contains;
    }

    public bool Contains(RoutingSlip other)
    {
        var contains = this.Zip(other, (o, i) => o.Equals(i)).All(x => x);
        return contains;
    }

    IEnumerator<Uri> IEnumerable<Uri>.GetEnumerator() => _uris.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _uris.GetEnumerator();

    public Uri this[int index] => _uris[index];
    public int Count => _uris.Count;
    public void Stamp(Uri uri)
    {
        if (!TryAdd(uri))
        {
            throw new InvalidOperationException($"The uri {uri} is already in the routing slip");
        }
    }

    public Uri[] ToArray()
    {
        return _uris.ToArray();
    }

    public bool StartsWith(IRoutingSlip other)
    {
        return StartsWith(other.ToArray());
    }

    public bool StartsWith(params Uri[] uris)
    {
        var startsWith = true;
        lock (_lock)
        {
            if (uris.Length <= _uris.Count)
            {
                if (_uris.Where((uri, i) => uris[i] != uri).Any())
                {
                    startsWith = false;
                }
            }
            else
            {
                startsWith = false;
            }
        }

        return startsWith;
    }

    public void Append(IRoutingSlip other)
    {
        var source = other.ToArray();
        if (other.StartsWith(this))
        {
            source = source.Skip(_uris.Count).ToArray();
        }

        foreach (var uri in source)
        {
            Stamp(uri);
        }
    }
}
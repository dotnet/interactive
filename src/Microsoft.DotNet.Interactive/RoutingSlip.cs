// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip : IReadOnlyList<Uri>
{
    private readonly HashSet<Uri> _uniqueUris ;
    private readonly List<Uri> _uris;
    private readonly object _lock = new();

    public RoutingSlip(RoutingSlip source = null)
    {
        if (source is { })
        {
            _uniqueUris = new HashSet<Uri>(source);
            _uris = new List<Uri>(source);
        }
        else
        {
            _uniqueUris = new HashSet<Uri>();
            _uris = new List<Uri>();
        }
    }

    public bool TryAdd(Uri kernelOrKernelHostUri)
    {
        lock (_lock)
        {
            if (_uniqueUris.Add(kernelOrKernelHostUri))
            {
                _uris.Add(kernelOrKernelHostUri);
                return true;
            }
        }

        return false;
    }

    public bool Contains(Uri kernelOrKernelHostUri)
    {
        return _uniqueUris.Contains(kernelOrKernelHostUri);
    }
    
    public bool Contains(RoutingSlip other)
    {
        if (other._uris.Count < _uris.Count)
        {
            return false;
        }
        
        for (var i = 0; i < _uris.Count; i++)
        {
            if (_uris[i].Equals(other._uris[i]))
            {
                return false;
            }
        }

        return true;
    }

    IEnumerator<Uri> IEnumerable<Uri>.GetEnumerator()
    {
        return _uris.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _uris.GetEnumerator();
    }

    public Uri this[int index] => _uris[index];
    public int Count => _uris.Count;
}

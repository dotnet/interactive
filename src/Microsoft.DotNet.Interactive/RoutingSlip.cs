// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip : IReadOnlyCollection<Uri>
{
    private readonly List<Uri> _uris;
    private readonly object _lock = new();

    public RoutingSlip()
    {
        _uris = new List<Uri>();
    }

    public bool TryAdd(Uri kernelOrKernelHostUri)
    {
        var added = false;
        lock (_lock)
        {
            if (!_uris.Contains(kernelOrKernelHostUri))
            {
                _uris.Add(kernelOrKernelHostUri);
                added = true;
            }
        }
       
        return added;
    }

    public IEnumerator<Uri> GetEnumerator()
    {
        return _uris.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) _uris).GetEnumerator();
    }

    public Uri this[int i] => _uris[i];
    
    public int Count => _uris.Count;
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip : IReadOnlyList<Uri>
{
    private readonly List<Uri> _uris;
    private readonly object _lock = new();

    public RoutingSlip()
    {
        _uris = new List<Uri>();
    }

    public bool TryAdd(Uri kernelOrKernelHostUri)
    {
        lock (_lock)
        {
            if (!_uris.Contains(kernelOrKernelHostUri))
            {
                _uris.Add(kernelOrKernelHostUri);
                return true;
            }
        }

        return false;
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
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip : IReadOnlyList<Uri>
{
    private readonly HashSet<Uri> _uniqueUris ;
    private readonly List<Entry> _uris;
    private readonly ConcurrentDictionary<Uri, Entry> _entriesByUri;

    public RoutingSlip(RoutingSlip source = null)
    {
        if (source is { })
        {
            _uniqueUris = new HashSet<Uri>(source);
            _uris = new List<Entry>(source._uris);
            _entriesByUri = new ConcurrentDictionary<Uri, Entry>(source._entriesByUri);
        }
        else
        {
            _uniqueUris = new HashSet<Uri>();
            _entriesByUri = new ConcurrentDictionary<Uri, Entry>();
            _uris = new List<Entry>();
        }
    }

    public bool TryMarkArrival(Uri kernelOrKernelHostUri)
    {
        return TryMark(kernelOrKernelHostUri, false);
    }

    public bool TryMarkHandled(Uri kernelOrKernelHostUri)
    {
        return TryMark(kernelOrKernelHostUri, true);
    }

    private bool TryMark(Uri kernelOrKernelHostUri, bool handled)
    {
        var entry = new Entry(kernelOrKernelHostUri)
        {
            Handled = handled
        };
        var ret = _entriesByUri.TryAdd(kernelOrKernelHostUri, entry);
        if (ret)
        {
            _uniqueUris.Add(kernelOrKernelHostUri);
            _uris.Add(new Entry(kernelOrKernelHostUri));
        }

        return ret;
    }

    public void MarkHandled(Uri kernelOrKernelHostUri)
    {
        if (_entriesByUri.TryGetValue(kernelOrKernelHostUri, out var entry))
        {
            if (entry.Handled)
            {
                throw new InvalidOperationException($"The routing slip has already been handled by {kernelOrKernelHostUri}");
            }
            else
            {
                entry.Handled = true;
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"The kernel or kernel host {kernelOrKernelHostUri} is not part of this routing slip");
        }
    }

    public bool Contains(Uri kernelOrKernelHostUri) => _entriesByUri.ContainsKey(kernelOrKernelHostUri);

    public bool Contains(RoutingSlip other)
    {
        var contains_old = this.Zip(other, (o, i) => o.Equals(i)).All(x => x);
        var contains = _uniqueUris.IsSupersetOf(other._uniqueUris);
        return contains;
    }

    IEnumerator<Uri> IEnumerable<Uri>.GetEnumerator() => _uris.Select(e => e.KernelUri).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _uris.GetEnumerator();

    public Uri this[int index] => _uris[index].KernelUri;
    public int Count => _uris.Count;

    public class Entry
    {
        public Entry(Uri kernelUri)
        {
            KernelUri = kernelUri;
            Handled = false;
        }

        public bool Handled { get; internal set; }

        public Uri KernelUri { get; }
    }

    public bool HasProcessed(Uri kernelInfoUri)
    {
        return _entriesByUri.TryGetValue(kernelInfoUri, out var entry) && entry.Handled;
    }
}


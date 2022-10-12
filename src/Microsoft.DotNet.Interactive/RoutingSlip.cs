// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip
{
    private readonly ConcurrentDictionary<Uri, Entry> _entriesByKernelUris = new();
    private readonly List<Entry> _orderedEntries = new();
   

    public RoutingSlip(RoutingSlip source = null)
    {
        if (source is { })
        {
            foreach (var entry in source._orderedEntries)
            {
                var newEntry = new Entry(entry.KernelUri, entry.IsCompleted);
                _entriesByKernelUris.TryAdd(entry.KernelUri, entry);
                _orderedEntries.Add(newEntry);
            }
        }

    }

    public void MarkAsReceived(Uri kernelOrKernelHostUri)
    {
        var newEntry = new Entry(kernelOrKernelHostUri, false);
        if (!_entriesByKernelUris.TryAdd(kernelOrKernelHostUri, newEntry))

        {
            throw new InvalidOperationException($"The routing slip already contains {kernelOrKernelHostUri}");
        }
        else
        {
            _orderedEntries.Add(newEntry);
        }
    }

    public bool StartsWith(RoutingSlip other)
    {
        return StartsWith(other._orderedEntries.Select(e => e.KernelUri).ToArray());
    }

    public bool StartsWith(params Uri[] kernelUris)
    {
        if (kernelUris.Length > _orderedEntries.Count)
        {
            return false;
        }
        var contains = _orderedEntries.Zip(kernelUris, (o, i) => o.KernelUri.Equals(i)).All(x => x);
        return contains;
    }

    public void Append(RoutingSlip other)
    {
        throw new NotImplementedException();
    }

    public Uri[] ToUriArray()
    {
        return _orderedEntries.Select(e => e.KernelUri).ToArray();
    }

    public void MarkAsCompleted(Uri uri)
    {
        if (_entriesByKernelUris.TryGetValue(uri, out var entry))
        {
            entry.MarkAsCompleted();
        }
        else
        {
            throw new InvalidOperationException($"The routing slip does not contain {uri}");
        }
    }

    public void AddAndMarkAsCompleted(Uri uri)
    {
        MarkAsReceived(uri);
        MarkAsCompleted(uri);
    }

    class Entry
    {
        public Entry(Uri kernelUri, bool isCompleted)
        {
            KernelUri = kernelUri;
            IsCompleted = isCompleted;
        }

        public bool IsCompleted { get; private set; }

        public Uri KernelUri { get; }

        public void MarkAsCompleted()
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException($"The routing slip is already marked as completed for {KernelUri}");
            }
            IsCompleted = true;
        }
    }

    public bool Contains(Uri uri)
    {
        return _entriesByKernelUris.ContainsKey(uri);
    }
}

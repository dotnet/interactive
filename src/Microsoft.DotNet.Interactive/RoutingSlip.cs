// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip
{
    private readonly bool _requireReceivedStampBeforeComplete;
    private readonly ConcurrentDictionary<Uri, Entry> _entriesByKernelUris = new();
    private readonly List<Entry> _orderedEntries = new();


    public RoutingSlip(RoutingSlip source = null, bool requireReceivedStampBeforeComplete = true)
    {
        _requireReceivedStampBeforeComplete = requireReceivedStampBeforeComplete;
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
        InternalAddEntry(newEntry);
    }

    private void InternalAddEntry(Entry newEntry)
    {
        if (!_entriesByKernelUris.TryAdd(newEntry.KernelUri, newEntry))

        {
            throw new InvalidOperationException($"The routing slip already contains {newEntry.KernelUri}");
        }

        _orderedEntries.Add(newEntry);
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

    public void Append(RoutingSlip other, bool skipOverlapping = true)
    {
        IEnumerable<Entry> toAppend = other._orderedEntries;
        if (skipOverlapping)
        {
            if (other.StartsWith(this))
            {
                toAppend = toAppend.Skip(_orderedEntries.Count);
            }
        }
        
        foreach (var entry in toAppend)
        {
            InternalAddEntry(entry);
        }
    }

    public Uri[] ToUriArray()
    {
        return _orderedEntries.Select(e => e.KernelUri).ToArray();
    }

    public void MarkAsCompleted(Uri uri)
    {
        if (_requireReceivedStampBeforeComplete)
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
        else
        {
            var newEntry = new Entry(uri, true);
            InternalAddEntry(newEntry);
        }
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

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public abstract class RoutingSlipBase : IRoutingSlip
{
    private readonly List<Entry> _entries;

    protected ICollection<Entry> Entries => _entries;

    protected RoutingSlipBase(IRoutingSlip source = null)
    {
        _entries = source switch
        {
            { } => new List<Entry>(source.ToArray().Select(e => new Entry { Uri = new Uri(e.AbsoluteUri), Completed = true })),
            _ => new List<Entry>()
        };
    }
    
    protected class Entry
    {
        public Uri Uri { get; set; }
        public bool Completed { get; set; }
    }

    public abstract void Stamp(Uri uri);

    public Uri[] ToArray()
    {
        var entries = _entries.Where(e => e.Completed).Select(e => e.Uri).ToArray();
        return entries;
    }

    public bool StartsWith(IRoutingSlip other)
    {
        return StartsWith(other.ToArray());
    }

    public bool StartsWith(params Uri[] uris)
    {
        var startsWith = true;


        if (uris.Length <= _entries.Count)
        {
            if (_entries.Where((entry, i) => uris[i] != entry.Uri).Any())
            {
                startsWith = false;
            }
        }
        else
        {
            startsWith = false;
        }

        return startsWith;
    }

    public void Append(IRoutingSlip other)
    {
        var source = other.ToArray();
        if (other.StartsWith(this))
        {
            source = source.Skip(_entries.Count).ToArray();
        }

        foreach (var uri in source)
        {
            Stamp(uri);
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public abstract class RoutingSlip
{
    private readonly List<Entry> _entries;

    protected ICollection<Entry> Entries => _entries;

    protected RoutingSlip(RoutingSlip source = null)
    {
        _entries = source switch
        {
            { } => new List<Entry>(source.ToUriArray().Select(e => new Entry { Uri = e, Completed = true })),
            _ => new List<Entry>()
        };
    }
    
    protected class Entry
    {
        public string Uri { get; set; }
        public bool Completed { get; set; }
    }

    public abstract void Stamp(Uri uri);

    public string[] ToUriArray()
    {
        var entries = _entries.Where(e => e.Completed).Select(e => e.Uri).ToArray();
        return entries;
    }

    public bool Contains(Uri uri)
    {
        return Contains(uri.AbsoluteUri);
    }

    public bool Contains(string absoluteUri)
    {
        return _entries.Any(e => e.Uri == absoluteUri);
    }

    public bool StartsWith(RoutingSlip other)
    {
        return StartsWith(other.ToUriArray());
    }

    public bool StartsWith(params string[] absoluteUris)
    {
        var startsWith = true;

        if (absoluteUris.Length > 0 && absoluteUris.Length <= _entries.Count)
        {
            if (absoluteUris.Where((entry, i) => _entries[i].Uri != entry).Any())
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

    public bool StartsWith(params Uri[] uris)
    {
        return StartsWith(uris.Select(u => u.AbsoluteUri).ToArray());
    }

    public void ContinueWith(RoutingSlip other)
    {
        var source = other.ToUriArray();
        if (source.Length > 0)
        {
            if (other.StartsWith(this))
            {
                source = source.Skip(_entries.Count).ToArray();
            }

            foreach (var uri in source)
            {
                if (!Contains(uri))
                {
                    _entries.Add(new Entry {Uri = uri, Completed = true});
                }
                else
                {
                    throw new InvalidOperationException($"The uri {uri} is already in the routing slip");
                }
            }
        }
    }
}
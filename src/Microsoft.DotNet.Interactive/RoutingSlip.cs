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

    public abstract void Stamp(Uri uri);

    public string[] ToUriArray()
    {
        var entries = _entries.Select(e => e.AbsoluteUri).ToArray();
        return entries;
    } 

    public bool Contains(Uri uri)
    {
        return Contains(uri.AbsoluteUri);
    }

    public bool Contains(string uri)
    {
        return _entries.Any(e => e.Uri == uri);
    }

   
    public bool StartsWith(RoutingSlip other)
    {
        return StartsWith(other._entries);
    }

    public bool StartsWith(params string[] uris)
    {
        var startsWith = true;

        if (uris.Length > 0 && uris.Length <= _entries.Count)
        {
            if (uris.Where((entry, i) => _entries[i].Uri != entry).Any())
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
        return StartsWith(uris.Select(GetAbsoluteUriWithoutQuery).ToArray());
    }

    public void ContinueWith(RoutingSlip other)
    {
        var source = other._entries;
        if (source.Count > 0)
        {
            if (other.StartsWith(this))
            {
                for (var i = 0; i < _entries.Count; i++)
                {
                    if (!_entries[i].Completed)
                    {
                        _entries[i].Completed = source[i].Completed;
                    }
                }
                source = source.Skip(_entries.Count).ToList();
            }

            foreach (var entry in source)
            {

                if (!Contains(entry))
                {
                    _entries.Add(new Entry {Uri = entry.Uri, Completed = entry.Completed });
                }
                else
                {
                    throw new InvalidOperationException($"The uri {entry.Uri} is already in the routing slip");
                }
            }
        }
    }
    protected bool Contains(Entry entry)
    {
        return _entries.Any(e => e.Uri == entry.Uri);
    }

    protected bool StartsWith(List<Entry> entries)
    {
        var startsWith = true;

        if (entries.Count > 0 && entries.Count <= _entries.Count)
        {
            if (entries.Where((entry, i) => _entries[i].Uri != entry.Uri).Any())
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

    protected static string GetAbsoluteUriWithoutQuery(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;
        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            absoluteUri = absoluteUri.Replace(uri.Query, string.Empty);
        }
        return absoluteUri;
    }

    protected class Entry
    {
        private string _uri;
        private bool _completed;
        public string AbsoluteUri { get; private set; }

        public string Uri
        {
            get => _uri;
            set
            {
                _uri = value;
                Update();
            }
        }

        private void Update()
        {
            AbsoluteUri = null;
            if (!string.IsNullOrWhiteSpace(_uri))
            {
                var uriBuilder = new UriBuilder(_uri);

                if (!_completed)
                {
                    uriBuilder.Query = _completed
                        ? ""
                        : "completed=false";
                }
                AbsoluteUri = uriBuilder.Uri.AbsoluteUri;
            }

        }

        public bool Completed
        {
            get => _completed;
            set
            {
                _completed = value;
                Update();

            }
        }
    }

}
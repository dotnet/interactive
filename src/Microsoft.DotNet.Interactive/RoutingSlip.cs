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
            { } => new List<Entry>(source.ToUriArray().Select(e => new Entry { Uri = e })),
            _ => new List<Entry>()
        };
    }

    public abstract void Stamp(Uri uri);

    public string[] ToUriArray()
    {
        var entries = _entries.Select(e => e.AbsoluteUri).ToArray();
        return entries;
    } 

    public bool Contains(Uri uri) => Contains(uri.AbsoluteUri);

    public bool Contains(string uri) => _entries.Any(e => e.AbsoluteUri == uri);
    
    public bool StartsWith(RoutingSlip other) => StartsWith(other._entries);

    public bool StartsWith(params string[] uris)
    {
        var startsWith = true;

        if (uris.Length > 0 && uris.Length <= _entries.Count)
        {
            if (uris.Where((entry, i) => _entries[i].AbsoluteUri != entry).Any())
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

    public bool StartsWith(params Uri[] uris) => StartsWith(uris.Select(u => u.AbsoluteUri).ToArray());

    public void ContinueWith(RoutingSlip other)
    {
        var source = other._entries;
        if (source.Count > 0)
        {
            if (other.StartsWith(this))
            {
                source = source.Skip(_entries.Count).ToList();
            }

            foreach (var entry in source)
            {

                if (!Contains(entry))
                {
                    _entries.Add(new Entry {Uri = entry.Uri, Tag = entry.Tag });
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
        return _entries.Any(e => e.AbsoluteUri == entry.AbsoluteUri);
    }

    protected bool StartsWith(List<Entry> entries)
    {
        var startsWith = true;

        if (entries.Count > 0 && entries.Count <= _entries.Count)
        {
            if (entries.Where((entry, i) => _entries[i].AbsoluteUri != entry.AbsoluteUri).Any())
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
        private string _tag;
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

                if (!string.IsNullOrWhiteSpace(_tag))
                {
                    uriBuilder.Query = $"tag={_tag}";
                }
               
                AbsoluteUri = uriBuilder.Uri.AbsoluteUri;
            }

        }

        public string Tag
        {
            get => _tag;
            set
            {
                _tag = value;
                Update();
            }
        }
    }
}
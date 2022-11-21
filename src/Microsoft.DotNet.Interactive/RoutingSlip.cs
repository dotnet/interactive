// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public abstract class RoutingSlip
{
    private readonly List<Entry> _entries = new();

    private protected ICollection<Entry> Entries => _entries;

    public abstract void Stamp(Uri uri);

    public string[] ToUriArray()
    {
        var entries = _entries.Select(e => e.AbsoluteUriWithQuery).ToArray();
        return entries;
    }   

    public int Count => _entries.Count;

    public bool Contains(Uri uri, bool ignoreQuery = false)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        return ignoreQuery
                   ? ContainsUriWithoutQuery(GetAbsoluteUriWithoutQuery(uri))
                   : Contains(uri.AbsoluteUri);
    }

    private bool Contains(string uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.AbsoluteUriWithQuery == uri) return true;
        }

        return false;
    }

    private bool ContainsUriWithoutQuery(string uriWithoutQuery)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.AbsoluteUriWithoutQuery == uriWithoutQuery) return true;
        }

        return false;
    }

    public bool StartsWith(RoutingSlip other) => StartsWith(other._entries);

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
                    _entries.Add(new Entry(entry.AbsoluteUriWithoutQuery, entry.Tag));
                }
                else
                {
                    throw new InvalidOperationException($"The uri {entry.AbsoluteUriWithoutQuery} is already in the routing slip");
                }
            }
        }
    }

    private protected bool Contains(Entry entry)
    {
        return _entries.Any(e => e.AbsoluteUriWithQuery == entry.AbsoluteUriWithQuery);
    }

    private protected bool StartsWith(List<Entry> entries)
    {
        var startsWith = true;

        if (entries.Count > 0 && entries.Count <= _entries.Count)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (_entries[i].AbsoluteUriWithQuery != entries[i].AbsoluteUriWithQuery)
                {
                    startsWith = false;
                    break;
                }
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

    private protected class Entry
    {
        public Entry(string absoluteUriWithoutQuery, string tag = null)
        {
            if (string.IsNullOrWhiteSpace(absoluteUriWithoutQuery))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(absoluteUriWithoutQuery));
            }

            AbsoluteUriWithoutQuery = GetAbsoluteUriWithoutQuery(new Uri(absoluteUriWithoutQuery, UriKind.Absolute));

            Tag = tag;

            var uriBuilder = new UriBuilder(AbsoluteUriWithoutQuery);

            if (!string.IsNullOrWhiteSpace(Tag))
            {
                uriBuilder.Query = $"tag={Tag}";
            }

            AbsoluteUriWithQuery = uriBuilder.Uri.AbsoluteUri;
        }

        public string AbsoluteUriWithoutQuery { get; }

        public string Tag { get; }

        public string AbsoluteUriWithQuery { get; }

        public override string ToString() => AbsoluteUriWithQuery;
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

[DebuggerStepThrough]
[TypeFormatterSource(typeof(MessageDiagnosticsFormatterSource))]
public abstract class RoutingSlip
{
    private readonly List<Entry> _entries = new();
    internal ICollection<Entry> Entries => _entries;

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

    public bool StartsWith(RoutingSlip other)
    {
        if (other._entries.Count > 0 && other._entries.Count <= _entries.Count)
        {
            for (int i = 0; i < other._entries.Count; i++)
            {
                if (_entries[i].AbsoluteUriWithQuery != other._entries[i].AbsoluteUriWithQuery)
                {
                    return false;
                }
            }
        }
        else
        {
            return false;
        }

        return true;
    }

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

    public override string ToString()
    {
        using var writer = new StringWriter();
        writer.Write("[");

        var i = 0;
        foreach (var entry in Entries)
        {
            writer.Write(entry.AbsoluteUriWithoutQuery);

            if (!string.IsNullOrEmpty(entry.Tag))
            {
                writer.Write(" (");
                writer.Write(entry.Tag);
                writer.Write(")");
            }

            if (++i != Entries.Count)
            {
                writer.Write(", ");
            }
        }

        writer.Write("]");
        return writer.ToString();
    }

    private bool Contains(Entry entry)
    {
        return _entries.Any(e => e.AbsoluteUriWithQuery == entry.AbsoluteUriWithQuery);
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

    internal class Entry
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

        public override string ToString() =>
            Tag is not null
                ? $"{AbsoluteUriWithoutQuery} ({Tag})"
                : $"{AbsoluteUriWithoutQuery}";
    }
}

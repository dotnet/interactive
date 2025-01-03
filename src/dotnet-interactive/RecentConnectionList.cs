// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.App.Connection;

namespace Microsoft.DotNet.Interactive.App;

[JsonConverter(typeof(RecentConnectionListConverter))]
public class RecentConnectionList : ICollection<ConnectionShortcut>
{
    internal const int DefaultCapacity = 5;

    private readonly List<ConnectionShortcut> _list = new();

    public int Capacity { get; init; } = DefaultCapacity;

    public int Count => _list.Count;

    public bool IsReadOnly => false;

    public void Add(ConnectionShortcut value)
    {
        if (_list.FirstOrDefault(item => CodeIsEquivalent(item.ConnectCode, value.ConnectCode)) is {  } duplicate)
        {
            // move the duplicate to the top of the list
            _list.Remove(duplicate);
            _list.Insert(0, duplicate);
        }
        else
        {
            if (Count == Capacity)
            {
                _list.RemoveAt(Count - 1);
            }

            _list.Insert(0, value);
        }
    }

    private bool CodeIsEquivalent(
        IReadOnlyList<string> first, 
        IReadOnlyList<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (first[i].Trim() != second[i].Trim())
            {
                return false;
            }
        }

        return true;
    }

    public void Clear()
    {
        _list.Clear();
    }

    public IEnumerator<ConnectionShortcut> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(ConnectionShortcut item)
    {
        return _list.Contains(item);
    }

    public void CopyTo(ConnectionShortcut[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public bool Remove(ConnectionShortcut item)
    {
        return _list.Remove(item);
    }
}
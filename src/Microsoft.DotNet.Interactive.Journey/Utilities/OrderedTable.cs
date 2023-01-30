// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Journey.Utilities;

public class OrderedTable<T> : ICollection<T>
{
    private readonly IDictionary<T, LinkedListNode<T>> _Dictionary;
    private readonly LinkedList<T> _LinkedList;

    public OrderedTable()
        : this(EqualityComparer<T>.Default)
    {
    }

    public OrderedTable(IEqualityComparer<T> comparer)
    {
        _Dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
        _LinkedList = new LinkedList<T>();
    }

    public int Count => _Dictionary.Count;

    public virtual bool IsReadOnly => _Dictionary.IsReadOnly;

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    public bool Add(T item)
    {
        if (_Dictionary.ContainsKey(item)) return false;
        var node = _LinkedList.AddLast(item);
        _Dictionary.Add(item, node);
        return true;
    }

    public void Clear()
    {
        _LinkedList.Clear();
        _Dictionary.Clear();
    }

    public bool Remove(T item)
    {
        if (item == null) return false;
        var found = _Dictionary.TryGetValue(item, out var node);
        if (!found) return false;
        _Dictionary.Remove(item);
        _LinkedList.Remove(node);
        return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _LinkedList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(T item)
    {
        return item != null && _Dictionary.ContainsKey(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _LinkedList.CopyTo(array, arrayIndex);
    }
}
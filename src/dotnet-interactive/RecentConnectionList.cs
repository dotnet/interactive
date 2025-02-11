// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.App;

[JsonConverter(typeof(RecentConnectionListConverter))]
public class RecentConnectionList : ICollection<CodeExpansion>
{
    internal const int DefaultCapacity = 10;

    private readonly List<CodeExpansion> _list = new();
    private int _capacity = DefaultCapacity;

    public int Capacity
    {
        get => _capacity;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value < _list.Count)
            {
                _list.RemoveRange(value, _list.Count - value);
            }

            _capacity = value;
        }
    }

    public int Count => _list.Count;

    public bool IsReadOnly => false;

    public void Add(CodeExpansion value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (_list.FirstOrDefault(item => IsEquivalent(item, value)) is {  } duplicate)
        {
            _list.Remove(duplicate);
            _list.Insert(0, value);
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

    private bool IsEquivalent(
        CodeExpansion first,
        CodeExpansion second)
    {
        if (first.Info.Name.Equals(second.Info.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (first.Content.Count != second.Content.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Content.Count; i++)
        {
            if (first.Content[i].TargetKernelName != second.Content[i].TargetKernelName)
            {
                return false;
            }

            if (first.Content[i].Code.Trim() != second.Content[i].Code.Trim())
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

    public IEnumerator<CodeExpansion> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(CodeExpansion item)
    {
        return _list.Contains(item);
    }

    public void CopyTo(CodeExpansion[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public bool Remove(CodeExpansion item)
    {
        return _list.Remove(item);
    }
}
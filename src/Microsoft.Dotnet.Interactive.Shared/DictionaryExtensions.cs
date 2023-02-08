// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Utility;

internal static partial class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> getValue)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = getValue(key);
            dictionary.Add(key, value);
        }

        return value;
    }

    public static TValue GetOrAdd<TValue>(
        this IDictionary<string, object> dictionary,
        string key,
        Func<string, TValue> getValue)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = getValue(key);
            dictionary.Add(key, value);
        }

        return (TValue)value;
    }

#if NETSTANDARD2_0
    public static bool TryAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> source, 
        TKey key,
        TValue value)
    {
        if (source.TryGetValue(key, out _))
        {
            return false;
        }
        else
        {
            source.Add(key, value);
            return true;
        }
    }
#endif

    public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary1,
        IDictionary<TKey, TValue> dictionary2,
        bool replace = false,
        IEqualityComparer<TKey> comparer = null)
    {
        IDictionary<TKey, TValue> result;
        if (comparer is null)
        {
            result = new Dictionary<TKey, TValue>();
        }
        else
        {
            result = new Dictionary<TKey, TValue>(comparer);
        }

        var first = dictionary2;
        var second = dictionary1;

        if (replace)
        {
            first = dictionary1;
            second = dictionary2;
        }

        foreach (var p in first)
        {
            result[p.Key] = p.Value;
        }

        foreach (var p in second)
        {
            result[p.Key] = p.Value;
        }

        return result;
    }

    public static void MergeWith<TKey, TValue>(
        this IDictionary<TKey, TValue> target,
        IDictionary<TKey, TValue> source,
        bool replace = false)
    {
        foreach (var pair in source)
        {
            if (replace || !target.ContainsKey(pair.Key))
            {
                target[pair.Key] = pair.Value;
            }
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents.Utility;

internal static partial class DictionaryExtensions
{
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
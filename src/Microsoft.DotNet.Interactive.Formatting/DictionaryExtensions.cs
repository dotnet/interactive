// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary1,
            IDictionary<TKey, TValue> dictionary2,
            bool replace = false,
            IEqualityComparer<TKey> comparer = null)
        {
            IDictionary<TKey, TValue> result;
            if (comparer == null)
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
}
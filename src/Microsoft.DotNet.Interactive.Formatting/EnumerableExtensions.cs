// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class EnumerableExtensions
    {
        public static (IReadOnlyList<T> items, int? remainingCount) TakeAndCountRemaining<T>(
            this IEnumerable<T> source,
            int count,
            bool forceCountRemainder = false)
        {
            using var enumerator = source.GetEnumerator();

            var items = new List<T>();

            int? remainingCount = null;

            while (enumerator.MoveNext())
            {
                items.Add(enumerator.Current);

                if (items.Count >= count)
                {
                    if (forceCountRemainder)
                    {
                        remainingCount = source.Count() - items.Count;
                    }
                    else
                    {
                        remainingCount = source switch
                        {
                            ICollection collection => collection.Count - items.Count,
                            ICollection<T> collection => collection.Count - items.Count,
                            _ => null
                        };
                    }

                    return (items, remainingCount);
                }
            }

            return (items, 0);
        }
    }
}
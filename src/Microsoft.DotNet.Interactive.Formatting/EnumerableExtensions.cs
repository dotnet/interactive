// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class EnumerableExtensions
    {
        public static (IReadOnlyList<T> items, int remainingCount) TakeAndCountRemaining<T>(
            this IEnumerable<T> source,
            int count)
        {
            using var enumerator = source.GetEnumerator();

            var items = new List<T>();

            while (enumerator.MoveNext())
            {
                items.Add(enumerator.Current);

                if (items.Count >= count)
                {
                    return (items, CountRemaining());
                }
            }

            return (items, 0);

            int CountRemaining()
            {
                switch (source)
                {
                    case ICollection<T> collection:
                        return collection.Count - items.Count;
                    default:

                        var remainingCount = 0;

                        while (enumerator.MoveNext())
                        {
                            remainingCount++;
                        }

                        return remainingCount;
                }
            }
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.App.Tests.Extensions;

public static class EnumerableExtensions
{
    internal static IEnumerable<T> FlattenBreadthFirst<T>(
        this IEnumerable<T> source,
        Func<T, IEnumerable<T>> children)
    {
        var queue = new Queue<T>();

        foreach (var item in source)
        {
            queue.Enqueue(item);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var option in children(current))
            {
                queue.Enqueue(option);
            }

            yield return current;
        }
    }
}
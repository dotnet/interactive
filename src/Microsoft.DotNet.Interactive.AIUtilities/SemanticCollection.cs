// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class EnumerableSearchExtensions{
    public static IEnumerable<KeyValuePair<T, float>> OrderBySimilarity<T>(this IEnumerable<T> source, T query,
        ISimilarityComparer<T> comparer)
    {
        return source.Select(item => new KeyValuePair<T, float>(item, comparer.Score(item, query)))
            .OrderByDescending(entry => entry.Value);
    }
}
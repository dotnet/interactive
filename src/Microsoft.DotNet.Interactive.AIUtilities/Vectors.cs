// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class Vectors
{
    public static float[] Centroid(this IEnumerable<float[]> vectors)
    {
        var size = vectors.First().Length;

        var accumulated = vectors.Aggregate((Enumerable.Repeat<float>(0f, size), 0), (acc, d) => (acc.Item1.Zip(d, (a, b) => a + b).ToArray(), acc.Item2 + 1));

        return accumulated.Item1.Select(e => e/ accumulated.Item2).ToArray();
    }
}
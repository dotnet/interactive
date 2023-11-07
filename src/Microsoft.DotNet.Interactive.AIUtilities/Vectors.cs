// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Reactive.Linq;
using Pocket;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class Vectors
{
    static Vectors()
    {
        _logger = new Logger(typeof(Text).FullName);
    }

    private static readonly Logger _logger;

    public static float[] Centroid(this IEnumerable<float[]> vectors, Func<float[], float>? weight = null)
    {
        _logger.Event();
        var size = vectors.First().Length;

        var accumulated = vectors.Aggregate((Enumerable.Repeat(0f, size), 0), (acc, d) =>
        {
            var w = weight?.Invoke(d) ?? 1 ;
            return (acc.Item1.Zip(d, (a, b) => a * w + b).ToArray(), acc.Item2 + 1);
        });

        return accumulated.Item1.Select(e => e/ accumulated.Item2).ToArray();
    }

    public static IObservable<float[]> Centroid(this IObservable<float[]> vectors, int vectorSize)
    {
        _logger.Event();
        var acc = Enumerable.Repeat(0f, vectorSize).ToArray();
        var sampled = 0;
        return vectors.Select(vector =>
        {
            sampled++; 
            acc = acc.Zip(vector, (a, v) => ((a * (sampled - 1)) + v) / sampled).ToArray();

            return acc;

        });
    }
}
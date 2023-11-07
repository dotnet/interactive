// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pocket;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class Sampling
{
    static Sampling()
    {
        _logger = new Logger(typeof(Text).FullName);
    }

    private static readonly Logger _logger;

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        _logger.Event();
        var rnd = new Random();
        var list = new List<T>(source);
        while (list.Count > 0)
        {
            var pos = rnd.Next(list.Count);
            var sample = list.ElementAt(pos);
            list.RemoveAt(pos);
            yield return sample;
        }
    }
}
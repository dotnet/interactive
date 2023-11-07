// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Microsoft.DeepDev;
using System.Reactive.Linq;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class Text {

    public static IEnumerable<KeyValuePair<T, float>> ScoreBySimilarityTo<T>(this IEnumerable<T> source, T value,
        ISimilarityComparer<T> comparer)
    {
        return source.Select(item => new KeyValuePair<T, float>(item, comparer.Score(item, value)));
    }

    public static IEnumerable<KeyValuePair<TCollection, float>> ScoreBySimilarityTo<TCollection, TValue>(this IEnumerable<TCollection> source, TValue value,
        ISimilarityComparer<TValue> comparer, Func<TCollection, TValue> valueSelector)
    {
        return source.Select(item => new KeyValuePair<TCollection, float>(item, comparer.Score(valueSelector(item), value)));
    }

    public static IObservable<string> ChunkByTokenCountWithOverlap(this IObservable<string> source, ITokenizer tokenizer, int maxTokenCount, int overlapTokenCount)
    {
        if (maxTokenCount <= overlapTokenCount)
        {
            throw new ArgumentException($"Cannot be greater or equal to {nameof(maxTokenCount)}",
                nameof(overlapTokenCount));
        }

        return source.SelectMany(text =>
        {
            return Observable.Create<string>(o =>
            {
                var chunks = tokenizer.ChunkByTokenCountWithOverlap(text, maxTokenCount, overlapTokenCount);
                foreach (var chunk in chunks)
                {
                    o.OnNext(chunk);
                }

                o.OnCompleted();
                return Disposable.Empty;
            });

        });
    }

    public static IObservable<string> ChunkByTokenCount(this IObservable<string> source, ITokenizer tokenizer, int maxTokenCount)
    {
        return source.ChunkByTokenCountWithOverlap(tokenizer, maxTokenCount, 0);
    }
}
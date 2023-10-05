// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.DeepDev;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class Observable
{
    public static IObservable<string> ChunkByTokenCountWithOverlap(this IObservable<string> source, ITokenizer tokenizer, int maxTokenCount, int overlapTokenCount)
    {
        if (maxTokenCount <= overlapTokenCount)
        {
            throw new ArgumentException($"Cannot be greater or equal to {nameof(maxTokenCount)}",
                nameof(overlapTokenCount));
        }

        return source.SelectMany(text =>
        {
            return System.Reactive.Linq.Observable.Create<string>(o =>
            {
                var chunks = tokenizer.ChunkByTokenCountWithOverlap(text ,maxTokenCount, overlapTokenCount);
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
        return source.ChunkByTokenCountWithOverlap(tokenizer, maxTokenCount,0 );
    } 
}


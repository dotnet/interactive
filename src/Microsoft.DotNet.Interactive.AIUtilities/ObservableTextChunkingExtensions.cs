// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class ObservableTextChunkingExtensions
{
    public static IObservable<string> ChunkByTokenCountWithOverlapAsync(this IObservable<string> source, int maxTokenCount, int overlapTokenCount, TokenizerModel model)
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
                var chunks = text.ChunkByTokenCountWithOverlapAsync(maxTokenCount, overlapTokenCount, model).GetAwaiter().GetResult();
                foreach (var chunk in chunks)
                {
                    o.OnNext(chunk);
                }

                o.OnCompleted();
                return Disposable.Empty;
            });

        });
    }

    public static IObservable<string> ChunkByTokenCountAsync(this IObservable<string> source, int maxTokenCount, TokenizerModel model)
    {
        return source.SelectMany(text =>
        {
            return Observable.Create<string>( o =>
            {
                var chunks = text.ChunkByTokenCountAsync(maxTokenCount, model).GetAwaiter().GetResult();
                foreach (var chunk in chunks)
                {
                    o.OnNext(chunk);
                }

                o.OnCompleted();
                return Disposable.Empty;
            });
        });
    }
}


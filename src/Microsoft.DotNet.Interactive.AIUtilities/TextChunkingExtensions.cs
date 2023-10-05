// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DeepDev;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class TextChunkingExtensions
{
    public static async Task<int> GetTokenCountAsync(this string text, TokenizerModel model)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var tokenizer = model switch
        {
            TokenizerModel.ada2 => await TokenizerBuilder.CreateByModelNameAsync("text-embedding-ada-002"),
            TokenizerModel.gpt35 => await TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo"),
            TokenizerModel.gpt4 => await TokenizerBuilder.CreateByModelNameAsync("gpt4"),
            _ => throw new NotSupportedException()
        };

        var encoded = tokenizer.Encode(text, Array.Empty<string>()).ToArray();

        return encoded.Length;
    }

    public static IEnumerable<string> ChunkWithOverlap(this string text, int maxChunkSize, int overlapSize)
    {
        if (maxChunkSize <= overlapSize)
        {
            throw new ArgumentException($"Cannot be greater or equal to {nameof(maxChunkSize)}", nameof(overlapSize));
        }

        var start = 0;
        while (start < text.Length - overlapSize)
        {
            var size = Math.Min(text.Length - start, maxChunkSize);
            yield return text.Substring(start, size);
            start += (maxChunkSize - overlapSize);

        }
    }

    public static Task<IEnumerable<string>> ChunkByTokenCountAsync(this string text, int maxTokenCount,
        TokenizerModel model, bool average = false)
    {
        return text.ChunkByTokenCountWithOverlapAsync(maxTokenCount, 0, model, average);
    }

    public static async Task<IEnumerable<string>> ChunkByTokenCountWithOverlapAsync(this string text, int maxTokenCount,
        int overlapTokenCount, TokenizerModel model, bool average = false)
    {
        if (maxTokenCount <= overlapTokenCount)
        {
            throw new ArgumentException($"Cannot be greater or equal to {nameof(maxTokenCount)}",
                nameof(overlapTokenCount));
        }

        var tokenizer = model switch
        {
            TokenizerModel.ada2 => await TokenizerBuilder.CreateByModelNameAsync("text-embedding-ada-002"),
            TokenizerModel.gpt35 => await TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo"),
            TokenizerModel.gpt4 => await TokenizerBuilder.CreateByModelNameAsync("gpt4"),
            _ => throw new NotSupportedException()
        };
        var chunkSize = maxTokenCount;
        var chunkOverlapSize = overlapTokenCount;
        var encoded = tokenizer.Encode(text, Array.Empty<string>()).ToArray();

        var chunks = new List<string>();
        var encodedChucks = new List<int[]>();
        var start = 0;

        while (start < encoded.Length - overlapTokenCount)
        {
            var size = Math.Min(encoded.Length - start, chunkSize);
            encodedChucks.Add(encoded[start..(start + size)]);
            start += (chunkSize - chunkOverlapSize);
        }

        if (average)
        {
            var averageChunk = encodedChucks.Sum(x => x.Length) / (double)(encodedChucks.Count);
            start = 0;
            chunkSize = (int)Math.Ceiling(averageChunk);
            chunkOverlapSize = Math.Min(chunkOverlapSize, chunkSize - 1);
            encodedChucks = new List<int[]>();
            while (start < encoded.Length - chunkOverlapSize)
            {
                var size = Math.Min(encoded.Length - start, chunkSize);
                encodedChucks.Add(encoded[start..(start + size)]);
                start += (chunkSize - chunkOverlapSize);
            }
        }

        chunks.AddRange(encodedChucks.Select(tokenizer.Decode));

        return chunks;
    }
}
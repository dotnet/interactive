// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DeepDev;
using Pocket;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class Tokenizer
{
    static Tokenizer()
    {
        _logger = new Logger(typeof(Text).FullName);
    }

    private static readonly Logger _logger;

    public static int GetTokenCount(this ITokenizer tokenizer, string text)
    {
        _logger.Event();
        var encoded = tokenizer.Encode(text, Array.Empty<string>()).ToArray();
        return encoded.Length;
    }

    public static async Task<ITokenizer> CreateAsync(TokenizerModel model)
    {
        _logger.Event(properties: ("model", model));

        var tokenizer = model switch
        {
            TokenizerModel.ada2 => await TokenizerBuilder.CreateByModelNameAsync("text-embedding-ada-002"),
            TokenizerModel.gpt35 => await TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo"),
            TokenizerModel.gpt4 => await TokenizerBuilder.CreateByModelNameAsync("gpt4"),
            _ => throw new ArgumentOutOfRangeException($"{model}")
        };
        return tokenizer;
    }

    public static string TruncateByTokenCount(this ITokenizer tokenizer,  string text, int tokenCount)
    {
        _logger.Event();
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var encoded = tokenizer.Encode(text, Array.Empty<string>()).ToArray();
        return tokenizer.Decode(encoded.Take(tokenCount).ToArray());
    }

    public static IEnumerable<string> ChunkWithOverlap(this string text, int maxChunkSize, int overlapSize)
    {
        _logger.Event();
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

    public static IEnumerable<string> ChunkByTokenCount(this ITokenizer tokenizer, string text, int maxTokenCount,
        bool average = false)
    {
        return tokenizer.ChunkByTokenCountWithOverlap(text, maxTokenCount, 0,  average);
    }

    public static IEnumerable<string> ChunkByTokenCountWithOverlap(this ITokenizer tokenizer, string text, int maxTokenCount,
        int overlapTokenCount,  bool average = false)
    {
        _logger.Event();

        if (maxTokenCount <= overlapTokenCount)
        {
            throw new ArgumentException($"Cannot be greater or equal to {maxTokenCount}",
                nameof(overlapTokenCount));
        }

        var chunkSize = maxTokenCount;
        var chunkOverlapSize = overlapTokenCount;
        var encoded = tokenizer.Encode(text, Array.Empty<string>()).ToArray();

        var chunks = new List<string>();
        var encodedChucks = new List<int[]>();
        var start = 0;

        ExtractEncodedChunks();

        if (average)
        {
            var averageChunk = encodedChucks.Sum(x => x.Length) / (double)(encodedChucks.Count);
            var newChunkSize = (int)Math.Ceiling(averageChunk);
            var newChunkOverlapSize = Math.Min(chunkOverlapSize, newChunkSize - 1);

            if (newChunkSize != chunkSize || newChunkOverlapSize != chunkOverlapSize)
            {
                start = 0;
                chunkSize = newChunkSize;
                chunkOverlapSize = newChunkOverlapSize;
                encodedChucks = new List<int[]>();
                ExtractEncodedChunks();
            }
        }

        chunks.AddRange(encodedChucks.Select(tokenizer.Decode));

        return chunks;

        void ExtractEncodedChunks()
        {
            while (start < encoded.Length - chunkOverlapSize)
            {
                var size = Math.Min(encoded.Length - start, chunkSize);
                encodedChucks.Add(encoded[start..(start + size)]);
                start += (chunkSize - chunkOverlapSize);
            }
        }
    }
}
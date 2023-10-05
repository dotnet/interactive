// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Reactive.Subjects;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class TextChunkingTests
{
    private const string LongText = """
        Call me Ishmael. Some years ago—never mind how long precisely—having little or no money in my purse, and nothing particular to interest me on shore, I thought I would sail about a little and see the watery part of the world. It is a way I have of driving off the spleen, and regulating the circulation. Whenever I find myself growing grim about the mouth; whenever it is a damp, drizzly November in my soul; whenever I find myself involuntarily pausing before coffin warehouses, and bringing up the rear of every funeral I meet; and especially whenever my hypos get such an upper hand of me, that it requires a strong moral principle to prevent me from deliberately stepping into the street, and methodically knocking people’s hats off—then, I account it high time to get to sea as soon as I can. This is my substitute for pistol and ball. With a philosophical flourish Cato throws himself upon his sword; I quietly take to the ship. There is nothing surprising in this. If they but knew it, almost all men in their degree, some time or other, cherish very nearly the same feelings towards the ocean with me.
        """;

    [Fact]
    public async Task can_calculate_token_count()
    {
        var tokenCount = await LongText.GetTokenCountAsync(TokenizerModel.ada2);
        tokenCount.Should().Be(239);
    }

    [Fact]
    public void can_create_overlapping_chunks_by_size()
    {
        var text = "abcdefghijklmnopqrs";
        var chunks = text.ChunkWithOverlap(maxChunkSize: 10, overlapSize: 5);
        chunks.Should().BeEquivalentTo(new[]
        {
            "abcdefghij",
            "fghijklmno",
            "klmnopqrs"
        });
    }

    [Fact]
    public async Task can_create_chunks_by_token_count()
    {
        var chunks = await LongText.ChunkByTokenCountAsync(maxTokenCount: 10, model: TokenizerModel.ada2);
        chunks.Should().BeEquivalentTo(new[]
        {
            "Call me Ishmael. Some years ago—", "never mind how long precisely—having little or no",
            " money in my purse, and nothing particular to interest", " me on shore, I thought I would sail about", 
            " a little and see the watery part of the", " world. It is a way I have of driving", 
            " off the spleen, and regulating the circulation.", 
            " Whenever I find myself growing grim about the mouth;", 
            " whenever it is a damp, drizzly November in", " my soul; whenever I find myself involuntarily", 
            " pausing before coffin warehouses, and bringing up the", 
            " rear of every funeral I meet; and especially whenever", 
            " my hypos get such an upper hand of me", 
            ", that it requires a strong moral principle to prevent", 
            " me from deliberately stepping into the street, and method", 
            "ically knocking people’s hats off—then, I", 
            " account it high time to get to sea as soon", 
            " as I can. This is my substitute for pistol", 
            " and ball. With a philosophical flourish Cato throws", 
            " himself upon his sword; I quietly take to the", 
            " ship. There is nothing surprising in this. If", 
            " they but knew it, almost all men in their", 
            " degree, some time or other, cherish very nearly", 
            " the same feelings towards the ocean with me."
        });
    }

    [Fact]
    public async Task can_create_overlapping_chunks_by_token_count()
    {
        var chunks = await LongText.ChunkByTokenCountWithOverlapAsync(maxTokenCount: 80, overlapTokenCount: 10, model: TokenizerModel.ada2);
        chunks.Should().BeEquivalentTo(new[]
        {
            "Call me Ishmael. Some years ago—never mind how long precisely—having little or no money in my purse, and nothing particular to interest me on shore, I thought I would sail about a little and see the watery part of the world. It is a way I have of driving off the spleen, and regulating the circulation. Whenever I find myself growing grim about the mouth;", 
            " Whenever I find myself growing grim about the mouth; whenever it is a damp, drizzly November in my soul; whenever I find myself involuntarily pausing before coffin warehouses, and bringing up the rear of every funeral I meet; and especially whenever my hypos get such an upper hand of me, that it requires a strong moral principle to prevent me from deliberately stepping into the street, and method", 
            " me from deliberately stepping into the street, and methodically knocking people’s hats off—then, I account it high time to get to sea as soon as I can. This is my substitute for pistol and ball. With a philosophical flourish Cato throws himself upon his sword; I quietly take to the ship. There is nothing surprising in this. If they but knew it, almost all men in their",
            " they but knew it, almost all men in their degree, some time or other, cherish very nearly the same feelings towards the ocean with me."
        });
    }

    [Fact]
    public void can_create_chunks_by_token_count_from_observable()
    {
        var stream = new ReplaySubject<string>();

        var chunks = new List<string>();

        stream.OnNext(LongText);

        using var _ = stream.ChunkByTokenCountAsync(maxTokenCount: 10,  model: TokenizerModel.ada2).Subscribe(chunks.Add);
        chunks.Should().BeEquivalentTo(new[]
        {
            "Call me Ishmael. Some years ago—", "never mind how long precisely—having little or no",
            " money in my purse, and nothing particular to interest", " me on shore, I thought I would sail about",
            " a little and see the watery part of the", " world. It is a way I have of driving",
            " off the spleen, and regulating the circulation.",
            " Whenever I find myself growing grim about the mouth;",
            " whenever it is a damp, drizzly November in", " my soul; whenever I find myself involuntarily",
            " pausing before coffin warehouses, and bringing up the",
            " rear of every funeral I meet; and especially whenever",
            " my hypos get such an upper hand of me",
            ", that it requires a strong moral principle to prevent",
            " me from deliberately stepping into the street, and method",
            "ically knocking people’s hats off—then, I",
            " account it high time to get to sea as soon",
            " as I can. This is my substitute for pistol",
            " and ball. With a philosophical flourish Cato throws",
            " himself upon his sword; I quietly take to the",
            " ship. There is nothing surprising in this. If",
            " they but knew it, almost all men in their",
            " degree, some time or other, cherish very nearly",
            " the same feelings towards the ocean with me."
        });
    }

    [Fact]
    public void can_create_overlapping_chunks_by_token_count_from_observable()
    {
        var stream = new ReplaySubject<string>();

        var chunks = new List<string>();

        stream.OnNext(LongText);

        using var _ = stream.ChunkByTokenCountWithOverlapAsync(maxTokenCount: 80, overlapTokenCount: 10, model: TokenizerModel.ada2).Subscribe(chunks.Add);
        chunks.Should().BeEquivalentTo(new[]
        {
            "Call me Ishmael. Some years ago—never mind how long precisely—having little or no money in my purse, and nothing particular to interest me on shore, I thought I would sail about a little and see the watery part of the world. It is a way I have of driving off the spleen, and regulating the circulation. Whenever I find myself growing grim about the mouth;",
            " Whenever I find myself growing grim about the mouth; whenever it is a damp, drizzly November in my soul; whenever I find myself involuntarily pausing before coffin warehouses, and bringing up the rear of every funeral I meet; and especially whenever my hypos get such an upper hand of me, that it requires a strong moral principle to prevent me from deliberately stepping into the street, and method",
            " me from deliberately stepping into the street, and methodically knocking people’s hats off—then, I account it high time to get to sea as soon as I can. This is my substitute for pistol and ball. With a philosophical flourish Cato throws himself upon his sword; I quietly take to the ship. There is nothing surprising in this. If they but knew it, almost all men in their",
            " they but knew it, almost all men in their degree, some time or other, cherish very nearly the same feelings towards the ocean with me."
        });
    }
}
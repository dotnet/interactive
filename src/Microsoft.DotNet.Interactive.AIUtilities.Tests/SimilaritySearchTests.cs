// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class SimilaritySearchTests
{
    [Fact]
    public void can_search_collection_for_similar_element()
    {
        var collection = new List<string>
        {
            "diugo",
            "diago",
            "carlo",
            "chuck"
        };

        var search = collection.ScoreBySimilarity(
            "diego",
            new CosineSimilarityComparer<string>(a => a.Select(c => (float)c).ToArray())
        )
            .OrderByDescending(e => e.Value)
            .Take(1);

        search.Should().BeEquivalentTo(new [] { KeyValuePair.Create("diago", 0.9998783f) });
    }

    [Fact]
    public void can_search_collection_for_similar_element_f()
    {
        var vectorCollection = new List<float[]>
        {
            new []{1f,0f},
            new []{1f,1.5f},
            new []{1f,1.7f},
            new []{1f,1.9f}
        };
        var search = vectorCollection.ScoreBySimilarity(
            new[] { 1f, 1f },
            new CosineSimilarityComparer<float[]>(t => t)
        ).OrderByDescending(e => e.Value)
            .Take(2);

        var expected = new[]
        {
            KeyValuePair.Create(  new  [] { 1f,1.5f },  0.9805807f ),
            KeyValuePair.Create( new[] { 1f,1.7f },  0.9679969f )
        };

        search.Should().BeEquivalentTo(expected);
    }
}
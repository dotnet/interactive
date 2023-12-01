// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Tests;
using Microsoft.DotNet.Interactive.Telemetry;

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

        var search = collection.ScoreBySimilarityTo(
            "diego",
            new CosineSimilarityComparer<string>(a => a.Select(c => (float)c).ToArray())
        )
            .OrderByDescending(e => e.Score)
            .Take(1);

        search.Should().BeEquivalentTo(new [] { ScoredValue.Create("diago", 0.9998783f) });
    }

    [Fact]
    public void can_search_collection_for_element_similar_to_value()
    {
        var collection = new[]
        {
           new { Text =  "diugo", Number = 1.0f},
           new { Text = "diago",  Number = 1.0f},
           new { Text = "carlo", Number = 1.0f }, 
           new { Text = "chuck",  Number = 1.0f }
        };

        var search = collection.ScoreBySimilarityTo(
                "diego",
                new CosineSimilarityComparer<string>(a => a.Select(c => (float)c).ToArray()),
                        e => e.Text
            )
            .OrderByDescending(e => e.Score)
            .Take(1);

        search.Should().BeEquivalentTo(new[] { ScoredValue.Create(new { Text = "diago", Number = 1.0f }, 0.9998783f) });
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
        var search = vectorCollection.ScoreBySimilarityTo(
            new[] { 1f, 1f },
            new CosineSimilarityComparer<float[]>(t => t)
        ).OrderByDescending(e => e.Score)
            .Take(2);

        var expected = new[]
        {
            ScoredValue.Create(  new  [] { 1f,1.5f }, 0.9805807f ),
            ScoredValue.Create( new[] { 1f,1.7f },  0.9679969f )
        };

        search.Should().BeEquivalentTo(expected);
    }
}

public class TelemetryTests
{
    [Fact]
    public void are_good()
    {
        var firstTimeUseNoticeSentinel = new FakeFirstTimeUseNoticeSentinel
        {
            SentinelExists = true
        };
        var telemetrySender = new FakeTelemetrySender(firstTimeUseNoticeSentinel);
        using var _ = telemetrySender.SubscribeToPocketLogger(typeof(CosineSimilarityComparer<>).Assembly);

        var vectorCollection = new List<float[]>
        {
            new []{1f,0f},
            new []{1f,1.5f},
            new []{1f,1.7f},
            new []{1f,1.9f}
        };
        var __ = vectorCollection.ScoreBySimilarityTo(
                new[] { 1f, 1f },
                new CosineSimilarityComparer<float[]>(t => t)
            ).ToArray();

        telemetrySender.TelemetryEvents.Should().NotBeEmpty();
        var expected = $"{typeof(Text).FullName}.ScoreBySimilarityTo";
        telemetrySender.TelemetryEvents.Should().Contain(e =>
        e.EventName == expected);
    }   
}
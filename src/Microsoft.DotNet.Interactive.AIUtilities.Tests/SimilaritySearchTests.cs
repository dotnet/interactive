// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
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

        var search = collection.Search("diego",
            new CosineSimilarityComparer<string>(a => a.Select(c => (float)c).ToArray()),
            q => q.Select(c => (float)c).ToArray()
        );

        search.Should().BeEquivalentTo(new [] { new ScoredItem<string>("diago", 0.9998783f) });
    } 
}
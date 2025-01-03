// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Connection;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class RecentConnectionListTests
{
    [Fact]
    public void When_more_than_the_MRU_list_limit_is_reached_then_the_least_recently_used_is_removed_and_new_item_is_added_at_the_beginning()
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new("one",  ["value1"]));
        list.Add(new("two",  ["value2"]));
        list.Add(new("three",["value3"]));
        list.Add(new("four", ["value4"]));

        list.Count.Should().Be(3);

        list.Should().BeEquivalentTo(
        [
            new ConnectionShortcut("four", ["value4"]),
            new ConnectionShortcut("three", ["value3"]),
            new ConnectionShortcut("two", ["value2"]),
        ]);
    }

    [Theory]
    [InlineData(new[] { "one" }, new[] { "one" })]
    [InlineData(new[] { "one" }, new[] { "one  " })]
    [InlineData(new[] { "one", "two" }, new[] { "one", "two" })]
    [InlineData(new[] { "one  ", "two" }, new[] { "one", "two" })]
    public void When_a_duplicate_value_is_added_then_it_is_moved_to_the_top_of_the_list(
        string[] duplicateCode1,
        string[] duplicateCode2)
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new("one", ["value1"]));
        list.Add(new("two", duplicateCode1));
        list.Add(new("three", ["value3"]));
        list.Add(new("four", duplicateCode2)); // value (not name) is a duplicate

        list.Should().BeEquivalentTo(
        [
            new ConnectionShortcut("two", duplicateCode1),
            new ConnectionShortcut("three", ["value3"]),
            new ConnectionShortcut("one", ["value1"]),
        ]);
    }

    [Fact]
    public void It_can_be_round_trip_JSON_serialized()
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new("one", ["value1"]));
        list.Add(new("two", ["value2"]));
        list.Add(new("three", ["value3"]));

        var json = JsonSerializer.Serialize(list, Serializer.JsonSerializerOptions);

        var deserialized = JsonSerializer.Deserialize<RecentConnectionList>(json, Serializer.JsonSerializerOptions);

        deserialized.Capacity.Should().Be(3);
        deserialized.Should().BeEquivalentTo(list.Reverse());
    }

    [Fact]
    public void The_list_can_be_cleared()
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new("one", ["value1"]));
        list.Add(new("two", ["value2"]));
        list.Add(new("three", ["value3"]));

        list.Clear();

        list.Should().BeEmpty();
    }
}
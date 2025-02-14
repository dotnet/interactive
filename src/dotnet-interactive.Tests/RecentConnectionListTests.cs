// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Connection;
using Xunit;
using static Microsoft.DotNet.Interactive.App.CodeExpansion;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class RecentConnectionListTests
{
    [Fact]
    public void When_more_than_the_MRU_list_limit_is_reached_then_the_least_recently_used_is_removed_and_new_item_is_added_at_the_beginning()
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new CodeExpansion([new("value1")], new CodeExpansionInfo("one", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value2")], new CodeExpansionInfo("two", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value3")], new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value4")], new CodeExpansionInfo("four", CodeExpansionKind.RecentConnection)));

        list.Count.Should().Be(3);

        list.Should().BeEquivalentTo(
        [
            new CodeExpansion([new("value4")], new CodeExpansionInfo("four", CodeExpansionKind.RecentConnection)),
            new CodeExpansion([new("value3")], new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)),
            new CodeExpansion([new("value2")], new CodeExpansionInfo("two", CodeExpansionKind.RecentConnection)),
        ]);
    }

    [Theory]
    [InlineData(new[] { "one" }, new[] { "one" })]
    [InlineData(new[] { "one" }, new[] { "one  " })]
    [InlineData(new[] { "one", "two" }, new[] { "one", "two" })]
    [InlineData(new[] { "one  ", "two" }, new[] { "one", "two" })]
    public void When_a_duplicate_value_is_added_then_it_is_moved_to_the_top_of_the_list(
        string[] original,
        string[] duplicate)
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new(
            [new("value1")],
            new CodeExpansionInfo("one", CodeExpansionKind.RecentConnection)));
        list.Add(new(
            original.Select(c => new CodeExpansionSubmission(c)).ToArray(), 
            new CodeExpansionInfo("two", CodeExpansionKind.RecentConnection)));
        list.Add(new(
            [new("value3")], 
            new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)));
        list.Add(new(
            duplicate.Select(c => new CodeExpansionSubmission(c)).ToArray(), 
            new CodeExpansionInfo("four", CodeExpansionKind.RecentConnection))); // value (not name) is a duplicate

        list.Should().BeEquivalentTo(
        [
            new CodeExpansion(duplicate.Select(c => new CodeExpansionSubmission(c)).ToArray(), new CodeExpansionInfo("four", CodeExpansionKind.RecentConnection)),
            new CodeExpansion([new("value3")], new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)),
            new CodeExpansion([new("value1")], new CodeExpansionInfo("one", CodeExpansionKind.RecentConnection)),
        ]);
    }

    [Fact]
    public void When_a_duplicate_name_is_added_it_replaces_the_previous_entry_having_the_same_name()
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new CodeExpansion([new("value1")], new CodeExpansionInfo("DUPLICATE1", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value2")], new CodeExpansionInfo("DUPLICATE2", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value3")], new CodeExpansionInfo("DUPLICATE1", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value4")], new CodeExpansionInfo("DUPLICATE2", CodeExpansionKind.RecentConnection)));
        
        list.Count.Should().Be(2);

        list.Should().BeEquivalentTo(
        [
            new CodeExpansion([new("value4")], new CodeExpansionInfo("DUPLICATE2", CodeExpansionKind.RecentConnection)),
            new CodeExpansion([new("value3")], new CodeExpansionInfo("DUPLICATE1", CodeExpansionKind.RecentConnection)),
        ]);
    }

    [Fact]
    public void It_can_be_round_trip_JSON_serialized()
    {
        var list = new RecentConnectionList();

        list.Add(new CodeExpansion([new("value1")], new CodeExpansionInfo("one", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value2")], new CodeExpansionInfo("two", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value3")], new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)));

        var json = JsonSerializer.Serialize(list, Serializer.JsonSerializerOptions);

        var deserialized = JsonSerializer.Deserialize<RecentConnectionList>(json, Serializer.JsonSerializerOptions);

        deserialized.Should().BeEquivalentTo(list.Reverse());
    }

    [Fact]
    public void Serialization_contract_has_not_been_broken()
    {
        var codeExpansion = new CodeExpansion(
            [new("#!connect jupyter --kernel-name python3 --kernel-spec python3", "csharp")],
            new CodeExpansionInfo("python3 - python 3.11.4 (Preview)",
                                  CodeExpansionKind.RecentConnection,
                                  Description: "This is the description"));

        var json = """
                    {
                       "items": [
                       {
                           "info": {
                           "name": "python3 - python 3.11.4 (Preview)",
                           "kind": "RecentConnection",
                           "description": "This is the description"
                           },
                           "content": [
                           {
                               "code": "#!connect jupyter --kernel-name python3 --kernel-spec python3",
                               "targetKernelName": "csharp"
                           }
                           ]
                       }
                       ]
                    }
                   """;

        var deserialized = JsonSerializer.Deserialize<RecentConnectionList>(json, Serializer.JsonSerializerOptions);

        deserialized.Should().BeEquivalentTo(
        [
            codeExpansion
        ]);
    }

    [Fact]
    public void When_capacity_is_reduced_then_excess_items_are_removed()
    {
         var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new CodeExpansion([new("value1")], new CodeExpansionInfo("one", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value2")], new CodeExpansionInfo("two", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value3")], new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)));

        list.Capacity = 2;


        list.Select(i => i.Info.Name)
            .Should()
            .BeEquivalentTo("three", "two");
    }

    [Fact]
    public void The_list_can_be_cleared()
    {
        var list = new RecentConnectionList { Capacity = 3 };

        list.Add(new CodeExpansion([new("value1")], new CodeExpansionInfo("one", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value2")], new CodeExpansionInfo("two", CodeExpansionKind.RecentConnection)));
        list.Add(new CodeExpansion([new("value3")], new CodeExpansionInfo("three", CodeExpansionKind.RecentConnection)));

        list.Clear();

        list.Should().BeEmpty();
    }
}
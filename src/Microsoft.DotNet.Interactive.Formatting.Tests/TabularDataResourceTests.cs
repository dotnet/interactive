// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public class TabularDataResourceTests
{
    [Fact]
    public void can_create_from_JsonDocument()
    {
        var doc = JsonDocument.Parse(@"[
{ ""name"": ""mitch"", ""age"": 42, ""salary"":10.0, ""active"":true }
]");
        var expected = new TabularDataResource(
            new TableSchema
            {
                Fields = new TableDataFieldDescriptors
                {
                    new("name", TableSchemaFieldType.String),
                    new("age", TableSchemaFieldType.Integer),
                    new("salary", TableSchemaFieldType.Number),
                    new("active", TableSchemaFieldType.Boolean),
                }
            },
            new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "mitch",
                    ["age"] = 42,
                    ["salary"] = 10.0,
                    ["active"] = true,
                }
            });

        var actual = doc.ToTabularDataResource();

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void schema_is_inferred_correctly_when_leading_data_item_has_null_field()
    {
        var tabularDataResource = JsonDocument.Parse(@"
[
  {
        ""name"": ""Granny Smith apple"",
        ""deliciousness"": null,
        ""color"":null
  },
  {
        ""name"": ""Rainier cherry"",
        ""deliciousness"": 9000, 
        ""color"":""yellow""
  }
]").ToTabularDataResource();

        tabularDataResource
            .Schema
            .Fields["name"]
            .Type.Should().Be(TableSchemaFieldType.String);

        tabularDataResource
            .Schema
            .Fields["deliciousness"]
            .Type.Should().Be(TableSchemaFieldType.Integer);

        tabularDataResource
            .Schema
            .Fields["color"]
            .Type.Should().Be(TableSchemaFieldType.String);
    }

    [Fact]
    public void can_be_deserialized_from_json()
    {
        var json = @"
{   ""profile"": ""tabular-data-resource"",
    ""schema"": {
        ""primaryKey"": [],
        ""fields"": [
            {
                ""name"": ""name"",
                ""type"": ""string""
            },
            {
                ""name"": ""age"",
                ""type"": ""integer""
            },
            {
                ""name"": ""salary"",
                ""type"": ""number""
            },
            {
                ""name"": ""active"",
                ""type"": ""boolean""
            },
            {
                ""name"": ""date"",
                ""type"": ""datetime""
            },
            {
                ""name"": ""summary"",
                ""type"": ""object""
            },
            {
                ""name"": ""list"",
                ""type"": ""array""
            }
        ]
    },
    ""data"" : [
        { ""name"": ""mitch"", ""age"": 42, ""salary"":10.0, ""active"":true, ""date"": ""2020-01-01"", ""summary"": { ""a"": 1, ""b"": 2 }, ""list"":[1,2,3,4] }
    ]
}";
        var expected = new TabularDataResource(
            new TableSchema
            {
                Fields = new TableDataFieldDescriptors
                {
                    new("name", TableSchemaFieldType.String),
                    new("age", TableSchemaFieldType.Integer),
                    new("salary", TableSchemaFieldType.Number),
                    new("active", TableSchemaFieldType.Boolean),
                    new("date", TableSchemaFieldType.DateTime),
                    new("summary", TableSchemaFieldType.Object),
                    new("list", TableSchemaFieldType.Array)
                }
            },
            new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "mitch",
                    ["age"] = 42,
                    ["salary"] = 10.0,
                    ["active"] = true,
                    ["date"] = new DateTime(2020, 1, 1),
                    ["summary"] = new Dictionary<string, object>
                    {
                        ["a"] = 1,
                        ["b"] = 2
                    },
                    ["list"] = new object[]{1,2,3,4}
                }
            });

        var actual = JsonSerializer.Deserialize<TabularDataResource>(json, JsonFormatter.SerializerOptions);

        actual.Should().BeEquivalentTo(expected);
    }
}
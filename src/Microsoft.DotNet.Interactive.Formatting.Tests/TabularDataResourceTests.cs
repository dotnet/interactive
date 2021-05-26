// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
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
                        new("age", TableSchemaFieldType.Number),
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
                .Type.Should().Be(TableSchemaFieldType.Number);

            tabularDataResource
                .Schema
                .Fields["color"]
                .Type.Should().Be(TableSchemaFieldType.String);
        }
    }
}
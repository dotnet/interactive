// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class TabularDataResourceFormatterTests : IDisposable
    {
        private readonly Configuration _configuration;

        public TabularDataResourceFormatterTests()
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public void can_generate_tabular_json_when_non_numeric_literals_are_used()
        {
            var data = new[]
            {
                new { Name = "Q", IsValid = false, Cost = double.NaN },
                new { Name = "U", IsValid = false, Cost = 5.0 },
                new { Name = "E", IsValid = true, Cost = double.NegativeInfinity },
                new { Name = "S", IsValid = false, Cost = 10.0 },
                new { Name = "T", IsValid = false, Cost = double.PositiveInfinity }
            };

            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_object_array()
        {
            var data = new[]
            {
                new { Name = "Q", IsValid = false, Cost = 10.0 },
                new { Name = "U", IsValid = false, Cost = 5.0 },
                new { Name = "E", IsValid = true, Cost = 10.2 },
                new { Name = "S", IsValid = false, Cost = 10.0 },
                new { Name = "T", IsValid = false, Cost = 10.0 }
            };

            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_sequence_of_sequences_of_ValueTuples()
        {
            IEnumerable<IEnumerable<(string name, object value)>> data =
                new[]
                {
                    new (string name, object value)[]
                    {
                        ("id", 1),
                        ("name", "apple"),
                        ("color", "green"),
                        ("deliciousness", 10)
                    },
                    new (string name, object value)[]
                    {
                        ("id", 2),
                        ("name", "banana"),
                        ("color", "yellow"),
                        ("deliciousness", 11)
                    },
                    new (string name, object value)[]
                    {
                        ("id", 3),
                        ("name", "cherry"),
                        ("color", "red"),
                        ("deliciousness", 9000)
                    },
                };

            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_sequence_of_sequences_of_KeyValuePairs()
        {
            IEnumerable<IEnumerable<KeyValuePair<string, object>>> data =
                new[]
                {
                    new[]
                    {
                        new KeyValuePair<string, object>("id", 1),
                        new KeyValuePair<string, object>("name", "apple"),
                        new KeyValuePair<string, object>("color", "green"),
                        new KeyValuePair<string, object>("deliciousness", 10)
                    },
                    new[]
                    {
                        new KeyValuePair<string, object>("id", 2),
                        new KeyValuePair<string, object>("name", "banana"),
                        new KeyValuePair<string, object>("color", "yellow"),
                        new KeyValuePair<string, object>("deliciousness", 11)
                    },
                    new[]
                    {
                        new KeyValuePair<string, object>("id", 3),
                        new KeyValuePair<string, object>("name", "cherry"),
                        new KeyValuePair<string, object>("color", "red"),
                        new KeyValuePair<string, object>("deliciousness", 9000)
                    },
                };

            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_dictionary()
        {
            var data = new Dictionary<string,int>
            {
                ["one"] = 1,
                ["two"] = 2
            };

            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_data_with_nullable_types()
        {
            var data = new Dictionary<string, int?>
            {
                ["one"] = 1,
                ["two"] = null
            };

            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void Tabular_data_resource_with_is_formatted_as_a_table()
        {
            var tabularDataResource = JsonDocument.Parse(@"
[
  {
      ""name"": ""Granny Smith apple"", 
      ""deliciousness"": 12, 
      ""color"":""green"",
      ""available"":true 
  },
  { 
      ""name"": ""Rainier cherry"",
      ""deliciousness"": 9000, 
      ""color"":""yellow"",  
      ""available"":true
  }
]").ToTabularDataResource();

            tabularDataResource
                .ToDisplayString("text/html")
                .Should()
                .Be("<table><thead><tr><td><span>name</span></td><td><span>deliciousness</span></td><td><span>color</span></td><td><span>available</span></td></tr></thead><tbody><tr><td>Granny Smith apple</td><td><div class=\"dni-plaintext\">12</div></td><td>green</td><td><div class=\"dni-plaintext\">True</div></td></tr><tr><td>Rainier cherry</td><td><div class=\"dni-plaintext\">9000</div></td><td>yellow</td><td><div class=\"dni-plaintext\">True</div></td></tr></tbody></table>");
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}
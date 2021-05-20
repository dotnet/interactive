// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text.Json;

using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class TabularDataResourceTest
    {
        private readonly Configuration _configuration;

        public TabularDataResourceTest()
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public void can_create_from_document()
        {
            var doc = JsonDocument.Parse(@"[
{ ""name"": ""mitch"", ""age"": 42, ""salary"":10.0, ""active"":true }
]");
            var data = doc.ToTabularDataResource();
            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public void When_data_explorer_is_present_then_html_formatter_formats_tabular_data_resource_with_it()
        {
            var tabularDataResource = JsonDocument
                                      .Parse(@"[{ ""name"": ""mitch"", ""age"": 42, ""salary"":10.0, ""active"":true }]")
                                      .ToTabularDataResource();

            tabularDataResource
                .ToDisplayString("text/html")
                .Should()
                .Be(
                   "<table><thead><tr><td><span>name</span></td><td><span>age</span></td><td><span>salary</span></td><td><span>active</span></td></tr></thead><tbody><tr><td>mitch</td><td><div class=\"dni-plaintext\">42</div></td><td><div class=\"dni-plaintext\">10</div></td><td><div class=\"dni-plaintext\">True</div></td></tr></tbody></table>");
        }
    }
}
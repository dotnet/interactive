// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Lsp;
using Microsoft.DotNet.Interactive.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class LspSerializationContractTests
    {
        private readonly Configuration _configuration;

        public LspSerializationContractTests()
        {
            _configuration = new Configuration()
                .UsingExtension("json");

            _configuration = _configuration.SetInteractive(true);
        }

        [Fact]
        public void HoverParams_has_well_formed_deserialization()
        {
            var json = @"{
    ""textDocument"": {
        ""uri"": ""document-uri""
    },
    ""position"":{
        ""line"": 1,
        ""character"": 2
    }
}";
            var jObject = JObject.Parse(json);
            var actual = jObject.ToObject<HoverParams>();
            var expected = new HoverParams(
                new TextDocument("document-uri"),
                new Position(1, 2));
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void HoverResponse_with_range_has_well_formed_serialization()
        {
            var hoverResponse = new HoverResponse(
                new MarkupContent(MarkupKind.Markdown, "content"),
                new Range(new Position(1, 2), new Position(3, 4)));
            var json = SerializeToLspString(hoverResponse);
            this.Assent(json, _configuration);
        }

        [Fact]
        public void HoverResponse_without_range_has_well_formed_serialization()
        {
            var hoverResponse = new HoverResponse(
                new MarkupContent(MarkupKind.Markdown, "content"));
            var json = SerializeToLspString(hoverResponse);
            this.Assent(json, _configuration);
        }

        public static string SerializeToLspString<T>(T value)
        {
            using var writer = new StringWriter();
            LspSerializer.JsonSerializer.Serialize(writer, value);
            var json = writer.ToString();
            return json;
        }
    }
}

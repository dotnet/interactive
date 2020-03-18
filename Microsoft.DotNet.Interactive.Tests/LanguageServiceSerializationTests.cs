// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.LanguageService;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class LanguageServiceSerializationTests
    {
        private T Parse<T>(string json)
        {
            var jo = JObject.Parse(json);
            return jo.ToLspObject<T>();
        }

        private void VerifyRoundTrip<T>(T original)
        {
            var json = original.SerializeLspObject();
            var roundTripped = Parse<T>(json);
            roundTripped.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void HoverParams_can_be_deserialized()
        {
            using var _ = new AssertionScope();
            var hover = Parse<HoverParams>(@"
{
    ""textDocument"": {
        ""uri"": ""document-uri-contents""
    },
    ""position"": {
        ""line"": 1,
        ""character"": 2
    }
}");
            hover.TextDocument.Uri.Should().Be("document-uri-contents");
            hover.Position.Line.Should().Be(1);
            hover.Position.Character.Should().Be(2);
        }

        [Fact]
        public void HoverParams_can_be_round_tripped()
        {
            var hover = new HoverParams()
            {
                TextDocument = new TextDocument("document-uri-contents"),
                Position = new Position(1, 2)
            };
            VerifyRoundTrip(hover);
        }
    }
}

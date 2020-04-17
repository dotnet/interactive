// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.CompilerServices;
using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Lsp;
using Microsoft.DotNet.Interactive.Extensions;
using Newtonsoft.Json;
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

        private void Dissent<T>(T expected, JsonSerializer serializer, [CallerMemberName] string testName = null, [CallerFilePath] string filePath = null)
        {
            var pathToJson = Path.Combine(Path.GetDirectoryName(filePath), $"{GetType().Name}.{testName}.expected.json");
            if (!File.Exists(pathToJson))
            {
                // ensure there's something
                File.Create(pathToJson).Close();
            }

            var json = File.ReadAllText(pathToJson);
            var jObject = JObject.Parse(json);
            var actual = jObject.ToObject<T>(serializer);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void HoverParams_has_well_formed_deserialization()
        {
            var hoverParams = new HoverParams(
                new TextDocument("document-uri"),
                new Position(1, 2));
            Dissent(hoverParams, LspSerializer.JsonSerializer);
        }

        [Fact]
        public void HoverResponse_with_range_has_well_formed_serialization()
        {
            var hoverResponse = new HoverResponse(
                new MarkupContent(MarkupKind.Markdown, "content"),
                new Range(new Position(1, 2), new Position(3, 4)));
            var json = LspSerializer.JsonSerializer.SerializeToString(hoverResponse);
            this.Assent(json, _configuration);
        }

        [Fact]
        public void HoverResponse_without_range_has_well_formed_serialization()
        {
            var hoverResponse = new HoverResponse(
                new MarkupContent(MarkupKind.Markdown, "content"));
            var json = LspSerializer.JsonSerializer.SerializeToString(hoverResponse);
            this.Assent(json, _configuration);
        }
    }
}

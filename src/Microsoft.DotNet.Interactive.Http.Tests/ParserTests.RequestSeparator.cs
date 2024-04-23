// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    public class RequestSeparator
    {

        [Fact]
        public void request_separator_at_start_of_request_is_valid()
        {
            var code = """
                ###
                GET https://example.com
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.ChildNodes
                  .Should().ContainSingle<HttpRequestNode>()
                  .Which.UrlNode.Text.Should().Be("https://example.com");
        }

        [Fact]
        public void tokens_on_request_separator_nodes_are_parsed_into_request_separator()
        {
            var result = Parse(
                """
                ### Slow Response (Json)
                GET https://httpbin.org/anything?page=2&pageSize=10
                """
                );

            var requestNodes = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestSeparatorNode>()
                                    .Which.Text.Should().Be("### Slow Response (Json)");
        }

        [Fact]
        public void tokens_on_request_separator_nodes()
        {
            var result = Parse(
                """
                @MyRestaurantApi_HostAddress = https://localhost:7293

                GET {{MyRestaurantApi_HostAddress}}/api/Contact
                ###

                @Somevar = hello

                ###

                PUT https://httpbin.org/anything
                Content-Type: application/json

                {
                    "content": "content here",
                    "message": {{Message}}
                }

                ###
                """
                );

            result.SyntaxTree.RootNode.ChildNodes.OfType<HttpRequestSeparatorNode>().Count().Should().Be(3);
        }
    }
}
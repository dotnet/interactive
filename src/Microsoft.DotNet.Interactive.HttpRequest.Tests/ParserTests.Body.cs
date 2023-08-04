// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Body
    {
        [Fact]
        public void body_separator_is_present()
        {
            var result = Parse(
                """
            POST https://example.com/comments HTTP/1.1
            Content-Type: application/xml
            Authorization: token xxx

            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);

            var requestNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.BodySeparatorNode.ChildTokens.First().Kind.Should().Be(HttpTokenKind.NewLine);
        }

        [Fact]
        public void when_headers_are_not_present_there_should_be_no_header_nodes()
        {
            var result = Parse(
                """
                POST https://example.com/comments HTTP/1.1

                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);

            var requestNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.HeadersNode.Should().BeNull();
        }

        [Fact]
        public void body_is_parsed_correctly_when_headers_are_not_present()
        {
            var result = Parse(
                """
                POST https://example.com/comments HTTP/1.1

                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);

            var requestNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.BodyNode.Text.Should().Be(
                """
                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);
        }

        [Fact]
        public void multiple_new_lines_before_body_are_parsed_correctly()
        {
            var result = Parse(
                """
                POST https://example.com/comments HTTP/1.1
                Content-Type: application/xml
                Authorization: token xxx




                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);

            var requestNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.BodyNode.Text.Should().Be(
                """
                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);
        }
    }
}
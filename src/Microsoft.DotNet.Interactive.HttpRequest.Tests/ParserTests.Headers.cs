// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Headers
    {
        [Fact]
        public void header_with_body_is_parsed_correctly()
        {
            var result = Parse(
                """
        POST https://example.com/comments
        Content-Type: application/xml
        Authorization: token xxx

        <request>
            <name>sample</name>
            <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
        </request>
        """);

            var requestNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.HeadersNode.HeaderNodes.Count.Should().Be(2);
            requestNode.BodyNode.Text.Should().Be(
                """
        <request>
            <name>sample</name>
            <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
        </request>
        """);
        }

        [Fact]
        public void header_separator_is_parsed()
        {
            var result = Parse(
                """
        POST https://example.com/comments HTTP/1.1
        Content-Type: application
        """);

            var requestNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.HeadersNode.HeaderNodes.Single().SeparatorNode.Text.Should().Be(":");
        }

        [Fact]
        public void headers_are_parsed_correctly()
        {
            var result = Parse(
                """
                GET https://example.com HTTP/1.1
                Accept: */*
                Accept-Encoding : gzip, deflate, br
                Accept-Language : en-US,en;q=0.9
                ContentLength:7060
                Cookie: expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;
                """);

            var headersNode = result.SyntaxTree.RootNode
                                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                                    .ChildNodes.Should().ContainSingle<HttpHeadersNode>().Which;

            var headerNodes = headersNode.HeaderNodes.ToArray();
            headerNodes.Should().HaveCount(5);

            headerNodes[0].NameNode.Text.Should().Be("Accept");
            headerNodes[0].ValueNode.Text.Should().Be("*/*");

            headerNodes[1].NameNode.Text.Should().Be("Accept-Encoding");
            headerNodes[1].ValueNode.Text.Should().Be("gzip, deflate, br");

            headerNodes[2].NameNode.Text.Should().Be("Accept-Language");
            headerNodes[2].ValueNode.Text.Should().Be("en-US,en;q=0.9");

            headerNodes[3].NameNode.Text.Should().Be("ContentLength");
            headerNodes[3].ValueNode.Text.Should().Be("7060");

            headerNodes[4].NameNode.Text.Should().Be("Cookie");
            headerNodes[4].ValueNode.Text.Should().Be("expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;");
        }
    }
}
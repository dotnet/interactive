// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
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
        public void header_separator_is_parsed()
        {
            var result = Parse(
                """
        POST https://example.com/comments HTTP/1.1
        Content-Type: application
        """);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                  .Should().ContainSingle<HttpHeaderSeparatorNode>()
                  .Which.Text.Should().Be(":");
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

            var headersNode = result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                                    .Should().ContainSingle<HttpHeadersNode>().Which;

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

        [Fact]
        public void Common_headers_are_bound_correctly()
        {
            var result = Parse(
                """
                GET https://example.com 
                Accept: {{accept}}
                Accept-Encoding : {{acceptEncoding}}
                Accept-Language : {{acceptLanguage}}
                Content-Length:  {{contentLength}}
                Cookie: {{cookie}}
                user-agent: {{userAgent}}
                """);

            var accept = "*/*";
            var acceptEncoding = "gzip, deflate, br";
            var acceptLanguage = "en-US,en;q=0.9";
            var contentLength = 7060;
            var cookie = "expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;";
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.203";

            HttpBindingDelegate bind = node =>
            {
                return HttpBindingResult<object>.Success(node.Text switch
                {
                    "accept" => accept,
                    "acceptEncoding" => acceptEncoding,
                    "acceptLanguage" => acceptLanguage,
                    "contentLength" => contentLength,
                    "cookie" => cookie,
                    "userAgent" => userAgent
                });
            };

            var bindingResult = result.SyntaxTree.RootNode.ChildNodes.OfType<HttpRequestNode>().Single().TryGetHttpRequestMessage(bind);

            bindingResult.Diagnostics.Should().BeEmpty();

            var request = bindingResult.Value;

            request.Headers.Accept.Should().ContainSingle().Which.MediaType.Should().Be(accept);

            request.Headers.AcceptEncoding.Select(e => e.Value).Should().BeEquivalentTo("gzip", "deflate", "br");

            request.Headers.AcceptLanguage.Select(l => l.Value).Should().BeEquivalentTo("en-US", "en");

            request.Headers.AcceptLanguage
                   .Where(q => q.Quality.HasValue)
                   .Select(l => l.Quality.Value)
                   .Should().BeEquivalentSequenceTo(0.9);

            request.Content.Headers.ContentLength.Should().Be(7060);

        }



        [Fact]
        public void Diagnostics_are_reported_for_missing_header_expression_values()
        {
            var result = Parse(
                """
                GET https://example.com 
                Accept: {{accept}}
                """);

            var bindingResult = result.SyntaxTree.RootNode.ChildNodes.OfType<HttpRequestNode>().Single()
                                      .TryGetHttpRequestMessage(node => HttpBindingResult<object>.Failure(node.CreateDiagnostic("oops!")));

            bindingResult.Diagnostics.Should().ContainSingle()
                         .Which.Message.Should().Be("oops!");
        }


        [Fact]
        public void Missing_header_value_produces_a_diagnostic()
        {
            var result = Parse(
                """
                GET https://example.com 
                Accept:
                """);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                  .Should().ContainSingle<HttpHeaderNode>()
                  .Which.GetDiagnostics()
                  .Should().ContainSingle()
                  .Which.Message.Should().Be("Missing header value");
        }
    }
}
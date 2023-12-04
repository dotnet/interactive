// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    public class Body
    {
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

        [Fact]
        public void Whitespace_after_headers_is_not_parsed_as_body()
        {
            var code = """
                                
                hptps://example.com 
                Accept: */*




                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should().NotContain(n => n is HttpBodyNode);
        }

        [Fact]
        public void When_body_is_absent_HttpRequestMessage_Content_is_set_to_null()
        {
            var result = Parse(
                """
                GET https://example.com
                """);

            HttpBindingDelegate bind =
                node => HttpBindingResult<object>.Failure(
                    node.CreateDiagnostic(CreateDiagnosticInfo("oops!")));

            var requestNode = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpRequestNode>().Single();
            var bindingResult = requestNode.TryGetHttpRequestMessage(bind);
            bindingResult.Diagnostics.Should().BeEmpty();

            var request = bindingResult.Value;
            request.Headers.Should().BeEmpty();
            request.Content.Should().BeNull();
        }

        [Fact]
        public void When_body_is_present_HttpRequestMessage_Content_is_set_appropriately()
        {
            var result = Parse(
                """
                GET https://example.com

                name=value
                """);

            HttpBindingDelegate bind =
                node => HttpBindingResult<object>.Failure(
                    node.CreateDiagnostic(CreateDiagnosticInfo("oops!")));

            var requestNode = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpRequestNode>().Single();
            var bindingResult = requestNode.TryGetHttpRequestMessage(bind);
            bindingResult.Diagnostics.Should().BeEmpty();

            var request = bindingResult.Value;
            request.Headers.Should().BeEmpty();

            var contentHeaders = request.Content.Headers;
            contentHeaders.ContentType.ToString().Should().Be("text/plain; charset=utf-8");
            contentHeaders.ContentLength.Should().Be(10);
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Url
    {
        [Fact]
        public void whitespace_is_legal_after_url()
        {
            var result = Parse("GET https://example.com  ");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .UrlNode.ChildTokens.Last().Kind.Should().Be(HttpTokenKind.Whitespace);
        }

        [Fact]
        public void newline_is_legal_at_the_after_url()
        {
            var result = Parse(
                """
                GET https://example.com

                """);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildTokens.Last().Kind.Should().Be(HttpTokenKind.NewLine);
        }

        [Theory]
        [InlineData("https://example.com?hat&ost=foo")]
        [InlineData("https://example.com?q=3081#blah-2%203")]
        public void common_url_structures_are_parsed_correctly(string url)
        {
            var result = Parse($"GET {url}");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .UrlNode.Text.Should().Be(url);
        }

        [Fact]
        public void request_node_without_method_node_created_correctly()
        {
            var result = Parse("https://example.com");

            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Should().BeNull();
        }

        [Fact]
        public void url_node_can_return_url()
        {
            var result = Parse(
                """
        GET https://{{host}}/api/{{version}}comments/1
        """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var urlNode = requestNode.UrlNode;
            var bindingResult = urlNode.TryGetUri(node =>
            {
                return node.Text switch
                {
                    "host" => node.CreateBindingSuccess("example.com"),
                    "version" => node.CreateBindingSuccess("123-")
                };
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.ToString().Should().Be("https://example.com/api/123-comments/1");
        }

        [Fact]
        public void error_is_reported_for_undefined_variable()
        {
            var result = Parse(
                """
            GET https://example.com/api/{{version}}comments/1
            """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var urlNode = requestNode.UrlNode;

            var message = "Variable 'version' was not defined.";

            HttpBindingDelegate bind = node => node.CreateBindingFailure(message);

            var bindingResult = urlNode.TryGetUri(bind);
            bindingResult.IsSuccessful.Should().BeFalse();
            bindingResult.Diagnostics.Should().ContainSingle().Which.Message.Should().Be(message);
        }

        [Fact]
        public void Missing_url_produces_a_diagnostic()
        {
            var code = """
                GET 
                Accept: application/json
                """;

            var result = Parse(code);

            var diagnostic = result.GetDiagnostics().Should().ContainSingle(n => n.Message == "Missing URL").Which;

            diagnostic.LinePositionSpan.Start.Line.Should().Be(0);
            diagnostic.LinePositionSpan.Start.Character.Should().Be(3);
            diagnostic.LinePositionSpan.End.Line.Should().Be(0);
            diagnostic.LinePositionSpan.End.Character.Should().Be(3);
        }

        [Fact]
        public void Invalid_url_produces_a_diagnostic()
        {
            var code = """
                GET https://
                """;

            var result = Parse(code);

            var diagnostic = result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should().ContainSingle<HttpUrlNode>()
                                   .Which.GetDiagnostics().Should().ContainSingle()
                                   .Which;


            diagnostic.LinePositionSpan.Start.Line.Should().Be(0);
            diagnostic.LinePositionSpan.Start.Character.Should().Be(4);
            diagnostic.LinePositionSpan.End.Line.Should().Be(0);
            diagnostic.LinePositionSpan.End.Character.Should().Be(12);
        }
    }
}
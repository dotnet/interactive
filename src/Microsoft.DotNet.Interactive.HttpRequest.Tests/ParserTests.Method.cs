// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Method
    {
        [Fact]
        public void whitespace_is_legal_at_the_beginning_of_a_request()
        {
            var result = Parse("  GET https://example.com");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildTokens.First().Kind
                  .Should().Be(HttpTokenKind.Whitespace);
        }

        [Fact]
        public void newline_is_legal_at_the_beginning_of_a_request()
        {
            var result = Parse(
                """

                GET https://example.com
                """);

            var requestNode = result.SyntaxTree
                                   .RootNode
                                   .ChildNodes
                                   .Should()
                                   .ContainSingle<HttpRequestNode>().Which;

            requestNode.ChildTokens.First().Kind.Should().Be(HttpTokenKind.NewLine);
            requestNode.MethodNode.Text.Should().Be("GET");
        }

        [Fact]
        public void comment_is_legal_at_the_beginning_of_a_request()
        {
            var result = Parse(
                """
                # this is a comment
                GET https://example.com
                """);

            var requestNode = result.SyntaxTree
                                   .RootNode
                                   .ChildNodes
                                   .Should()
                                   .ContainSingle<HttpRequestNode>().Which;

            requestNode.ChildNodes.Should().ContainSingle<HttpCommentNode>();
            requestNode.MethodNode.Text.Should().Be("GET");
        }

        [Theory]
        [InlineData("GET https://example.com", "GET")]
        [InlineData("POST https://example.com", "POST")]
        [InlineData("OPTIONS https://example.com", "OPTIONS")]
        [InlineData("TRACE https://example.com", "TRACE")]
        public void common_verbs_are_parsed_correctly(string line, string method)
        {
            var result = Parse(line);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Text.Should().Be(method);
        }

        [Theory]
        [InlineData("GET https://example.com", "GET")]
        [InlineData("Get https://example.com", "Get")]
        [InlineData("OPTIONS https://example.com", "OPTIONS")]
        [InlineData("options https://example.com", "options")]
        public void it_can_parse_verbs_regardless_of_their_casing(string line, string method)
        {
            var result = Parse(line);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Text.Should().Be(method);
        }

        [Fact]
        public void Unrecognized_verb_produces_a_diagnostic()
        {
            var result = Parse("OOPS https://example.com");

            var diagnostic = result.GetDiagnostics().Should().ContainSingle().Which;

            diagnostic.Message.Should().Be("Unrecognized HTTP verb OOPS");

            diagnostic.LinePositionSpan.Start.Line.Should().Be(0);
            diagnostic.LinePositionSpan.Start.Character.Should().Be(0);
            diagnostic.LinePositionSpan.End.Line.Should().Be(0);
            diagnostic.LinePositionSpan.End.Character.Should().Be(4);
        }
    }
}
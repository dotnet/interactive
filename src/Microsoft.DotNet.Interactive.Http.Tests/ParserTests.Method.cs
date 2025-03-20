// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    [TestClass]
    public class Method
    {
        [TestMethod]
        public void whitespace_is_legal_at_the_beginning_of_a_request()
        {
            var result = Parse("  GET https://example.com");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildTokens.First().Kind
                  .Should().Be(TokenKind.Whitespace);
        }

        [TestMethod]
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

            requestNode.ChildTokens.First().Kind.Should().Be(TokenKind.NewLine);
            requestNode.MethodNode.Text.Should().Be("GET");
        }

        [TestMethod]
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

        [TestMethod]
        [DataRow("GET https://example.com", "GET")]
        [DataRow("POST https://example.com", "POST")]
        [DataRow("OPTIONS https://example.com", "OPTIONS")]
        [DataRow("TRACE https://example.com", "TRACE")]
        public void common_verbs_are_parsed_correctly(string line, string method)
        {
            var result = Parse(line);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Text.Should().Be(method);
        }

        [TestMethod]
        [DataRow("GET https://example.com", "GET")]
        [DataRow("Get https://example.com", "Get")]
        [DataRow("OPTIONS https://example.com", "OPTIONS")]
        [DataRow("options https://example.com", "options")]
        public void it_can_parse_verbs_regardless_of_their_casing(string line, string method)
        {
            var result = Parse(line);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Text.Should().Be(method);
        }

        [TestMethod]
        public void Unrecognized_verb_produces_a_diagnostic()
        {
            var result = Parse("OOPS https://example.com");

            var diagnostic = result.GetDiagnostics().Should().ContainSingle().Which;

            diagnostic.GetMessage().Should().Be("Unrecognized HTTP verb 'OOPS'.");

            var lineSpan = diagnostic.Location.GetLineSpan();
            lineSpan.StartLinePosition.Line.Should().Be(0);
            lineSpan.StartLinePosition.Character.Should().Be(0);
            lineSpan.EndLinePosition.Line.Should().Be(0);
            lineSpan.EndLinePosition.Character.Should().Be(4);
        }
    }
}
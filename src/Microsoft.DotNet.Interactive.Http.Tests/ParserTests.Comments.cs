// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    [TestClass]
    public class Comments
    {
        [TestMethod]
        [DataRow("""
                    // #This is a comment
                    GET https://example.com
                    """)]
        [DataRow("""
                    // // // // This is a comment
                    GET https://example.com
                    """)]  
        [DataRow("""
                    # // This is a comment
                    GET https://example.com
                    """)]
        [DataRow("""
                    # This is a comment
                    GET https://example.com
                    """)]
        public void line_comment_before_method_and_url_is_parsed_correctly(string code)
        {
            var result = Parse(code);

            var expectedCommentText = code.Split(['\n', '\r'])[0];

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>()
                                    .Which;

            requestNode.UrlNode.Text.Should().Be("https://example.com");

            requestNode.ChildNodes.Should().ContainSingle<HttpCommentNode>()
                       .Which.Text.Should().Be(expectedCommentText);
        }

        [TestMethod]
        [DataRow("""
                    https://example.com
                    #
                    """, "#")]
        [DataRow("""
                    https://example.com
                    //
                    """, "//")]
        public void comment_without_text_at_end_of_request_is_parsed_correctly(string code, string expectedCommentText)
        {

            var result = Parse(code);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should()
                  .ContainSingle<HttpCommentNode>()
                  .Which.Text.Should().Be(expectedCommentText);
        }

        [TestMethod]
        public void Comment_node_can_immediately_follow_headers()
        {
            var code = """
                GET https://example.com
                Accept: text/plain
                # This is a comment

                this is the body
                """;

            var result = Parse(code);

            result.GetDiagnostics().Should().BeEmpty();

            result.SyntaxTree.RootNode.DescendantNodesAndTokens()
                  .Should().ContainSingle<HttpCommentNode>()
                  .Which.Text.Should().Be("# This is a comment");
        }

        [TestMethod]
        [DataRow("""
                # This is a comment
                """)]
        [DataRow("""
                # This is a comment
                # This is the second line of the comment
                """)]
        [DataRow("""
                # This is a comment
                # This is the second line of the comment
                # This is the third line of the comment
                """)]
        [DataRow("""
                GET https://example.com
                # This is a comment
                # This is the second line of the comment
                """)]
        [DataRow("""
                # This is a comment

                # This is the second line of the comment
                """)]
        [DataRow("""
                # This is a comment

                # This is the second line of the comment

                # This is the third line of the comments
                """)]
        public void Comment_node_without_request_node_does_not_produce_diagnostics(string code)
        {
            var result = Parse(code);

            result.GetDiagnostics().Should().BeEmpty();

        }

        [TestMethod]
        public void comment_after_request_separator_is_parsed_correctly()
        {
            var code = """
                @MyRestaurantApi_HostAddress = https://localhost:7094

                GET {{MyRestaurantApi_HostAddress}}/api/Contact

                ###

                # get a specific contact
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.ChildNodes.Last().Should().BeOfType<HttpCommentNode>().Which.Text.Should().Be("# get a specific contact");
        }
    }
}
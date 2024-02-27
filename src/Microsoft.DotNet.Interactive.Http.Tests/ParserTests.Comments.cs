// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    public class Comments
    {
        [Fact]
        public void line_comment_before_method_and_url_is_parsed_correctly()
        {
            var code = """
                # This is a comment
                GET https://example.com
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.ChildNodes
                  .Should().ContainSingle<HttpRequestNode>()
                  .Which.ChildNodes.Should().ContainSingle<HttpCommentNode>()
                  .Which.Text.Should().Be("# This is a comment");
        }

        [Fact]
        public void comment_without_text_at_end_of_request_is_parsed_correctly()
        {
            var code = """
                https://example.com
                #
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should()
                  .ContainSingle<HttpCommentNode>()
                  .Which.Text.Should().Be("#");
        }

        [Fact]
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

        [Fact]
        public void Comment_node_without_request_node_does_not_produce_diagnostics()
        {
            var code = """
                # This is a comment
                """;

            var result = Parse(code);

            result.GetDiagnostics().Should().BeEmpty();

        }
    }
}
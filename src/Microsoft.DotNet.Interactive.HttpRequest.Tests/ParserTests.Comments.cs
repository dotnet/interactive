// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Comments
    {
        [Fact]
        public void comments_are_parsed_correctly()
        {
            var result = Parse(
                """
                # This is a comment
                GET https://example.com HTTP/1.1"
                """);

            var methodNode = result.SyntaxTree.RootNode
                                   .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                                   .MethodNode;

            methodNode.ChildNodes.Should().ContainSingle<HttpCommentNode>().Which.Text.Should().Be(
                "# This is a comment");
        }
    }
}
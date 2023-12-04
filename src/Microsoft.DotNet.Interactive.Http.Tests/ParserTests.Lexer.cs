// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    public class Lexer
    {
        [Fact]
        public void multiple_whitespaces_are_treated_as_a_single_token()
        {
            var result = Parse("  \t  ");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildTokens.First().Should().BeOfType<HttpSyntaxToken>();

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildTokens.Single().Text.Should().Be("  \t  ");
        }

        [Fact]
        public void multiple_newlines_are_parsed_into_different_tokens()
        {
            var result = Parse("\n\v\r\n\n");

            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildTokens.Select(t => new { t.Text, t.Kind }).Should().BeEquivalentSequenceTo(
                      new { Text = "\n", Kind = HttpTokenKind.NewLine },
                      new { Text = "\v", Kind = HttpTokenKind.NewLine },
                      new { Text = "\r\n", Kind = HttpTokenKind.NewLine },
                      new { Text = "\n", Kind = HttpTokenKind.NewLine });
        }

        [Fact]
        public void multiple_punctuations_are_parsed_into_different_tokens()
        {
            var result = Parse(".!?.:/");
            
            var requestNode = result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should().ContainSingle<HttpBodyNode>().Which;
            requestNode
                  .ChildTokens.Select(t => new { t.Text, t.Kind })
                  .Should().BeEquivalentSequenceTo(
                      new { Text = ".", Kind = HttpTokenKind.Punctuation },
                      new { Text = "!", Kind = HttpTokenKind.Punctuation },
                      new { Text = "?", Kind = HttpTokenKind.Punctuation },
                      new { Text = ".", Kind = HttpTokenKind.Punctuation },
                      new { Text = ":", Kind = HttpTokenKind.Punctuation },
                      new { Text = "/", Kind = HttpTokenKind.Punctuation });
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Lexer
    {
        [Fact]
        public void multiple_whitespaces_are_treated_as_a_single_token()
        {
            var result = Parse("  \t  ");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.ChildTokens.First().Should().BeOfType<HttpSyntaxToken>();

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.ChildTokens.Single().Text.Should().Be("  \t  ");
        }

        [Fact]
        public void multiple_newlines_are_parsed_into_different_tokens()
        {
            var result = Parse("\n\v\r\n\n");

            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.ChildTokens.Select(t => new { t.Text, t.Kind }).Should().BeEquivalentSequenceTo(
                      new { Text = "\n", Kind = HttpTokenKind.NewLine },
                      new { Text = "\v", Kind = HttpTokenKind.NewLine },
                      new { Text = "\r\n", Kind = HttpTokenKind.NewLine },
                      new { Text = "\n", Kind = HttpTokenKind.NewLine });
        }

        [Fact]
        public void multiple_punctuations_are_parsed_into_different_tokens()
        {
            var result = Parse(".!?.:/");
            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .UrlNode.ChildTokens.Select(t => new { t.Text, t.Kind }).Should().BeEquivalentSequenceTo(
                      new { Text = ".", Kind = HttpTokenKind.Punctuation },
                      new { Text = "!", Kind = HttpTokenKind.Punctuation },
                      new { Text = "?", Kind = HttpTokenKind.Punctuation },
                      new { Text = ".", Kind = HttpTokenKind.Punctuation },
                      new { Text = ":", Kind = HttpTokenKind.Punctuation },
                      new { Text = "/", Kind = HttpTokenKind.Punctuation });
        }
    }
}
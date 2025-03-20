// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    [TestClass]
    public class Lexer
    {
        [TestMethod]
        public void multiple_whitespaces_are_treated_as_a_single_token()
        {
            var result = Parse("  \t  ");

            result.SyntaxTree.RootNode
                  .ChildTokens.First().Should().BeOfType<SyntaxToken>();

            result.SyntaxTree.RootNode
                  .ChildTokens.Single().Text.Should().Be("  \t  ");
        }

        [TestMethod]
        public void multiple_newlines_are_parsed_into_different_tokens()
        {
            var result = Parse("\n\v\r\n\n");

            result.SyntaxTree.RootNode
                  .ChildTokens.Select(t => new { t.Text, t.Kind }).Should().BeEquivalentSequenceTo(
                      new { Text = "\n", Kind = TokenKind.NewLine },
                      new { Text = "\v", Kind = TokenKind.NewLine },
                      new { Text = "\r\n", Kind = TokenKind.NewLine },
                      new { Text = "\n", Kind = TokenKind.NewLine });
        }

        [TestMethod]
        public void multiple_punctuations_are_parsed_into_different_tokens()
        {
            var result = Parse(".!?.:/");

            var requestNode = result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should().ContainSingle<HttpUrlNode>().Which;
            requestNode
                  .ChildTokens.Select(t => new { t.Text, t.Kind })
                  .Should().BeEquivalentSequenceTo(
                      new { Text = ".", Kind = TokenKind.Punctuation },
                      new { Text = "!", Kind = TokenKind.Punctuation },
                      new { Text = "?", Kind = TokenKind.Punctuation },
                      new { Text = ".", Kind = TokenKind.Punctuation },
                      new { Text = ":", Kind = TokenKind.Punctuation },
                      new { Text = "/", Kind = TokenKind.Punctuation });
        }
    }
}
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
    public class Trivia
    {
        [Fact]
        public void it_can_parse_an_empty_string()
        {
            var result = Parse("");
            result.SyntaxTree.Should().NotBeNull();
        }

        [Fact]
        public void it_can_parse_a_string_with_only_whitespace()
        {
            var result = Parse(" \t ");

            result.SyntaxTree.RootNode
                  .ChildTokens.First().Text.Should().Be(" \t ");
        }

        [Fact]
        public void it_can_parse_a_string_with_only_newlines()
        {
            var result = Parse("\r\n\n\r\n");

            result.SyntaxTree.RootNode
                  .FullText.Should().Be("\r\n\n\r\n");
        }
    }
}
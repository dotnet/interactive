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
    public class Trivia
    {
        [TestMethod]
        public void it_can_parse_an_empty_string()
        {
            var result = Parse("");
            result.SyntaxTree.Should().NotBeNull();
        }

        [TestMethod]
        public void it_can_parse_a_string_with_only_whitespace()
        {
            var result = Parse(" \t ");

            result.SyntaxTree.RootNode
                  .ChildTokens.First().Text.Should().Be(" \t ");
        }

        [TestMethod]
        public void string_with_only_newlines_is_parsed_into_root_node()
        {
            var result = Parse("\r\n\n\r\n");

            result.SyntaxTree.RootNode
                  .FullText.Should().Be("\r\n\n\r\n");
        }
    }
}
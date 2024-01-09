// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class DirectiveOptions
    {
        [Fact]
        public void Words_prefixed_with_hyphens_are_parsed_into_option_name_nodes()
        {
            var tree = Parse("#!directive --option");

            var optionNode = tree.RootNode.DescendantNodesAndTokens()
                                 .Should().ContainSingle<DirectiveOptionNode>()
                                 .Which;

            optionNode.OptionNameNode.Text.Should().Be("--option");
        }

        [Fact]
        public void Words_prefixed_with_hyphens_are_parsed_into_argument_nodes()
        {
            var tree = Parse("#!directive --option argument");

            var argumentNode = tree.RootNode.DescendantNodesAndTokens()
                                   .Should().ContainSingle<DirectiveArgumentNode>()
                                   .Which;

            argumentNode.Text.Should().Be("argument");
        }
    }
}
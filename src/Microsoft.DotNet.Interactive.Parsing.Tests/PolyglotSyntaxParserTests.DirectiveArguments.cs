// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    // FIX: (PolyglotSyntaxParserTests) combine into (and rename) named parameter tests if this approach pans out

    public class DirectiveArguments
    {
        [Theory]
        [InlineData("""
            #!directive --option "this is the argument"
            """)]
        [InlineData("""
            #!directive "this is the argument"
            """)]
        public void Quoted_arguments_can_include_whitespace(string code)
        {
            var tree = Parse(code);

            var argumentNode = tree.RootNode.DescendantNodesAndTokens()
                                   .Should().ContainSingle<DirectiveParameterValueNode>()
                                   .Which;

            argumentNode.Text.Should().Be("\"this is the argument\"");
        }
    }
}
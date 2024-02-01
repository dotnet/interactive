// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class Subcommands
    {
        readonly PolyglotParserConfiguration _config = new("csharp")
        {
            KernelInfos =
            {
                ["csharp"] = new("csharp")
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#!test")
                        {
                            Subcommands =
                            {
                                new("one")
                                {
                                    NamedParameters =
                                    {
                                        new("--opt-one")
                                    }
                                },
                                new("two")
                                {
                                    NamedParameters =
                                    {
                                        new("--opt-two")
                                    }
                                },
                            },
                            NamedParameters =
                            {
                                new("--opt")
                            },
                            Parameters =
                            {
                                new("value")
                            }
                        }
                    }
                }
            }
        };

        [Fact]
        public void Subcommand_can_be_parsed()
        {
            var tree = Parse("""
                #!test one
                """, _config);

            tree.RootNode.GetDiagnostics().Should().BeEmpty();

            tree.RootNode.DescendantNodesAndTokens()
                .Should()
                .ContainSingle<DirectiveSubcommandNode>()
                .Which.Text.Should().Be("one");
        }
    }
}
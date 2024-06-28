// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class Completions
    {
        public class Directives
        {
            [Theory]
            [InlineData("#!$$", new[]{"#!connect", "#!set", "#!who"})]
            [InlineData("#!conn$$", new[]{"#!connect"})]
            public async Task produce_completions_for_partiaL_text(string markupCode, string[] expectedCompletions)
            {
                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(expectedCompletions);
            }

            [Theory]
            [InlineData("#!connect $$")]
            [InlineData("#!connect          $$")]
            public async Task produce_completions_for_subcommands(string markupCode)
            {
                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(
                    [
                        "named-pipe",
                        "stdio",
                        "signalr",
                        "jupyter",
                        "mssql",
                    ]);
            }

            [Theory]
            [InlineData("#!connect jupyter $$")]
            [InlineData("#!connect   jupyter       $$")]
            public async Task produce_completions_for_parameter_names(string markupCode)
            {
                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(["--kernel-name"]);
            }

            [Fact]
            public async Task produce_completions_for_parameter_values_when_parameter_allows_implicit_name()
            {
                var config = new PolyglotParserConfiguration("csharp")
                {
                    KernelInfos =
                    {
                        new("csharp")
                        {
                            SupportedDirectives =
                            [
                                new KernelActionDirective("#!test")
                                {
                                    Parameters =
                                    [
                                        new KernelDirectiveParameter("--parameter")
                                                { AllowImplicitName = true }
                                            .AddCompletions(_ => ["one", "two", "three"]),
                                        new("--other-parameter")
                                    ]
                                }
                            ]
                        }
                    }
                };

                MarkupTestFile.GetPosition("#!test  $$", out var code, out var position);

                var tree = Parse(code, config);

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(["one", "two", "three"]);
            }

            [Theory]
            [InlineData("#!test $$")]
            [InlineData("#!test $$  subcommand  ")]
            public async Task do_not_produce_parameter_completions_before_a_subcommand(string markupCode)
            {
                var config = new PolyglotParserConfiguration("csharp")
                {
                    KernelInfos =
                    {
                        new("csharp")
                        {
                            SupportedDirectives =
                            [
                                new KernelActionDirective("#!test")
                                {
                                    Parameters =
                                    [
                                        new("--parameter")
                                    ],
                                    Subcommands =
                                    [
                                        new("subcommand")
                                        {
                                            Parameters =
                                            [
                                                new KernelDirectiveParameter("--subcommand-parameter"),
                                            ]
                                        }
                                    ],
                                }
                            ]
                        }
                    }
                };

                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, config);

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().NotContain("--parameter");
            }
        }

        public class Subcommands
        {
            [Theory]
            [InlineData("#!connect mssql $$")]
            [InlineData("#!connect mssql         $$")]
            public async Task produce_completions_for_parameter_names(string markupCode)
            {
                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(
                [
                    "--kernel-name",
                    "--connection-string"
                ]);
            }

            [Theory]
            [InlineData("#!connect mssql $$")]
            [InlineData("#!connect mssql         $$")]
            public async Task do_not_produce_completions_for_sibling_subcommands(string markupCode)
            {
                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                var displayTextValues = completions.Select(c => c.DisplayText);

                displayTextValues.Should().NotContain("jupyter");
            }

            [Fact]
            public async Task produce_completions_for_parameter_values_when_parameter_allows_implicit_name()
            {
                var config = new PolyglotParserConfiguration("csharp")
                {
                    KernelInfos =
                    {
                        new("csharp")
                        {
                            SupportedDirectives =
                            [
                                new KernelActionDirective("#!test")
                                {
                                    Subcommands =
                                    [
                                        new("subcommand")
                                        {
                                            Parameters =
                                            [
                                                new KernelDirectiveParameter("--parameter")
                                                        { AllowImplicitName = true }
                                                    .AddCompletions(_ => ["one", "two", "three"]),
                                                new("--other-parameter")
                                            ]
                                        }
                                    ],
                                }
                            ]
                        }
                    }
                };

                MarkupTestFile.GetPosition("#!test subcommand $$", out var code, out var position);

                var tree = Parse(code, config);

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(["one", "two", "three"]);
            }
        }

        public class Parameters
        {
            [Fact]
            public async Task produce_completions_for_parameter_values()
            {
                var config = new PolyglotParserConfiguration("csharp")
                {
                    KernelInfos =
                    {
                        new("csharp")
                        {
                            SupportedDirectives =
                            [
                                new KernelActionDirective("#!test")
                                {
                                    Parameters =
                                    [
                                        new KernelDirectiveParameter("--parameter").AddCompletions(_ => ["one", "two", "three"])
                                    ]
                                }
                            ]
                        }
                    }
                };

                MarkupTestFile.GetPosition("#!test --parameter $$", out var code, out var position);

                var tree = Parse(code, config);

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(["one", "two", "three"]);
            }
        }

        // FIX: (Completions) test partial words and filtering
        // FIX: (Completions) test values for parameters allowing implicit names
    }
}
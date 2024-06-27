// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [Theory]
        [InlineData("#!connect $$")]
        [InlineData("#!connect          $$")]
        public async Task Completions_are_produced_for_subcommands_under_directive_within_whitespace(string markupCode)
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
                ]
            );
        }

        [Theory]
        [InlineData("#!connect $$")]
        [InlineData("#!connect          $$")]
        public async Task Completions_are_produced_for_parameter_names_under_directive_within_whitespace(string markupCode)
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
        public async Task Completions_are_produced_for_parameter_values_under_parameter_within_whitespace()
        {
            MarkupTestFile.GetPosition("#!test --parameter $$", out var code, out var position);

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

            var tree = Parse(code, config);

            var node = tree.RootNode.FindNode(position.Value)
                           .AncestorsAndSelf()
                           .OfType<DirectiveNode>()
                           .First();

            var completions = await node.GetCompletionsAtPositionAsync(position.Value);

            completions.Select(c => c.DisplayText).Should().Contain(["one", "two", "three"]);
        }

        // FIX: (Completions) Completions_are_produced_for_partial_subcommands
        // FIX: (Completions) Completions_are_produced_for_parameter_names
        // FIX: (Completions) Values_are_suggested_for_parameters_that_allow_implicit_names
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    [TestClass]
    public class Completions
    {
        [TestClass]
        public class Directives
        {
            [TestMethod]
            [DataRow("#!$$", new[]{"#!connect", "#!set", "#!who"})]
            [DataRow("#!conn$$", new[]{"#!connect"})]
            public async Task produce_completions_for_partial_text(string markupCode, string[] expectedCompletions)
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

            [TestMethod]
            [DataRow("#!connect $$")]
            [DataRow("#!connect          $$")]
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

            [TestMethod]
            [DataRow("#!connect jupyter $$",
                        new[] { "--kernel-name" })]
            [DataRow("#!connect jupyter       $$",
                        new[] { "--kernel-name" })]
            [DataRow("#!connect mssql  $$ --create-dbcontext",
                        new[] { "--kernel-name" })]
            [DataRow("#!connect mssql --create-dbcontext      $$",
                        new[] { "--kernel-name" })]
            [DataRow("""#!connect mssql --connection-string @input:{"saveAs":"mydbconnectionstring"}  $$""",
                        new[] { "--kernel-name" })]
            [DataRow("#!connect jupyter --kernel-name asdf $$",
                        new[]
                        {
                            "--url", "--kernel-spec", "--init-script", "--conda-env", "--bearer"
                        })]
            [DataRow("#!connect jupyter --kernel-name @input $$",
                        new[]
                        {
                            "--url", "--kernel-spec", "--init-script", "--conda-env", "--bearer"
                        })]
            [DataRow("""#!connect jupyter --kernel-name @input:{"saveAs":"xyz"} $$""",
                        new[]
                        {
                            "--url", "--kernel-spec", "--init-script", "--conda-env", "--bearer"
                        })]
            [DataRow("#!connect jupyter $$ --kernel-name",
                        new[]
                        {
                            "--url", "--kernel-spec", "--init-script", "--conda-env", "--bearer"
                        })]
            [DataRow("#!set --name @input $$",
                        new[]
                        {
                            "--value", "--byref", "--mime-type"
                        })]
            [DataRow("""#!set --name @input:{"saveAs":"xyz"} $$""",
                        new[]
                        {
                            "--value", "--byref", "--mime-type"
                        })]
            public async Task produce_completions_for_parameter_names(
                string markupCode,
                string[] expectedParameterNames)
            {
                MarkupTestFile.GetPosition(markupCode, out var code, out var position);

                var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

                var node = tree.RootNode.FindNode(position.Value)
                               .AncestorsAndSelf()
                               .OfType<DirectiveNode>()
                               .First();

                var completions = await node.GetCompletionsAtPositionAsync(position.Value);

                completions.Select(c => c.DisplayText).Should().Contain(expectedParameterNames);
            }

            [TestMethod]
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
                                            {
                                                AllowImplicitName = true
                                            }
                                            .AddCompletions(() => ["one", "two", "three"]),
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

            [TestMethod]
            [DataRow("#!test $$")]
            [DataRow("#!test $$  subcommand  ")]
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

        [TestClass]
        public class Subcommands
        {
            [TestMethod]
            [DataRow("#!connect mssql $$")]
            [DataRow("#!connect mssql         $$")]
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

            [TestMethod]
            [DataRow("#!connect mssql $$")]
            [DataRow("#!connect mssql         $$")]
            [DataRow("#!connect mssql --connection-string   @input $$")]
            [DataRow("#!connect mssql --connection-string abc $$")]
            [DataRow("#!connect mssql --create-dbcontext  $$  @input")]
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

            [TestMethod]
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
                                                    .AddCompletions(() => ["one", "two", "three"]),
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

        [TestClass]
        public class Parameters
        {
            [TestMethod]
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
                                        new KernelDirectiveParameter("--parameter").AddCompletions(() => ["one", "two", "three"])
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

        [TestMethod]
        [DataRow("#!set --name xyz $$")]
        [DataRow("#!set $$ --name xyz")]
        public async Task Parameter_names_are_not_suggested_when_they_have_reached_their_max_occurrences(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out var code, out var position);

            var tree = Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration());

            var node = tree.RootNode.FindNode(position.Value)
                           .AncestorsAndSelf()
                           .OfType<DirectiveNode>()
                           .First();

            var completions = await node.GetCompletionsAtPositionAsync(position.Value);

            completions.Should().Contain(c => c.InsertText == "--value");
            completions.Should().NotContain(c => c.InsertText == "--name");
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    [TestClass]
    public class Subcommands
    {
        private readonly TestContext _output;

        public Subcommands(TestContext output)
        {
            _output = output;
        }

        readonly PolyglotParserConfiguration _config = new("csharp")
        {
            KernelInfos =
            {
                new("csharp")
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#!test")
                        {
                            Subcommands =
                            {
                                new("one")
                                {
                                    Parameters =
                                    {
                                        new("--opt-one")
                                    }
                                },
                                new("two")
                                {
                                    Parameters =
                                    {
                                        new("--opt-two")
                                    }
                                },
                            },
                            Parameters =
                            {
                                new("--opt")
                            }
                        }
                    }
                }
            }
        };

        [TestMethod]
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

        [TestMethod]
        public void Subcommand_parameters_must_appear_after_subcommands()
        {
            var markupCode = """
                #!test --opt arg [|one|]
                """;
            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = Parse(code, _config);

            _output.WriteLine(tree.RootNode.Diagram());

            var diagnostics = tree.RootNode.GetDiagnostics();

            var diagnostic = diagnostics
                             .Should()
                             .ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
                             .Which;

            diagnostic.GetMessage().Should().Be("Parameters must appear after subcommands.");

            diagnostic
                .Location
                .GetLineSpan()
                .StartLinePosition
                .Character
                .Should()
                .Be(span.Start);

            diagnostic
                .Location
                .GetLineSpan()
                .EndLinePosition
                .Character
                .Should()
                .Be(span.End);
        }

        [TestMethod]
        [DataRow("""
                #!connect mssql --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false" --kernel-name sql-adventureworks 
                """)]
        [DataRow("""
                #!connect mssql  --kernel-name sql-adventureworks  --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false"
                """)]
        public void Parameters_defined_on_parent_command_are_valid_on_subcommand(string code)
        {
            var tree = Parse(code);

            _output.WriteLine(tree.RootNode.Diagram());

            tree.RootNode.DescendantNodesAndTokens()
                .Should()
                .ContainSingle<DirectiveParameterNameNode>(where: node => node.Text == "--kernel-name");

            tree.RootNode.GetDiagnostics().Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("""
                    #!connect mssql --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false" --kernel-name sql-adventureworks
                    """)]
        [DataRow("""
                    #!connect mssql  --kernel-name sql-adventureworks  --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false"
                    """)]
        public void Parameters_defined_on_subcommand_are_parented_to_the_subcommand_node(string code)
        {
            var tree = Parse(code);

            _output.WriteLine(tree.RootNode.Diagram());

            tree.RootNode.DescendantNodesAndTokens()
                .Should()
                .ContainSingle<DirectiveParameterNameNode>(where: node => node.Text == "--connection-string")
                .Which
                .Ancestors().OfType<DirectiveSubcommandNode>().Should().ContainSingle().Which.NameNode.Text.Should().Be("mssql");

            tree.RootNode.GetDiagnostics().Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("sub-command")]
        [DataRow("sub_command")]
        [DataRow("sub_com-mand")]
        public void Subcommands_can_contain_hyphens_and_underscores(string subcommandName)
        {
            var tree = Parse($"""
                             #!test {subcommandName} --opt 123
                             """, new PolyglotParserConfiguration("csharp")
            {
                KernelInfos =
                {
                    new("csharp")
                    {
                        SupportedDirectives =
                        {
                            new KernelActionDirective("#!test")
                            {
                                Subcommands =
                                {
                                    new(subcommandName)
                                    {
                                        Parameters =
                                        {
                                            new("--opt")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            });

            tree.RootNode
                .DescendantNodesAndTokens().Should().ContainSingle<DirectiveSubcommandNode>()
                .Which
                .NameNode.Text
                .Should().Be(subcommandName);
            tree.RootNode.GetDiagnostics().Should().BeEmpty();
        }

        [TestMethod]
        public void Grandchild_subcommands_cannot_be_added()
        {
            var directive = new KernelActionDirective("#!test");

            var childDirective = new KernelActionDirective("one");
            directive.Subcommands.Add(childDirective);
            var grandchildDirective = new KernelActionDirective("two");

            var addGrandchild = () =>
                childDirective.Subcommands.Add(grandchildDirective);

            addGrandchild.Should().Throw<ArgumentException>()
                         .Which.Message
                         .Should().Be("Only one level of directive subcommands is allowed.");
        }

        [TestMethod]
        public void Directives_with_subcommands_cannot_be_added_to_a_parent_directive()
        {
            var childDirective = new KernelActionDirective("one");
            var grandchildDirective = new KernelActionDirective("two");
            childDirective.Subcommands.Add(grandchildDirective);

            var parent = new KernelActionDirective("#!test");
            var addToParent = () =>
                parent.Subcommands.Add(childDirective);

            addToParent.Should().Throw<ArgumentException>()
                       .Which.Message
                       .Should().Be("Only one level of directive subcommands is allowed.");
        }

        [TestMethod]
        public void Commands_cannot_be_reparented()
        {
            var directive = new KernelActionDirective("#!test");

            var childDirective = new KernelActionDirective("one");
            directive.Subcommands.Add(childDirective);

            var newParent = new KernelActionDirective("#!test2");

            var addToNewParent = () =>
                newParent.Subcommands.Add(childDirective);

            addToNewParent.Should().Throw<ArgumentException>()
                          .Which.Message
                          .Should().Be("Directives cannot be reparented.");
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class Subcommands
    {
        private readonly ITestOutputHelper _output;

        public Subcommands(ITestOutputHelper output)
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

        [Fact]
        public void Parameters_cannot_appear_after_subcommands()
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

        [Fact]
        public void Parameters_defined_on_parent_command_are_valid_on_subcommand()
        {
            var tree = Parse("""
                #!connect mssql  --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false" --kernel-name sql-adventureworks 
                """);

            _output.WriteLine(tree.RootNode.Diagram());

            tree.RootNode.DescendantNodesAndTokens()
                .Should()
                .ContainSingle<DirectiveParameterNameNode>(where: node => node.Text == "--kernel-name");

            tree.RootNode.GetDiagnostics().Should().BeEmpty();
        }

        [Fact]
        public void Multiple_levels_of_subcommands_are_not_supported()
        {
            

            // TODO (Multiple_levels_of_subcommands_are_not_supported) write test
            throw new NotImplementedException();
        }
    }
}
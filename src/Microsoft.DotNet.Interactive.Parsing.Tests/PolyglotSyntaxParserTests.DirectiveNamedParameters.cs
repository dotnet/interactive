// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class DirectiveNamedParameters
    {
        [Fact]
        public void Words_prefixed_with_hyphens_are_parsed_into_option_name_nodes()
        {
            var tree = Parse("#!directive --option");

            var optionNode = tree.RootNode.DescendantNodesAndTokens()
                                 .Should().ContainSingle<DirectiveNamedParameterNode>()
                                 .Which;

            optionNode.NameNode.Text.Should().Be("--option");
        }

        [Fact]
        public void Words_prefixed_with_hyphens_are_parsed_into_argument_nodes()
        {
            var tree = Parse("#!directive --option argument");

            var argumentNode = tree.RootNode.DescendantNodesAndTokens()
                                   .Should().ContainSingle<DirectiveParameterNode>()
                                   .Which;

            argumentNode.Text.Should().Be("argument");
        }

        [Fact]
        public void Errors_for_unknown_options_are_available_as_diagnostics()
        {
            var markupCode = """
            #!csharp [|--invalid-option|]
            var x = 1;
            """;

            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = Parse(code);

            var node = tree.RootNode
                           .ChildNodes
                           .Should()
                           .ContainSingle<DirectiveNode>()
                           .Which;

            IEnumerable<Diagnostic> diagnostics = node.GetDiagnostics();

            var diagnostic = diagnostics
                             .Should()
                             .ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
                             .Which;

            diagnostic.GetMessage().Should().Be("Unknown named parameter '--invalid-option'");

            diagnostic
                .Location
                .GetLineSpan()
                .EndLinePosition
                .Character
                .Should()
                .Be(span.End);

            diagnostic
                .Location
                .GetLineSpan()
                .StartLinePosition
                .Character
                .Should()
                .Be(span.Start);
        }

        [Fact]
        public void When_there_are_too_many_occurrences_then_an_error_is_produced()
        {
            PolyglotParserConfiguration config = new("csharp")
            {
                KernelInfos =
                {
                    ["csharp"] = new("csharp")
                    {
                        SupportedDirectives =
                        {
                            new KernelActionDirective("#!test")
                            {
                                NamedParameters =
                                {
                                    new("--opt")
                                    {
                                        MaxOccurrences = 2
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var markupCode = """
                #!test [|--opt one|] [|--opt two|] [|--opt three|]
                """;

            MarkupTestFile.GetSpans(markupCode, out var code, out var spans);

            var tree = Parse(code, config);

            var diagnostics = tree.RootNode.GetDiagnostics().ToArray();

            diagnostics.Should().HaveCount(3);

            for (var i = 0; i < diagnostics.Length; i++)
            {
                var diagnostic = diagnostics[i];

                diagnostic.GetMessage().Should().Be("A maximum of 2 occurrences are allowed for named parameter '--opt'");

                var span = spans[i];

                diagnostic
                    .Location
                    .GetLineSpan()
                    .EndLinePosition
                    .Character
                    .Should()
                    .Be(span.End);

                diagnostic
                    .Location
                    .GetLineSpan()
                    .StartLinePosition
                    .Character
                    .Should()
                    .Be(span.Start);
            }
        }

        [Fact]
        public void When_there_are_not_enough_occurrences_then_an_error_is_produced()
        {
            PolyglotParserConfiguration config = new("csharp")
            {
                KernelInfos =
                {
                    ["csharp"] = new("csharp")
                    {
                        SupportedDirectives =
                        {
                            new KernelActionDirective("#!test")
                            {
                                NamedParameters =
                                {
                                    new("--opt")
                                    {
                                        Required = true
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var code = "#!test";

            var tree = Parse(code, config);

            var diagnostic = tree.RootNode.GetDiagnostics().Should().ContainSingle().Which;

            diagnostic.GetMessage().Should().Be("Missing required named parameter '--opt'");

            diagnostic
                .Location
                .GetLineSpan()
                .StartLinePosition
                .Character
                .Should()
                .Be(0);

            diagnostic
                .Location
                .GetLineSpan()
                .EndLinePosition
                .Character
                .Should()
                .Be(code.Length);
        }
        
    }
}
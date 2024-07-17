// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class Expressions
    {
        [Theory]
        [InlineData(
            """
            #!set --name myVar --value @input:"Please enter the value"
            """,
            "\"Please enter the value\"")]
        [InlineData(
            """
            #!set --name myVar --value @input:blah
            """,
            "blah")]
        [InlineData(
            """
            #!set --name myVar --value @input:{ "prompt": "Please enter the fruit name", "typeHint": "text", "recall": true }
            """,
            """{ "prompt": "Please enter the fruit name", "typeHint": "text", "recall": true }""")]
        public void Input_tokens_are_parsed_as_input_token_name_nodes(string code, string expectedParameters)
        {
            var tree = Parse(code);

            var descendantNodesAndTokens = tree.RootNode
                                               .DescendantNodesAndTokens()
                                               .OfType<SyntaxNode>()
                                               .ToArray();

            var valueNode = descendantNodesAndTokens
                            .Should()
                            .ContainSingle<DirectiveParameterNode>(node => node.NameNode?.Text == "--value")
                            .Which
                            .ValueNode;

            var expressionTypeNode = valueNode.DescendantNodesAndTokens()
                                              .Should().ContainSingle<DirectiveExpressionTypeNode>()
                                              .Which;

            expressionTypeNode.Text.Should().Be("@input:");
            expressionTypeNode.Type.Should().Be("input");

            valueNode.DescendantNodesAndTokens()
                     .Should().ContainSingle<DirectiveExpressionParametersNode>()
                     .Which.Text
                     .Should().Be(expectedParameters);
        }

        [Fact]
        public void Diagnostics_are_produced_for_invalid_JSON()
        {
            PolyglotParserConfiguration config = new("csharp")
            {
                KernelInfos =
                {
                    new("csharp")
                    {
                        SupportedDirectives =
                        {
                            new KernelActionDirective("#!test")
                            {
                                Parameters =
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

            var markupCode = """
                
                #!test --opt  @input:{ "fruit":   [|cherry|] } 

                
                """;

            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = PolyglotSyntaxParser.Parse(code, config);

            var diagnostic = tree.RootNode.GetDiagnostics().Should().ContainSingle().Which;

            diagnostic.GetMessage().Should().Be("Invalid JSON: 'c' is an invalid start of a value.");

            var linePositionSpan = diagnostic.Location.GetLineSpan();

            linePositionSpan.StartLinePosition.Line.Should().Be(1);
            linePositionSpan.EndLinePosition.Line.Should().Be(1);

            diagnostic.Location.SourceSpan.Start
                      .Should()
                      .Be(span.Start);

            diagnostic.Location.SourceSpan.End
                      .Should()
                      .Be(span.End);
        }
    }
}
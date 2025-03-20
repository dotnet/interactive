// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

[TestClass]
public partial class PolyglotSyntaxParserTests
{
    [TestClass]
    public class DirectiveParameters
    {
        private readonly TestContext _output;

        public DirectiveParameters(TestContext output)
        {
            _output = output;
        }

        [TestMethod]
        public void Words_prefixed_with_hyphens_are_parsed_into_parameter_name_nodes()
        {
            var tree = Parse("#!directive --option");

            var parameterNode = tree.RootNode.DescendantNodesAndTokens()
                                    .Should().ContainSingle<DirectiveParameterNode>()
                                    .Which;

            parameterNode.NameNode.Text.Should().Be("--option");
        }

        [TestMethod]
        public void Words_prefixed_with_hyphens_are_parsed_into_parameter_value_nodes()
        {
            var tree = Parse("#!directive --option argument");

            var argumentNode = tree.RootNode.DescendantNodesAndTokens()
                                   .Should().ContainSingle<DirectiveParameterValueNode>()
                                   .Which;

            argumentNode.Text.Should().Be("argument");
        }

        [TestMethod]
        [DataRow("""
            #!directive --option "this is the argument"
            """)]
        [DataRow("""
            #!directive "this is the argument"
            """)]
        public void Quoted_values_can_include_whitespace(string code)
        {
            var tree = Parse(code);

            var argumentNode = tree.RootNode.DescendantNodesAndTokens()
                                   .Should().ContainSingle<DirectiveParameterValueNode>()
                                   .Which;

            argumentNode.Text.Should().Be("\"this is the argument\"");
        }

        [TestMethod]
        [DataRow("#!test --flag --param 123")]
        [DataRow("#!test --flag 123")] // implicit parameter... but this is really confusing to read
        public void Flag_does_not_consume_parameter_value(string code)
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
                                    new("--param")
                                    {
                                        AllowImplicitName = true
                                    },
                                    new("--flag")
                                    {
                                        Flag = true
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var tree = Parse(code, config);

            _output.WriteLine(tree.RootNode.Diagram());

            tree.RootNode.GetDiagnostics().Should().BeEmpty();

            tree.RootNode.DescendantNodesAndTokens()
                .Should()
                .ContainSingle<DirectiveParameterNode>(node => node.Text == "--flag" && 
                                                               node.ValueNode is null);
        }

        [TestMethod]
        public void Errors_for_unknown_parameter_names_are_available_as_diagnostics()
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

            diagnostic.GetMessage().Should().Be("Unrecognized parameter name '--invalid-option'");

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
        public void When_there_are_too_many_occurrences_then_an_error_is_produced()
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

        [TestMethod]
        [DataRow("#!test")]
        public void When_a_required_parameter_is_missing_then_an_error_is_produced(string code)
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
                                    new("--required")
                                    {
                                        Required = true
                                    },
                                    new("--not-required")
                                }
                            }
                        }
                    }
                }
            };

            var tree = Parse(code, config);

            tree.RootNode
                .GetDiagnostics()
                .Should()
                .ContainSingle()
                .Which
                .GetMessage()
                .Should()
                .Be("Missing required parameter '--required'");
        }

        [TestMethod]
        [DataRow("#!test --not-required 123 --required")]
        [DataRow("#!test --required --not-required 123")]
        public void When_the_value_for_a_required_parameter_is_missing_then_an_error_is_produced(string code)
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
                                    new("--required")
                                    {
                                        Required = true
                                    },
                                    new("--not-required")
                                }
                            }
                        }
                    }
                }
            };

            var tree = Parse(code, config);

            tree.RootNode
                .GetDiagnostics()
                .Should()
                .ContainSingle()
                .Which
                .GetMessage()
                .Should()
                .Be("Missing value for required parameter '--required'");
        }

        [TestMethod]
        public void When_a_required_parameter_on_a_subcommand_is_missing_then_an_error_is_produced()
        {
            var tree = Parse("#!connect jupyter --kernel-spec .net-csharp");

            tree.RootNode
                .GetDiagnostics()
                .Should()
                .ContainSingle()
                .Which
                .GetMessage()
                .Should()
                .Be("Missing required parameter '--kernel-name'");
        }

        [TestMethod]
        [DataRow("#!connect jupyter --kernel-spec .net-csharp --kernel-name")]
        [DataRow("#!connect jupyter --kernel-name --kernel-spec .net-csharp")]
        public void When_the_value_for_a_required_parameter_on_a_subcommand_is_missing_then_an_error_is_produced(string code)
        {
            var tree = Parse(code);

            tree.RootNode
                .GetDiagnostics()
                .Should()
                .ContainSingle()
                .Which
                .GetMessage()
                .Should()
                .Be("Missing value for required parameter '--kernel-name'");
        }

        [TestMethod]
        [DataRow("""
                    "just a JSON string"
                    """)]
        [DataRow("""
                    { "fruit": "cherry" }
                    """)]
        [DataRow("""
                    [ "a string", 123, { "fruit": "Granny Smith" } ]
                    """)]
        public void Inline_JSON_is_consumed_as_a_parameter_value(string jsonParameter)
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
          
            var tree = PolyglotSyntaxParser.Parse($"""
                                                   #!test --opt {jsonParameter} other-parameter
                                                   """, config);

            tree.RootNode.GetDiagnostics().Should().BeEmpty();

            tree.RootNode.DescendantNodesAndTokens()
                .Should().ContainSingle<DirectiveParameterNode>(where: node => node.NameNode?.Text == "--opt")
                .Which.ChildNodes
                .Should().ContainSingle<DirectiveParameterValueNode>()
                .Which.Text
                .Should().Be(jsonParameter);
        }

        [TestMethod]
        public void Diagnostics_are_produced_for_invalid_JSON()
        {
            PolyglotParserConfiguration config = new("csharp")
            {
                KernelInfos =
                {
                    new KernelInfo("csharp")
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
                
                
                #!test --opt { "fruit": [|cherry|] }
                
                """;

            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = PolyglotSyntaxParser.Parse(code, config);

            var diagnostic = tree.RootNode.GetDiagnostics().Should().ContainSingle().Which;

            diagnostic.GetMessage().Should().Be("Invalid JSON: 'c' is an invalid start of a value.");

            var linePositionSpan = diagnostic.Location.GetLineSpan();

            linePositionSpan.StartLinePosition.Line.Should().Be(2);
            linePositionSpan.EndLinePosition.Line.Should().Be(2);

            diagnostic.Location.SourceSpan.Start
                      .Should()
                      .Be(span.Start);

            diagnostic.Location.SourceSpan.End
                      .Should()
                      .Be(span.End);
        }
    }

    [TestMethod]
    public void A_parameter_nodes_associated_parameter_can_be_looked_up()
    {
        var tree = Parse("#!set --value 123 --name x ");

        var parameterNode = tree.RootNode.DescendantNodesAndTokens().OfType<DirectiveParameterNode>().Last();

        parameterNode.TryGetParameter(out var parameter).Should().BeTrue();

        parameter.Name.Should().Be("--name");
        parameter.Required.Should().BeTrue();
    }
}
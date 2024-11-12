// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class JsonConversion
    {
        private readonly ITestOutputHelper _output;

        public JsonConversion(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Directive_nodes_not_containing_JSON_can_be_converted_to_JSON()
        {
            var tree = Parse("""
                #!connect mssql  --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false" --kernel-name sql-adventureworks 
                """);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync();

            result.Diagnostics.Should().BeEmpty();

            result.Value.Should().BeEquivalentJsonTo("""
                {
                  "commandType": "ConnectMsSql",
                  "command": {
                    "createDbcontext": false,
                    "invokedDirective": "#!connect mssql",
                    "kernelName": "sql-adventureworks",
                    "connectionString": "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false",
                    "targetKernelName": ".NET"
                  }
                }
                """);
        }

        [Fact]
        public async Task JSON_values_in_parameter_nodes_are_inserted_directly_into_the_serialized_JSON()
        {
            var tree = Parse("""
                #!set --name myVar --value { "one": 1, "many": [1, 2, 3] } 
                """);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync();

            _output.WriteLine(directiveNode.Diagram());

            result.Diagnostics.Should().BeEmpty();

            result.Value.Should().BeEquivalentJsonTo("""
                {
                  "commandType": "SendValue",
                  "command": {
                    "name": "myVar",
                    "value": { 
                        "one": 1,
                        "many": [1, 2, 3] 
                    },
                    "byref": false,
                    "invokedDirective": "#!set",
                    "targetKernelName": "csharp"
                  }
                }
                """);
        }

        [Theory]
        [InlineData("#!test --opt2 value2 value1")]
        [InlineData("#!test value1 --opt2 value2")]
        public async Task Property_name_is_written_for_implicit_parameter_names(string code)
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
                                KernelCommandType = typeof(TestCommand),
                                Parameters =
                                {
                                    new("--opt1")
                                    {
                                        AllowImplicitName = true
                                    },
                                    new("--opt2"),
                                }
                            }
                        }
                    }
                }
            };

            var tree = Parse(code, config);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync();

            _output.WriteLine(directiveNode.Diagram());

            result.Diagnostics.Should().BeEmpty();

            result.Value.Should().BeEquivalentJsonTo("""
                {
                  "commandType": "TestCommand",
                  "command": {
                    "opt1": "value1",
                    "opt2": "value2",
                    "invokedDirective": "#!test",
                    "targetKernelName": "csharp"
                  }
                }
                """);
        }

        [Fact]
        public async Task Required_parameter_can_use_implicit_parameter_name()
        {
            PolyglotParserConfiguration config = new("csharp")
            {
                KernelInfos =
                {
                    new KernelInfo("csharp")
                    {
                        SupportedDirectives =
                        {
                            new KernelActionDirective("#r")
                            {
                                KernelCommandType = typeof(TestCommand),
                                Parameters =
                                {
                                    new("--string-property")
                                    {
                                        AllowImplicitName = true,
                                        Required = true
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var tree = Parse("""
                             #r "nuget:MyLibrary,1.2.3"
                             """, config);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync();

            _output.WriteLine(directiveNode.Diagram());

            result.Diagnostics.Should().BeEmpty();

            result.Value.Should().BeEquivalentJsonTo("""
                                                     {
                                                       "commandType": "TestCommand",
                                                       "command": {
                                                         "stringProperty": "nuget:MyLibrary,1.2.3",
                                                         "invokedDirective": "#r",
                                                         "targetKernelName": "csharp"
                                                       }
                                                     }
                                                     """);
        }

        [Fact]
        public async Task Properties_are_written_for_parameters_on_parent_directives()
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
                                    new("--opt1"),
                                },
                                Subcommands =
                                {
                                    new("sub")
                                    {
                                        KernelCommandType = typeof(TestCommand),
                                        Parameters =
                                        {
                                            new("--opt2")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var tree = Parse("#!test sub --opt1 value1  --opt2 value2 ", config);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync();

            _output.WriteLine(directiveNode.Diagram());

            result.Diagnostics.Should().BeEmpty();

            result.Value.Should().BeEquivalentJsonTo("""
                {
                  "commandType": "TestCommand",
                  "command": {
                    "opt1": "value1",
                    "opt2": "value2",
                    "invokedDirective": "#!test sub",
                    "targetKernelName": "csharp"
                  }
                }
                """);
        }

        [Fact]
        public async Task When_there_are_error_diagnostics_then_JSON_serialization_fails()
        {
            var tree = Parse("#!set --oops wut");

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            directiveNode.GetDiagnostics().Should().NotBeEmpty();

            var result = await directiveNode.TryGetJsonAsync();

            _output.WriteLine(directiveNode.Diagram());

            result.IsSuccessful.Should().BeFalse();

            result.Value.Should().BeNull();
        }

        [Fact]
        public async Task When_there_are_unbound_expressions_and_no_binding_delegate_is_provided_then_JSON_serialization_fails()
        {
            var markupCode = "#!set --name myVar --value [|@fsharp:|]myVar";

            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = Parse(code);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            directiveNode.GetDiagnostics().Should().BeEmpty();

            var result = await directiveNode.TryGetJsonAsync();

            IEnumerable<Diagnostic> diagnostics = result.Diagnostics;

            var diagnostic = diagnostics
                             .Should()
                             .ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
                             .Which;

            diagnostic.GetMessage().Should().Be($"When bindings are present then a {nameof(DirectiveBindingDelegate)} must be provided.");

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

            result.IsSuccessful.Should().BeFalse();

            result.Value.Should().BeNull();
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class DirectiveOptions
    {
        [Fact]
        public void Words_prefixed_with_hyphens_are_parsed_into_option_name_nodes()
        {
            var tree = Parse("#!directive --option");

            var optionNode = tree.RootNode.DescendantNodesAndTokens()
                                 .Should().ContainSingle<DirectiveOptionNode>()
                                 .Which;

            optionNode.OptionNameNode.Text.Should().Be("--option");
        }

        [Fact]
        public void Words_prefixed_with_hyphens_are_parsed_into_argument_nodes()
        {
            var tree = Parse("#!directive --option argument");

            var argumentNode = tree.RootNode.DescendantNodesAndTokens()
                                   .Should().ContainSingle<DirectiveArgumentNode>()
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

            diagnostic.GetMessage().Should().Be("Unknown option '--invalid-option'");

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
}
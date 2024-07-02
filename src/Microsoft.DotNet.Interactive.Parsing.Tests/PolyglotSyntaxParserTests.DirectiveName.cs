// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class DirectiveName
    {
        [Theory]
        [InlineData(@"
[|#!|]", "fsharp")]
        [InlineData(@"
let x = 123
[|#!abc|]", "fsharp")]
        public void Incomplete_or_unknown_directive_node_is_parsed_as_directive_name_node(
            string markupCode,
            string defaultLanguage)
        {
            MarkupTestFile.GetSpans(markupCode, out var code, out var spans);

            var tree = Parse(code, defaultLanguage);

            using var _ = new AssertionScope();
            
            foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
            {
                var node = tree.RootNode.FindNode(position);

                node.Should().BeAssignableTo<DirectiveNameNode>();
            }
        }

        [Fact]
        public void Shebang_after_the_end_of_a_line_is_not_a_node_delimiter()
        {
            var code = "Console.WriteLine(\"Hello from C#!\");";

            var tree = Parse(code);

            tree.RootNode
                .ChildNodes
                .Should()
                .ContainSingle<LanguageNode>()
                .Which
                .Text
                .Should()
                .Be(code);
        }

        [Fact]
        public void Errors_for_unknown_directives_are_available_as_diagnostics()
        {
            var markupCode = """
            [|#!oops|]
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
            diagnostic.GetMessage().Should().Be("Unknown magic command '#!oops'");

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
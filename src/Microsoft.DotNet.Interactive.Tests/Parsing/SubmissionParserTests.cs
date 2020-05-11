// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    public class SubmissionParserTests
    {
        [Fact]
        public void Parsed_tree_can_recapitulate_original_text()
        {
            var code = @"
#!csharp 
var x = 123;
x
";
            var tree = CreateSubmissionParser().Parse(code);

            tree.ToString().Should().Be(code);
        }

        [Theory]
        [InlineData("#r \"/path/to/a.dll\"\nvar x = 123;", "csharp")]
        [InlineData("#r \"/path/to/a.dll\"\nlet x = 123", "fsharp")]
        public void Pound_r_file_path_is_parsed_as_a_language_node(string code, string language)
        {
            var parser = CreateSubmissionParser(language);

            var tree = parser.Parse(code);

            tree.GetRoot()
                .Should()
                .ContainSingle<LanguageNode>(n => n.Text == "#r \"/path/to/a.dll\"");
        }

        [Fact]
        public void Pound_r_nuget_is_parsed_as_a_directive_node_in_csharp()
        {
            var parser = CreateSubmissionParser("csharp");

            var tree = parser.Parse("var x = 1;\n#r \"nuget:SomePackage\"\nx");

            tree.GetRoot()
                .ChildNodes
                .Should()
                .ContainSingle<DirectiveNode>()
                .Which
                .Text
                .Trim()
                .Should()
                .Be("#r \"nuget:SomePackage\"");

        }

        [Fact]
        public void Pound_r_nuget_is_parsed_as_a_language_node_in_fsharp()
        {
            var parser = CreateSubmissionParser("fsharp");

            var tree = parser.Parse("var x = 1;\n#r \"nuget:SomePackage\"\nx");

            tree.GetRoot()
                .ChildNodes
                .Should()
                .ContainSingle<DirectiveNode>()
                .Which
                .Text
                .Trim()
                .Should()
                .Be("#r \"nuget:SomePackage\"");
        }

        [Fact]
        public void Pound_i_is_a_valid_directive()
        {
            var parser = CreateSubmissionParser("csharp");

            var tree = parser.Parse("var x = 1;\n#i \"nuget:/some/path\"\nx");

            tree.GetRoot()
                .ChildNodes
                .Should()
                .ContainSingle<DirectiveNode>()
                .Which
                .Text
                .Trim()
                .Should()
                .Be("#i \"nuget:/some/path\"");
        }

        [Fact]
        public void Submission_with_terminating_shebang_includes_it_in_language_node()
        {
            var parser = CreateSubmissionParser("csharp");

            var tree = parser.Parse("var x = 1;\n#!");

            tree.GetRoot()
                .Should()
                .ContainSingle<LanguageNode>()
                .Which
                .Text
                .Should()
                .EndWith("#!");
        }

        [Fact]
        public void Directive_parsing_errors_are_available_as_diagnostics()
        {
            var parser = CreateSubmissionParser("csharp");

            var tree = parser.Parse("#!csharp --invalid-option\nvar x = 1;");

            var node = tree.GetRoot()
                           .ChildNodes
                           .Should()
                           .ContainSingle<DirectiveNode>()
                           .Which;
            node
                .GetDiagnostics()
                .Should()
                .ContainSingle(d => d.Severity == DiagnosticSeverity.Error)
                .Which
                .Location
                .SourceSpan
                .Should()
                .BeEquivalentTo(node.Span);
        }

        [Theory]
        [InlineData("var x = 123$$;", typeof(LanguageNode))]
        [InlineData("#!csharp\nvar x = 123$$;", typeof(LanguageNode))]
        [InlineData("#!csharp\nvar x = 123$$;\n", typeof(LanguageNode))]
        [InlineData("#!csh$$arp\nvar x = 123;", typeof(KernelDirectiveNode))]
        [InlineData("#!csharp\n#!time a b$$ c", typeof(DirectiveNode))]
        public void Node_type_is_correctly_identified(
            string markupCode,
            Type expectedNodeType)
        {
            MarkupTestFile.GetPosition(markupCode, out var code, out var position);

            var tree = CreateSubmissionParser().Parse(code);

            var textSpan = tree.GetRoot().FindNode(position.Value);

            textSpan.Should().BeOfType(expectedNodeType);
        }

        [Fact]
        public void Directive_character_ranges_can_be_read()
        {
            var markupCode = @"
[|#!csharp|] 
var x = 123;
x
";

            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = CreateSubmissionParser("csharp").Parse(code);

            var textSpan = tree.GetRoot()
                               .FindNode(span)
                               .ChildTokens
                               .OfType<DirectiveToken>()
                               .Single()
                               .Span;

            textSpan.Should().BeEquivalentTo(span);
        }

        [Theory]
        [InlineData(@"{|csharp:    |}", "csharp")]
        [InlineData(@"{|csharp: var x = abc|}", "csharp")]
        [InlineData(@"
#!fsharp
{|fsharp:let x = |}
#!csharp
{|csharp:var x = 123;|}", "csharp")]
        [InlineData(@"
#!fsharp
{|fsharp:let x = |}
#!csharp
{|csharp:var x = 123;|}", "fsharp")]
        [InlineData(@"
#!fsharp
{|fsharp:  let x = |}
#!csharp
{|csharp:  var x = 123;|}", "fsharp")]
        public void Language_can_be_determined_for_a_given_position(
            string markupCode,
            string defaultLanguage)
        {
            MarkupTestFile.GetNamedSpans(markupCode, out var code, out var spansByName);

            var parser = CreateSubmissionParser(defaultLanguage);

            var tree = parser.Parse(code);

            using var _ = new AssertionScope();

            foreach (var pair in spansByName)
            {
                var expectedLanguage = pair.Key;
                var spans = pair.Value;

                foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
                {
                    var language = tree.GetLanguageAtPosition(position);

                    language
                        .Should()
                        .Be(expectedLanguage, because: $"position {position} should be {expectedLanguage}");
                }
            }
        }

        [Fact]
        public void Shebang_after_the_end_of_a_line_is_not_a_node_delimiter()
        {
            var parser = CreateSubmissionParser();

            var code = "Console.WriteLine(\"Hello from C#!\");";

            var tree = parser.Parse(code);

            tree.GetRoot()
                .Should()
                .ContainSingle<LanguageNode>()
                .Which
                .Text
                .Should()
                .Be(code);
        }

        private static SubmissionParser CreateSubmissionParser(
            string defaultLanguage = "csharp")
        {
            using var compositeKernel = new CompositeKernel();

            compositeKernel.DefaultKernelName = defaultLanguage;

            compositeKernel.Add(
                new CSharpKernel()
                    .UseNugetDirective(),
                new[] { "c#", "C#" });

            compositeKernel.Add(
                new FSharpKernel()
                    .UseNugetDirective(),
                new[] { "f#", "F#" });

            compositeKernel.Add(
                new PowerShellKernel(),
                new[] { "pwsh" });

            compositeKernel.UseDefaultMagicCommands();

            return compositeKernel.SubmissionParser;
        }
    }
}
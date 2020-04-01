// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using FluentAssertions;
using System.Linq;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
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

            var tree = new SubmissionParser("csharp").Parse(code);

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

        private static SubmissionParser CreateSubmissionParser(
            string defaultLanguage = "csharp",
            params Command[] directives)
        {
            using var kernel = new CompositeKernel();
            kernel.DefaultKernelName = defaultLanguage;
            kernel.Add(new FakeKernel("csharp"));
            kernel.Add(new FakeKernel("fsharp"));
            kernel.Add(new FakeKernel("powershell"), new[] { "pwsh" });
            kernel.UseDefaultMagicCommands();

            foreach (var directive in directives)
            {
                kernel.AddDirective(directive);
            }

            return kernel.SubmissionParser;
        }
    }
}
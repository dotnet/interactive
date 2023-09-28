// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Parsing;

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

        var nodes = tree.GetRoot().ChildNodes;

        nodes
            .Should()
            .ContainSingle<LanguageNode>()
            .Which
            .Text
            .Should()
            .Be(code);
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
            .Should()
            .Be("#i \"nuget:/some/path\"");
    }

    [Theory]
    [InlineData(Language.CSharp, Language.CSharp)]
    [InlineData(Language.CSharp, Language.FSharp)]
    [InlineData(Language.FSharp, Language.CSharp)]
    [InlineData(Language.FSharp, Language.FSharp)]
    public void Pound_i_is_dispatched_to_the_correct_kernel(Language defaultKernel, Language targetKernel)
    {
        var parser = CreateSubmissionParser(defaultKernel.LanguageName());

        var command = new SubmitCode("#i \"nuget: SomeLocation\"", targetKernelName: targetKernel.LanguageName());

        var subCommands = parser.SplitSubmission(command);

        subCommands
            .Should()
            .AllSatisfy(c => c.TargetKernelName.Should().Be(targetKernel.LanguageName()));
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

        var diagnostics = node.GetDiagnostics();

        diagnostics
            .Should()
            .ContainSingle(d => d.Severity == CodeAnalysis.DiagnosticSeverity.Error)
            .Which
            .LinePositionSpan.End.Character
            .Should()
            .Be(node.Span.End);

        diagnostics
            .Should()
            .ContainSingle(d => d.Severity == CodeAnalysis.DiagnosticSeverity.Error)
            .Which
            .LinePositionSpan.Start.Character
            .Should()
            .Be(node.Span.Start);
    }

    [Theory]
    [InlineData("var x = 123$$;", typeof(LanguageNode))]
    [InlineData("#!csharp\nvar x = 123$$;", typeof(LanguageNode))]
    [InlineData("#!csharp\nvar x = 123$$;\n", typeof(LanguageNode))]
    [InlineData("#!csh$$arp\nvar x = 123;", typeof(KernelNameDirectiveNode))]
    [InlineData("#!csharp\n#!time a b$$ c", typeof(ActionDirectiveNode))]
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

    [Theory]
    [InlineData(@"
{|none:#!fsharp |}
let x = 
{|fsharp:#!time |}
{|none:#!csharp|}
{|csharp:#!who |}", "fsharp")]
    public void Directive_node_indicates_parent_language(
        string markupCode,
        string defaultLanguage)
    {
        MarkupTestFile.GetNamedSpans(markupCode, out var code, out var spansByName);

        var parser = CreateSubmissionParser(defaultLanguage);

        var tree = parser.Parse(code);

        using var _ = new AssertionScope();

        foreach (var pair in spansByName)
        {
            var expectedParentLanguage = pair.Key;
            var spans = pair.Value;

            foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
            {
                var node = tree.GetRoot().FindNode(position);

                switch (node)
                {
                    case KernelNameDirectiveNode _:
                        expectedParentLanguage.Should().Be("none");
                        break;

                    case ActionDirectiveNode adn:
                        adn.ParentKernelName.Should().Be(expectedParentLanguage);
                        break;

                    default:
                        throw new AssertionFailedException($"Expected a {nameof(DirectiveNode)}  but found: {node}");
                }
            }
        }
    }

    [Theory]
    [InlineData(@"
[|#!|]", "fsharp")]
    [InlineData(@"
let x = 123
[|#!abc|]", "fsharp")]
    public void Incomplete_or_unknown_directive_node_is_recognized_as_a_directive_node(
        string markupCode,
        string defaultLanguage)
    {
        MarkupTestFile.GetSpans(markupCode, out var code, out var spans);

        var parser = CreateSubmissionParser(defaultLanguage);

        var tree = parser.Parse(code);

        using var _ = new AssertionScope();
        {
            foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
            {
                var node = tree.GetRoot().FindNode(position);

                node.Should().BeAssignableTo<DirectiveNode>();
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
            .ChildNodes
            .Should()
            .ContainSingle<LanguageNode>()
            .Which
            .Text
            .Should()
            .Be(code);
    }

    [Fact]
    public void root_node_span_always_expands_with_child_nodes()
    {
        var code = @"#r ""path/to/file""
// language line";
        var parser = CreateSubmissionParser();
        var tree = parser.Parse(code);
        var root = tree.GetRoot();
        var rootSpan = root.Span;

        root.ChildNodes
            .Should()
            .AllSatisfy(child => rootSpan.Contains(child.Span).Should().BeTrue());
    }

    private static SubmissionParser CreateSubmissionParser(string defaultLanguage = "csharp")
    {
        using var compositeKernel = new CompositeKernel
        {
            DefaultKernelName = defaultLanguage
        };

        compositeKernel.Add(
            new CSharpKernel()
                .UseNugetDirective()
                .UseWho(),
            new[] { "c#", "C#" });

        compositeKernel.Add(
            new FSharpKernel()
                .UseNugetDirective(),
            new[] { "f#", "F#" });

        compositeKernel.Add(
            new PowerShellKernel(),
            new[] { "powershell" });

        compositeKernel.UseDefaultMagicCommands();

        return compositeKernel.SubmissionParser;
    }

    [Fact]
    public async Task DiagnosticsProduced_events_always_point_back_to_the_original_command()
    {
        using var kernel = new CSharpKernel();
        var command = new SubmitCode("#!unrecognized");
        var result = await kernel.SendAsync(command);
        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Command.Should().BeSameAs(command);
    }

    [Fact]
    public async Task ParsedDirectives_With_Args_Consume_Newlines()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel().UseValueSharing(),
            new FSharpKernel().UseValueSharing(),
        };
        kernel.DefaultKernelName = "csharp";

        var csharpCode = @"
int x = 123;
int y = 456;";

        await kernel.SubmitCodeAsync(csharpCode);

        var fsharpCode = @"
#!share --from csharp x
#!share --from csharp y
Console.WriteLine($""{x} {y}"");";
        var commands = kernel.SubmissionParser.SplitSubmission(new SubmitCode(fsharpCode));

        commands
            .Should()
            .HaveCount(3)
            .And
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .NotBeEmpty();

    }

    [Theory]
    [InlineData(@"
#r one.dll
#r two.dll", "csharp")]
    [InlineData(@"
#r one.dll
var x = 123; // with some intervening code
#r two.dll", "csharp")]
    [InlineData(@"
#r one.dll
#r two.dll", "fsharp")]
    [InlineData(@"
#r one.dll
let x = 123 // with some intervening code
#r two.dll", "fsharp")]
    public void Multiple_pound_r_directives_are_submitted_together(
        string code,
        string defaultKernel)
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel().UseNugetDirective(),
            new FSharpKernel().UseNugetDirective(),
        };

        kernel.DefaultKernelName = defaultKernel;

        var commands = kernel.SubmissionParser.SplitSubmission(new SubmitCode(code));

        commands
            .Should()
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .ContainAll("#r one.dll", "#r two.dll");
    }

    [Fact]
    public void RequestDiagnostics_can_be_split_into_separate_commands()
    {
        var markupCode = @"

#!time$$

// language-specific code";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var _, out var _);

        var command = new RequestDiagnostics(code);
        var commands = new CSharpKernel().UseDefaultMagicCommands().SubmissionParser.SplitSubmission(command);

        commands
            .Should()
            .ContainSingle<RequestDiagnostics>()
            .Which
            .Code
            .Should()
            .NotContain("#!time");
    }

    [Fact]
    public void Whitespace_only_nodes_do_not_generate_separate_SubmitCode_commands()
    {
        using var kernel = new CompositeKernel
        {
            new FakeKernel("one"),
            new FakeKernel("two")
        };

        kernel.DefaultKernelName = "two";

        var commands = kernel.SubmissionParser.SplitSubmission(
            new SubmitCode(@"

#!one

#!two

"));

        commands.Should().NotContain(c => c is SubmitCode);
    }

    [Fact]
    public void ChooseKernelDirective_parserResults_are_available_in_the_split_commands()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
        };

        var code = @"
#!csharp
var a = 12;
a.Display();";
        var commands = kernel.SubmissionParser.SplitSubmission(new SubmitCode(code));

        commands.Should().ContainSingle<SubmitCode>()
            .Which
            .KernelChooserParseResult
            .CommandResult.Command.Name
            .Should()
            .Be("#!csharp");

    }
}
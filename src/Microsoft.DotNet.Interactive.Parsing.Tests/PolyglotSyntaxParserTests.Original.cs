// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    [Fact]
    public void Pound_r_nuget_is_parsed_as_a_compiler_directive_node_in_csharp()
    {
        var tree = Parse("var x = 1;\n#r \"nuget:SomePackage\"\nx", "csharp");

        var node = tree.RootNode
                       .ChildNodes
                       .Should()
                       .ContainSingle<DirectiveNode>()
                       .Which;
        node.Text
            .Should()
            .Be("#r \"nuget:SomePackage\"");

        node.Kind.Should().Be(DirectiveNodeKind.CompilerDirective);
    }

    [Fact]
    public void Pound_r_nuget_is_parsed_as_a_compiler_directive_node_in_fsharp()
    {
        var tree = Parse("var x = 1;\n#r \"nuget:SomePackage\"\nx", "fsharp");

        var node = tree.RootNode
                       .ChildNodes
                       .Should()
                       .ContainSingle<DirectiveNode>()
                       .Which;
        node.Text
            .Should()
            .Be("#r \"nuget:SomePackage\"");

        node.Kind.Should().Be(DirectiveNodeKind.CompilerDirective);
    }

    [Fact]
    public void Pound_i_is_a_valid_directive()
    {
        var tree = Parse("var x = 1;\n#i \"nuget:/some/path\"\nx");

        var node = tree.RootNode
                       .ChildNodes
                       .Should()
                       .ContainSingle<DirectiveNode>()
                       .Which;
        node.Text
            .Should()
            .Be("#i \"nuget:/some/path\"");

        node.Kind.Should().Be(DirectiveNodeKind.CompilerDirective);
    }

    [Theory]
    [InlineData("var x = 123$$;", typeof(LanguageNode))]
    [InlineData("#!csharp\nvar x = 123$$;", typeof(LanguageNode))]
    [InlineData("#!csharp\nvar x = 123$$;\n", typeof(LanguageNode))]
    [InlineData("#!csh$$arp\nvar x = 123;", typeof(DirectiveNameNode))]
    [InlineData("#!csharp\n#!time a b$$ c", typeof(DirectiveArgumentNode))]
    public void Node_type_is_correctly_identified(
        string markupCode,
        Type expectedNodeType)
    {
        MarkupTestFile.GetPosition(markupCode, out var code, out var position);

        var tree = Parse(code);

        var node = tree.RootNode.FindNode(position.Value);

        node.Should().BeOfType(expectedNodeType);
    }

    [Theory]
    [InlineData("#!csh$$arp\nvar x = 123;", nameof(DirectiveNodeKind.KernelSelector))]
    [InlineData("#!csharp\n#!time a b$$ c", nameof(DirectiveNodeKind.Action))]
    [InlineData("""#r $$"nuget:PocketLogger"  """, nameof(DirectiveNodeKind.CompilerDirective))]
    [InlineData("""#r $$"/path/to/a.dll"  """, nameof(DirectiveNodeKind.CompilerDirective))]
    [InlineData("""#i $$"nuget:https://api.nuget.org/v3/index.json" """, nameof(DirectiveNodeKind.CompilerDirective))]
    [InlineData("""#i $$"/path/to/some-folder"  """, nameof(DirectiveNodeKind.CompilerDirective))]
    public void DirectiveNode_kind_is_correctly_identified(
        string markupCode,
        string kind)
    {
        MarkupTestFile.GetPosition(markupCode, out var code, out var position);

        var tree = Parse(code);

        tree.RootNode
            .FindNode(position.Value)
            .AncestorsAndSelf()
            .Should()
            .ContainSingle<DirectiveNode>()
            .Which
            .Kind
            .ToString()
            .Should()
            .Be(kind);
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

        var tree = Parse(code);

        tree.RootNode
            .ChildNodes
            .Should()
            .ContainSingle<DirectiveNode>()
            .Which
            .Span
            .Should()
            .BeEquivalentTo(span);
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
    public void Kernel_name_can_be_determined_for_a_given_position(
        string markupCode,
        string defaultLanguage)
    {
        MarkupTestFile.GetNamedSpans(markupCode, out var code, out var spansByName);

        var tree = Parse(code, defaultLanguage);

        using var _ = new AssertionScope();

        foreach (var pair in spansByName)
        {
            var expectedLanguage = pair.Key;
            var spans = pair.Value;

            foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
            {
                var language = tree.GetKernelNameAtPosition(position);

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
    public void Directive_node_indicates_kernel_name(
        string markupCode,
        string defaultLanguage)
    {
        MarkupTestFile.GetNamedSpans(markupCode, out var code, out var spansByName);

        var tree = Parse(code, defaultLanguage);

        using var _ = new AssertionScope();

        foreach (var pair in spansByName)
        {
            var expectedParentLanguage = pair.Key;
            var spans = pair.Value;

            foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
            {
                var node = tree.RootNode.FindNode(position);

                switch (node.Parent)
                {
                    case DirectiveNode { Kind: DirectiveNodeKind.KernelSelector }:
                        expectedParentLanguage.Should().Be("none");
                        break;

                    case DirectiveNode { Kind: DirectiveNodeKind.Action } adn:
                        adn.TargetKernelName.Should().Be(expectedParentLanguage);
                        break;

                    default:
                        throw new AssertionFailedException($"Expected a {nameof(DirectiveNode)}  but found: {node}");
                }
            }
        }
    }


    [Fact]
    public void root_node_span_always_expands_with_child_nodes()
    {
        var code = """
            #r "path/to/file"
            // language line
            """;
        var tree = Parse(code);
        var root = tree.RootNode;
        var rootSpan = root.Span;

        root.ChildNodes
            .Should()
            .AllSatisfy(child => rootSpan.Contains(child.Span).Should().BeTrue());
    }
}
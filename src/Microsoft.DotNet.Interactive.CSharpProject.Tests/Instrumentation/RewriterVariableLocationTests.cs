// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Microsoft.DotNet.Interactive.CSharpProject.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation;

public class RewriterVariableLocationTests
{
    private readonly string rewrittenProgram;
    public RewriterVariableLocationTests()
    {
        MarkupTestFile.GetSpans(Sources.withNonAssignedLocals, out var code, out ImmutableArray<TextSpan> _);
        var document = Sources.GetDocument(code, true);
        var syntaxTree = document.GetSyntaxTreeAsync().Result;
        var instrumentedNodes = syntaxTree.GetRoot()
            .DescendantNodes()
            .Where(n => n is StatementSyntax);

        var symbols = document.GetSemanticModelAsync().Result.LookupSymbols(250)
            .Where(symbol => symbol.Kind == SymbolKind.Local);

        var a = symbols.First(symbol => symbol.Name == "a");
        var aLocations = new[]
        {
            new VariableLocation(a, 12, 12, 12, 13),
            new VariableLocation(a, 9, 9, 16, 21)
        };

        var s = symbols.First(symbol => symbol.Name == "s");
        var sLocations = new[]
        {
            new VariableLocation(s, 8, 8, 19, 20),
            new VariableLocation(s, 11, 11, 12, 13)
        };
        var locationMap = new VariableLocationMap();

        locationMap.AddLocations(a, aLocations);
        locationMap.AddLocations(s, sLocations);

        var rewriter = new InstrumentationSyntaxRewriter(
            instrumentedNodes,
            locationMap,
            new AugmentationMap()
        );

        var rewrittenProgramWithWhitespace = rewriter.ApplyToTree(syntaxTree).ToString();
        rewrittenProgram = TestUtils.RemoveWhitespace(rewrittenProgramWithWhitespace);
    }

    [Fact]
    public void Rewritten_Program_Should_Have_Balanced_Brackets()
    {
        Assert.Equal(
            rewrittenProgram.Count(x => x == '('),
            rewrittenProgram.Count(x => x == ')')
        );
    }
    [Fact]
    public void Rewritten_Program_Should_Have_Balanced_Square_Brackets()
    {
        Assert.Equal(
            rewrittenProgram.Count(x => x == '['),
            rewrittenProgram.Count(x => x == ']')
        );
    }
    [Fact]
    public void Rewritten_Program_Should_Have_Balanced_Squiggly_Brackets()
    {
        Assert.Equal(
            rewrittenProgram.Count(x => x == '{'),
            rewrittenProgram.Count(x => x == '}')
        );
    }

    [Fact]
    public void Rewritten_Program_Should_Have_Variable_A()
    {
        Assert.Contains(@"\""name\"":\""a\""", rewrittenProgram);
    }

    [Fact]
    public void Rewritten_Program_Should_Have_Variable_S()
    {
        Assert.Contains(@"\""name\"":\""s\""", rewrittenProgram);
    }

    [Fact]
    public void Rewritten_Program_Should_Have_Location_Of_A_First()
    {
        var expected = TestUtils.RemoveWhitespace(@"
    \""startLine\"": 12,
    \""startColumn\"": 12,
    \""endLine\"": 12,
    \""endColumn\"": 13
    "
        );
        Assert.Contains(expected, rewrittenProgram);
    }

    [Fact]
    public void Rewritten_Program_Should_Have_Location_Of_A_Second()
    {
        var expected = TestUtils.RemoveWhitespace(@"
    \""startLine\"": 9,
    \""startColumn\"": 16,
    \""endLine\"": 9,
    \""endColumn\"": 21
    "
        );
        Assert.Contains(expected, rewrittenProgram);
    }

    [Fact]
    public void Rewritten_Program_Should_Have_DeclaredAt_Of_A()
    {
        var expected = TestUtils.RemoveWhitespace(@"
    \""declaredAt\"": {
        \""start\"": 141,
        \""end\"": 142    
    }"
        );
        Assert.Contains(expected, rewrittenProgram);
    }
}
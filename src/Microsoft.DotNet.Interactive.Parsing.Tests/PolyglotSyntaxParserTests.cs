// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    private readonly AssertionScope _assertionScope;

    public PolyglotSyntaxParserTests(ITestOutputHelper output)
    {
        _output = output;
        _assertionScope = new AssertionScope();
    }

    public void Dispose()
        => _assertionScope.Dispose();

    private static PolyglotSyntaxTree Parse(string code, string defaultLanguage = "csharp")
    {
        var syntaxTree = PolyglotSyntaxParser.Parse(code, defaultLanguage);

        syntaxTree.RootNode.FullText.Should().Be(code);

        return syntaxTree;
    }

    private static DiagnosticInfo CreateDiagnosticInfo(string message) =>
        new(id: "DNI0000", message, DiagnosticSeverity.Error);
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests : IDisposable
{
    private readonly AssertionScope _assertionScope;

    public PolyglotSyntaxParserTests()
    {
        _assertionScope = new AssertionScope();
    }

    public void Dispose() => _assertionScope?.Dispose();

    private static PolyglotSyntaxTree Parse(string code, string defaultLanguage = "csharp")
    {
        var syntaxTree = PolyglotSyntaxParser.Parse(code, PolyglotParserConfigurationTests.GetDefaultConfiguration(defaultLanguage));

        syntaxTree.RootNode.FullText.Should().Be(code);

        return syntaxTree;
    }

    private static PolyglotSyntaxTree Parse(string code, PolyglotParserConfiguration configuration)
    {
        var syntaxTree = PolyglotSyntaxParser.Parse(code, configuration);

        syntaxTree.RootNode.FullText.Should().Be(code);

        return syntaxTree;
    }
}
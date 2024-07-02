// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Xunit.Abstractions;
using DiagnosticInfo  = Microsoft.DotNet.Interactive.Parsing.DiagnosticInfo;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    private readonly AssertionScope _assertionScope;

    public HttpParserTests(ITestOutputHelper output)
    {
        _output = output;
        _assertionScope = new AssertionScope();
    }

    public void Dispose()
        => _assertionScope.Dispose();

    private static HttpRequestParseResult Parse(string code)
    {
        var result = HttpRequestParser.Parse(code);

        result.SyntaxTree.RootNode.FullText.Should().Be(code);

        return result;
    }

    private static DiagnosticInfo CreateDiagnosticInfo(string message) => 
        new(id: "HTTP0000", message, DiagnosticSeverity.Error);
}
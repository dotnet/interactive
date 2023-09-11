// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests : IDisposable
{
    private readonly AssertionScope _assertionScope;

    public ParserTests(ITestOutputHelper output)
    {
        _output = output;
        _assertionScope = new AssertionScope();
    }

    public void Dispose()
    {
        _assertionScope.Dispose();
    }

    private static HttpRequestParseResult Parse(string code)
    {
        var result = HttpRequestParser.Parse(code);

        result.SyntaxTree.RootNode.FullText.Should().Be(code);

        return result;
    }
}
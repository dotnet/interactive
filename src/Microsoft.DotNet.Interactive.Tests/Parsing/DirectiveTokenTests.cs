// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Parsing;

public class DirectiveTokenTests
{
    [Theory]
    [InlineData("#!foo arg1 arg2")]
    [InlineData("#!foo")]
    [InlineData("#!foo\n")]
    [InlineData("#!foo\r\n")]
    public void Text_returns_directive_token_text(string code)
    {
        using var kernel = new CSharpKernel();

        var submissionParser = new SubmissionParser(kernel);

        var tree = submissionParser.Parse(code);

        tree.RootNode
            .DescendantNodesAndTokensAndSelf()
            .Should()
            .ContainSingle<DirectiveToken>()
            .Which
            .Text
            .Should()
            .Be("#!foo");
    }
}
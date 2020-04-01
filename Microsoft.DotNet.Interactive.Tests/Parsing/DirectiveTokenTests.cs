// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    public class DirectiveTokenTests
    {
        [Theory]
        [InlineData("#!foo arg1 arg2")]
        [InlineData("#!foo")]
        [InlineData("#!foo\n")]
        [InlineData("#!foo\r\n")]
        public void Text_returns_directive_token_text(string code)
        {
            var submissionParser = new SubmissionParser("csharp");

            var tree = submissionParser.Parse(code);

            tree.GetRoot()
                .DescendantNodesAndTokensAndSelf()
                .Should()
                .ContainSingle<DirectiveToken>()
                .Which
                .Text
                .Should()
                .Be("#!foo");
        }
    }
}
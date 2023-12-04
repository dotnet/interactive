// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    public class RequestSeparator
    {

        [Fact]
        public void request_separator_at_start_of_request_is_valid()
        {
            var code = """
                ###
                GET https://example.com
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.ChildNodes
                  .Should().ContainSingle<HttpRequestNode>()
                  .Which.UrlNode.Text.Should().Be("https://example.com");
        }

    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Request
    {
        [Fact]
        public void multiple_request_are_parsed_correctly()
        {
            var result = Parse(
                """
                  GET https://example.com

                  ###

                  GET https://example1.com

                  ###

                  GET https://example2.com
                  """);

            var requestNodes = result.SyntaxTree.RootNode
                                     .ChildNodes.OfType<HttpRequestNode>();

            requestNodes.Select(r => r.Text).Should()
                        .BeEquivalentSequenceTo(new[]
                        {
                            "GET https://example.com",
                            "GET https://example1.com", "GET https://example2.com"
                        });
        }
    }
}
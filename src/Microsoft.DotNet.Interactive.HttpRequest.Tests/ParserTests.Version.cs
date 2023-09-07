// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Version
    {
        [Fact]
        public void http_version_is_parsed_correctly()
        {
            var result = Parse("GET https://example.com HTTP/1.1");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .VersionNode.Text.Should().Be("HTTP/1.1");
        }

        [Fact]
        public void http_version_containing_whitespace_produces_a_diagnostic()
        {
            var version = """HTTP 1.1""";

            var code = $"""
                https://example.com {version}
                Accept: application/json
                """;

            var result = Parse(code);

            var versionNode = result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>()
                                        .Which.ChildNodes.Should().ContainSingle<HttpVersionNode>()
                                        .Which;

            versionNode.Text.Should().Be(version);
            versionNode.GetDiagnostics().Should().ContainSingle()
                       .Which.Message.Should().Be("Invalid HTTP version");
        }
    }
}
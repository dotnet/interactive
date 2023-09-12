// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                       .Which.GetMessage().Should().Be("Invalid HTTP version.");
        }

        [Fact]
        public void extra_whitespace_around_HTTP_version_does_not_produce_diagnostics()
        {
            var code = """
                                
                 	 https://example.com 	 HTTP/1.1  
                Accept: */*
                Accept-Encoding: gzip, deflate, br
                Accept-Language: en-US,en;q=0.9
                Content-Length:  7060
                Cookie: expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;
                Origin: https://www.bing.com
                Referer: https://www.bing.com/


                <book id="bk101">
                   <author>Gambardella, Matthew</author>
                   <title>XML Developer's Guide</title>
                   <genre>Computer</genre>  
                   <price>44.95</price>
                   <publish_date>2000-10-01</publish_date>
                   <description>An in-depth look at creating applications
                   with XML.</description>
                </book>

                
                """;

            var result = Parse(code);

            var httpVersionNode = result.SyntaxTree.RootNode.DescendantNodesAndTokens().Should().ContainSingle<HttpVersionNode>()
                                        .Which;

            httpVersionNode.Text.Should().Be("HTTP/1.1");

            httpVersionNode.GetDiagnostics().Should().BeEmpty();
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests
{
    public class Combinatorial
    {
        private readonly ITestOutputHelper _output;

        public Combinatorial(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GenerateRequests))]
        public void Valid_syntax_produces_expected_parse_tree_and_no_diagnostics(ISyntaxSpec syntaxSpec, int index)
        {
            var code = syntaxSpec.ToString();

            var parseResult = HttpRequestParser.Parse(code);

            _output.WriteLine($"""
                === Generation #{index} ===

                {code}
                """);

            parseResult.GetDiagnostics().Should().BeEmpty();

            syntaxSpec.Validate(parseResult.SyntaxTree.RootNode.ChildNodes.Single());
        }

        public static IEnumerable<object[]> GenerateRequests()
        {
            var i = 0;

            foreach (var method in Methods())
            foreach (var url in Urls())
            foreach (var version in Versions())
            foreach (var headerSection in HeaderSections())
            foreach (var bodySection in BodySections())
            {
                ++i;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(method, url, version, headerSection, bodySection),
                    i
                };
            }
        }

        private static IEnumerable<HttpMethodNodeSyntaxSpec> Methods()
        {
            yield return new("");
            yield return new("GET");
            yield return new("POST");
            yield return new("PUT");
        }

        private static IEnumerable<HttpUrlNodeSyntaxSpec> Urls()
        {
            yield return new("https://example.com");

            yield return new("https://example.com?key={{value}}", node =>
            {
                node.ChildNodes.Should().ContainSingle<HttpEmbeddedExpressionNode>()
                    .Which.ExpressionNode.Text.Should().Be("value");
            });
        }

        private static IEnumerable<HttpVersionNodeSyntaxSpec> Versions()
        {
            yield return new("");
            yield return new("HTTP/1.0");
            yield return new("HTTP/1.1");
        }

        private static IEnumerable<HttpBodyNodeSyntaxSpec> BodySections()
        {
            yield return null;

            yield return new("""
                {
                  "object": {
                    "key": "value"
                  },
                  "array": [1, 2, 3]
                }
                """);

            yield return new("""
                <book id="bk101">
                   <author>Gambardella, Matthew</author>
                   <title>XML Developer's Guide</title>
                   <genre>Computer</genre>  
                   <price>44.95</price>
                   <publish_date>2000-10-01</publish_date>
                   <description>An in-depth look at creating applications
                   with XML.</description>
                </book>
                """);

            yield return new("""
                { 
                    "number": {{numberValue}},
                    "string": 
                        {{stringValue}} 
                }
                """, node =>
            {
                node.ChildNodes.OfType<HttpEmbeddedExpressionNode>()
                    .Select(n => n.ExpressionNode.Text)
                    .Should().BeEquivalentTo("numberValue", "stringValue");
            });
        }

        private static IEnumerable<HttpHeadersNodeSyntaxSpec> HeaderSections()
        {
            yield return null;

            yield return new("""
                Accept: */*
                Accept-Encoding: gzip, deflate, br
                Accept-Language: en-US,en;q=0.9
                Content-Length: 7060
                Cookie: expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;
                Origin: https://www.bing.com
                Referer: https://www.bing.com/
                """);
        }
    }
}
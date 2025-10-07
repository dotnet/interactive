// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    public class Combinatorial
    {
        private readonly ITestOutputHelper _output;

        public Combinatorial(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GenerateValidRequests))]
        public void Valid_syntax_produces_expected_parse_tree_and_no_diagnostics(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();
            
            var parseResult = Parse(code);

            _output.WriteLine($"""
                === Generation #{generation} ===

                {code}
                """);
            
            parseResult.GetDiagnostics().Should().BeEmpty();

            syntaxSpec.Validate(parseResult.SyntaxTree.RootNode.ChildNodes.Single());
        }

        [Theory]
        [MemberData(nameof(GenerateValidRequestsWithExtraTrivia))]
        public void Valid_syntax_with_extra_trivia_produces_expected_parse_tree_and_no_diagnostics(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            var parseResult = Parse(code);

            _output.WriteLine($"""
                === Generation #{generation} ===

                {code}
                """);

            parseResult.GetDiagnostics().Should().BeEmpty();

            syntaxSpec.Validate(parseResult.SyntaxTree.RootNode.ChildNodes.Single());
        }

        [Theory]
        [MemberData(nameof(GenerateInvalidRequests))]
        public void Invalid_syntax_produces_diagnostics(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            var parseResult = Parse(code);

            _output.WriteLine($"""
                === Generation #{generation} ===

                {code}
                """);

            parseResult.GetDiagnostics().Should().NotBeEmpty();

            syntaxSpec.Validate(parseResult.SyntaxTree.RootNode.ChildNodes.Single());
        }

        [Theory]
        [MemberData(nameof(GenerateValidRequestsWithExtraTrivia))]
        public void Code_that_a_user_has_not_finished_typing_round_trips_correctly_and_does_not_throw(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            for (var truncateAfter = 0; truncateAfter < code.Length; truncateAfter++)
            {
                var truncatedCode = code[..truncateAfter];

                _output.WriteLine($"""
                === Generation #{generation} truncated after {truncateAfter} characters ===

                {truncatedCode}
                """);

                Parse(truncatedCode);
            }
        }

        public static IEnumerable<object[]> GenerateValidRequests()
        {
            var generationNumber = 0;

            foreach(var namedRequest in ValidNamedRequests())
            foreach (var variables in ValidVariableDeclarations())
            foreach (var method in ValidMethods())
            foreach (var url in ValidUrls())
            foreach (var version in ValidVersions())
            foreach (var headerSection in ValidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection),
                    generationNumber
                };
            }
        }

        public static IEnumerable<object[]> GenerateValidRequestsWithExtraTrivia()
        {
            var generationNumber = 0;

            foreach(var variables in ValidVariableDeclarations())
            foreach (var namedRequest in ValidNamedRequests())
            foreach (var method in ValidMethods())
            foreach (var url in ValidUrls())
            foreach (var version in ValidVersions())
            foreach (var headerSection in ValidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection)
                    {
                        Randomizer = new Random(1)
                    },
                    generationNumber
                };
            }
        }

        public static IEnumerable<object[]> GenerateInvalidRequests()
        {
            var generationNumber = 0;

            foreach (var namedRequest in ValidNamedRequests())
            foreach (var variables in ValidVariableDeclarations())
            foreach (var method in InvalidMethods())
            foreach (var url in ValidUrls())
            foreach (var version in ValidVersions())
            foreach (var headerSection in ValidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection),
                    generationNumber
                };
            }

            foreach (var namedRequest in ValidNamedRequests())
            foreach (var variables in ValidVariableDeclarations())
            foreach (var method in ValidMethods())
            foreach (var url in InvalidUrls())
            foreach (var version in ValidVersions())
            foreach (var headerSection in ValidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection),
                    generationNumber
                };
            }

            foreach(var namedRequest in ValidNamedRequests())
            foreach (var variables in ValidVariableDeclarations())
            foreach (var method in ValidMethods())
            foreach (var url in ValidUrls())
            foreach (var version in InvalidVersions())
            foreach (var headerSection in ValidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection),
                    generationNumber
                };
            }
            
            foreach(var namedRequest in ValidNamedRequests())
            foreach (var variables in ValidVariableDeclarations())
            foreach (var method in ValidMethods())
            foreach (var url in ValidUrls())
            foreach (var version in ValidVersions())
            foreach (var headerSection in InvalidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection),
                    generationNumber
                };
            }

           
            foreach (var namedRequest in InvalidNamedRequests())
            foreach (var variables in ValidVariableDeclarations())
            foreach (var method in ValidMethods())
            foreach (var url in ValidUrls())
            foreach (var version in ValidVersions())
            foreach (var headerSection in ValidHeaderSections())
            foreach (var bodySection in ValidBodySections())
            {
                ++generationNumber;
                yield return new object[]
                {
                    new HttpRequestNodeSyntaxSpec(namedRequest, variables, method, url, version, headerSection, bodySection),
                    generationNumber
                };
            }
        }
    }

        private static IEnumerable<HttpMethodNodeSyntaxSpec> ValidMethods()
        {
            yield return new("");
            yield return new("GET");
            yield return new("POST");
            yield return new("PUT");
        }

        private static IEnumerable<HttpCommentNodeSyntaxSpec> ValidNamedRequests()
        {
            yield return new("", true);
            yield return new("// @name example \r\n", true);
            yield return new("# @name example \r\n", true);
        }

        private static IEnumerable<HttpCommentNodeSyntaxSpec> InvalidNamedRequests()
        {
            yield return new("// @name \r\n", true);
            yield return new("# @nameExample \r\n", true);
            yield return new("// @name tes! \r\n", true);
        }

        private static IEnumerable<HttpMethodNodeSyntaxSpec> InvalidMethods()
        {
            yield return new("OOPS");
        }

        private static IEnumerable<HttpUrlNodeSyntaxSpec> ValidUrls()
        {
            yield return new("https://example.com");

            yield return new("https://example.com?key={{value}}", node =>
            {
                node.ChildNodes.Should().ContainSingle<HttpEmbeddedExpressionNode>()
                    .Which.ExpressionNode.Text.Should().Be("value");
            });
        }

        private static IEnumerable<HttpUrlNodeSyntaxSpec> InvalidUrls()
        {
            // Misspelled
            yield return new("hptps://example.com");
        }

        private static IEnumerable<HttpVersionNodeSyntaxSpec> ValidVersions()
        {
            yield return new("");
            yield return new("HTTP/1.1");
        }

        private static IEnumerable<HttpVersionNodeSyntaxSpec> InvalidVersions()
        {
            // Misspellled
            yield return new("HTPT");

            // The space is invalid
            yield return new("HTTP 1.1");
        }

        private static IEnumerable<HttpHeadersNodeSyntaxSpec> ValidHeaderSections()
        {
            yield return null;

            yield return new("""
                Accept: */*
                Accept-Encoding: gzip, deflate, br
                Accept-Language: en-US,en;q=0.9
                Content-Length:  7060
                Cookie: expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;
                Origin: https://www.bing.com
                Referer: https://www.bing.com/
                """);

            yield return new("""
                Authorization: Basic {{token}}
                Cookie: {{cookie}}
                """, node =>
            {
                node.DescendantNodesAndTokens().OfType<HttpEmbeddedExpressionNode>()
                    .Select(n => n.ExpressionNode.Text)
                    .Should().BeEquivalentTo("token", "cookie");
            });
        }

        private static IEnumerable<HttpHeadersNodeSyntaxSpec> InvalidHeaderSections()
        {
            // The space in the header name is invalid
            yield return new("""
                Accept: */*
                Accept Encoding: gzip, deflate, br
                """);
        }

        private static IEnumerable<HttpBodyNodeSyntaxSpec> ValidBodySections()
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

        private static IEnumerable<HttpVariableDeclarationAndAssignmentNodeSyntaxSpec> ValidVariableDeclarations()
        {
            yield return null;

            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@host=localhost");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@host=https://example.com");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@api_key=secret123");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@base_url=https://{{host}}/api", node =>
            {
                node.ValueNode.DescendantNodesAndTokens().OfType<HttpEmbeddedExpressionNode>()
                    .Should().ContainSingle()
                    .Which.ExpressionNode.Text.Should().Be("host");
            });
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@escaped=\\{\\{text\\}\\}", node =>
            {
                node.ValueNode.Text.Should().Be("{{text}}");
            });
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@with_spaces=one two three");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@quoted=\"hello world\"");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@single_quoted='hello world'");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@user.name=john_doe");
            
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@dynamic={{$guid}}", node =>
            {
                node.ValueNode.DescendantNodesAndTokens().OfType<HttpEmbeddedExpressionNode>()
                    .Should().ContainSingle()
                    .Which.ExpressionNode.Text.Should().Be("$guid");
            });
        }

        private static IEnumerable<HttpVariableDeclarationAndAssignmentNodeSyntaxSpec> InvalidVariableDeclarations()
        {
            // Missing variable name
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@=value");
            
            // Variable name starting with number
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@123invalid=value");
            
            // Invalid character in variable name (hyphen)
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@my-var=value");
            
            // Space in variable name
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@var name=value");
            
            // Missing equals sign
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@host value");
            
            // Special characters in variable name
            yield return new HttpVariableDeclarationAndAssignmentNodeSyntaxSpec("@host!name=value");
        }
    }


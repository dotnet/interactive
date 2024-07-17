// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
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
                            "GET https://example1.com",
                            "GET https://example2.com"
                        });
        }

        [Fact]
        public void request_node_containing_only_url_and_no_variable_expressions_returns_HttpRequestMessage_with_GET_method()
        {
            var result = Parse(
                """
        https://example.com
        """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult =
                requestNode.TryGetHttpRequestMessage(
                    node => node.CreateBindingFailure(CreateDiagnosticInfo("oops")));

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://example.com/");
            bindingResult.Value.Method.Should().Be(HttpMethod.Get);
        }

        [Fact]
        public void request_node_with_multiple_variables_declared_prior_parsed_correctly()
        {
            var result = Parse(
                """
                @searchTerm=some-search-term
                @hostname=httpbin.org
                # variable using another variable
                @host=https://{{hostname}}
                # variable using "dynamic variables"
                @createdAt = {{$datetime iso8601}}

                https://httpbin.org/get
                """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            var variableNodes = result.SyntaxTree.RootNode.ChildNodes.OfType<HttpVariableDeclarationAndAssignmentNode>();
            variableNodes.Count().Should().Be(4);

            variableNodes.Select(n => n.DeclarationNode.Text).Should().BeEquivalentTo(new[] { "@searchTerm", "@hostname", "@host", "@createdAt" });
            variableNodes.Select(n => n.ValueNode.Text).Should().BeEquivalentTo(new[] { "some-search-term", "httpbin.org", "https://{{hostname}}", "{{$datetime iso8601}}" });

            variableNodes.Last().DescendantNodesAndTokens().OfType<HttpExpressionNode>().Count().Should().Be(1);
        }

        [Fact]
        public void request_node_with_embedded_expression_after_embedded_expression()
        {
            var result = Parse(
                """
                @host=https://httpbin.org
                @anything=anything
                @anyhost={{host}}/{{anything}}

                https://httpbin.org/get
                """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            var variableNodes = result.SyntaxTree.RootNode.ChildNodes.OfType<HttpVariableDeclarationAndAssignmentNode>();
            variableNodes.Count().Should().Be(3);

            variableNodes.Last().DescendantNodesAndTokens().OfType<HttpExpressionNode>().Count().Should().Be(2);

            requestNode.UrlNode.Text.Should().Be("https://httpbin.org/get");
        }

        [Fact]
        public void request_with_forward_slash_at_beginning_of_nodes_works()
        {
            var result = Parse(
                """
                @host=https://httpbin.org
                @anything=/anything
                @anyhost={{host}}{{anything}}

                {{anyhost}}
                """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult = requestNode.TryGetHttpRequestMessage(node =>
            {
                return node.CreateBindingFailure(CreateDiagnosticInfo(""));
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://httpbin.org/anything");
        }

        [Fact]
        public void single_request_with_comment_and_request_separator_parsed_correctly()
        {
            var result = Parse(
                """
                #
                @host=https://httpbin.org

                Post {{host}}
                ###
                """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult = requestNode.TryGetHttpRequestMessage(node =>
            {
                return node.CreateBindingFailure(CreateDiagnosticInfo(""));
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://httpbin.org/");
        }

        [Fact]
        public void request_node_containing_method_and_url_and_no_variable_expressions_returns_HttpRequestMessage_with_specified_method()
        {
            var result = Parse(
                """
        POST https://example.com
        """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult =
                requestNode.TryGetHttpRequestMessage(
                    node => node.CreateBindingFailure(CreateDiagnosticInfo("oops")));

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://example.com/");
            bindingResult.Value.Method.Should().Be(HttpMethod.Post);
        }

        [Fact]
        public void request_node_binds_variable_expressions_in_url()
        {
            var result = Parse(
                """
        https://{{host}}/api/{{version}}comments/1
        """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult = requestNode.TryGetHttpRequestMessage(node =>
            {
                return node.Text switch
                {
                    "host" => node.CreateBindingSuccess("example.com"),
                    "version" => node.CreateBindingSuccess("123-")
                };
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://example.com/api/123-comments/1");
        }

        [Fact]
        public void error_is_reported_for_undefined_variable()
        {
            var result = Parse(
                """
            GET https://example.com/api/{{version}}comments/1
            """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var message = "Variable 'version' was not defined.";

            var bindingResult =
                requestNode.TryGetHttpRequestMessage(
                    node => node.CreateBindingFailure(CreateDiagnosticInfo(message)));

            bindingResult.IsSuccessful.Should().BeFalse();
            bindingResult.Diagnostics.Should().ContainSingle().Which.GetMessage().Should().Be(message);
        }

        [Fact]
        public void binding_for_variable_using_another_variable_is_correct()
        {
            var result = Parse(
                """             
                @hostname = httpbin.org
                @host = https://{{hostname}}                     

                POST {{host}}/anything HTTP/1.1
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult = requestNode.TryGetHttpRequestMessage(node =>
            {
                return node.CreateBindingFailure(CreateDiagnosticInfo(""));
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://httpbin.org/anything");
        }

        [Fact]
        public void binding_for_variable_with_period_is_correct()
        {
            var result = Parse(
                """             
                @host.name = httpbin.org
                @host.full = https://{{host.name}}                     

                POST {{host.full}}/anything HTTP/1.1
                content-type: application/json

                {
                    "name": "sample1",
                }

                
                """
                );

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult = requestNode.TryGetHttpRequestMessage(node =>
            {
                return node.CreateBindingFailure(CreateDiagnosticInfo(""));
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://httpbin.org/anything");
        }

        [Fact]
        public void binding_for_variable_in_header_is_correct()
        {
            var result = Parse(
                """             
                @hostname = httpbin.org
                @host = https://{{hostname}}      
                @contentType = application/json

                POST {{host}}/anything HTTP/1.1
                content-type: {{contentType}}

                {
                    "name": "sample1",
                }

                
                """
                );

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                                    .Should().ContainSingle<HttpRequestNode>().Which;

            var bindingResult = requestNode.TryGetHttpRequestMessage(node =>
            {
                return node.CreateBindingFailure(CreateDiagnosticInfo(""));
            });

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://httpbin.org/anything");
            bindingResult.Value.Headers.ToString().Should().Be("");
        }
    }
}
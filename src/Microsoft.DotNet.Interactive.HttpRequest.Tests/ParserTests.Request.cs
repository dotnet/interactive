// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
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

            var bindingResult = requestNode.TryGetHttpRequestMessage(node => node.CreateBindingFailure("oops"));

            bindingResult.IsSuccessful.Should().BeTrue();
            bindingResult.Value.RequestUri.ToString().Should().Be("https://example.com/");
            bindingResult.Value.Method.Should().Be(HttpMethod.Get);
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

            var bindingResult = requestNode.TryGetHttpRequestMessage(node => node.CreateBindingFailure("oops"));

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

            HttpBindingDelegate bind = node => node.CreateBindingFailure(message);

            var bindingResult = requestNode.TryGetHttpRequestMessage(bind);
            bindingResult.IsSuccessful.Should().BeFalse();
            bindingResult.Diagnostics.Should().ContainSingle().Which.Message.Should().Be(message);
        }
    }
}
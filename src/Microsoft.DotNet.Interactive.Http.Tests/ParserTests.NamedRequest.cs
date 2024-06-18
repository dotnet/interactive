using System;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using static Microsoft.DotNet.Interactive.Http.Tests.HttpParserTests;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{

    public class NamedRequest
    {
        [Fact]
        public void named_request_is_parsed_correctly()
        {
            var code = """
                # @name login 

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Should().HaveCount(1);
        }

        [Fact]
        public void named_request_can_reference_content_type_header()
        {
            var code = """
                # @name login 

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [Fact]
        public void named_request_with_two_names_produces_an_error()
        {
            var code = """
                # @name login two

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Single();

            namedRequest.GetDiagnostics().Should().HaveCount(1);   
        }

        [Fact]
        public void named_request_with_special_characters_produces_an_error()
        {
            var code = """
                # @name tes!

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Single();

            namedRequest.GetDiagnostics().Should().HaveCount(1);
        }

        [Theory]
        [InlineData("login")]
        [InlineData("   login   ")]
        [InlineData("  login")]
        [InlineData("login   ")]
        [InlineData("login \r\n")]
        public void named_request_with_various_spaces_are_parsed_correctly(string name)
        {
            var code = $$$"""
                # @name {{{name}}}

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [Fact]
        public void named_request_with_slash_for_comment_start()
        {
            var code = """
                // @name login
                POST {{baseUrl}}/api/login HTTP/1.1 
                
                Content-Type: application/x-www-form-urlencoded 
                
                
                
                name=foo&password=bar 
                
                
                
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [Theory]
        [InlineData("@ name")]

        public void miscellaneous_spaces_around_name_parsed_correctly(string nameToken)
        {
            var code = $$$"""
                # {{{nameToken}}} login

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNamedRequestNode>().Single();

            namedRequest.NameNode.Text.Should().Be("@name");
        }
    }
}

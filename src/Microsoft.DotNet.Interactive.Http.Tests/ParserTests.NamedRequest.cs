using System;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using static Microsoft.DotNet.Interactive.Http.Tests.HttpParserTests;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    [TestClass]
    public class NamedRequest
    {
        [TestMethod]
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

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Should().HaveCount(1);
        }

        [TestMethod]
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

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [TestMethod]
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

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Single();

            namedRequest.GetDiagnostics().Should().HaveCount(1);   
        }

        [TestMethod]
        [DataRow("tes!")]
        [DataRow("tes#")]
        [DataRow("tes!t")]
        [DataRow("+est")]
        [DataRow("e-st")]
        [DataRow("te$t")]
        [DataRow("test%")]
        [DataRow("test^")]
        [DataRow("test&")]
        [DataRow("test*")]
        public void named_request_with_special_characters_produces_an_error(string name)
        {
            var code = $$$"""
                # @name {{{name}}}

                POST {{baseUrl}}/api/login HTTP/1.1 

                Content-Type: application/x-www-form-urlencoded 



                name=foo&password=bar 



                ###
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Should().ContainSingle().Which.GetDiagnostics().Should().ContainSingle().Which.GetMessage().Should().Be("""The supplied name does not follow the correct pattern. The name should only contain alphanumerical characters.""");

            
        }

        [TestMethod]
        [DataRow("login")]
        [DataRow("   login   ")]
        [DataRow("  login")]
        [DataRow("login   ")]
        [DataRow("login \r\n")]
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

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [TestMethod]
        public void named_request_comments_can_use_double_slash_prefix()
        {
            var code = """
                // @name login
                POST {{baseUrl}}/api/login HTTP/1.1 
                
                Content-Type: application/x-www-form-urlencoded 
                
                
                
                name=foo&password=bar 
                
                
                
                """;

            var result = Parse(code);

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [TestMethod]
        public void comments_before_named_request_are_parsed_correctly()
        {
            var code = """
                // test comment
                // @name login
                POST {{baseUrl}}/api/login HTTP/1.1 
                
                Content-Type: application/x-www-form-urlencoded 
                
                
                
                name=foo&password=bar 
                
                
                
                """;

            var result = Parse(code);

            result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpCommentNode>().Should().HaveCount(2);
            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }

        [TestMethod]
        [DataRow(" @name   ")]
        [DataRow("@name     ")]
        [DataRow("                @name   ")]
        [DataRow("              @name")]

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

            var namedRequest = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().Single();

            namedRequest.ValueNode.Text.Should().Be("login");
        }
    }
}

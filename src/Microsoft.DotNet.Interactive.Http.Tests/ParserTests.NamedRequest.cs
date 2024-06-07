using System;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

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
    }
}

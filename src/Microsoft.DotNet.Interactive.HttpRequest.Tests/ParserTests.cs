// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public class ParserTests : IDisposable
{

    public ParserTests()
    {
        assertionScope = new AssertionScope();
    }

    public void Dispose()
    {
        assertionScope.Dispose();
    }

    private readonly AssertionScope assertionScope;
    private static HttpRequestParseResult Parse(string code)
    {
        var result = HttpRequestParser.Parse(code);
        if (result.SyntaxTree is not null && result.SyntaxTree.RootNode is not null)
        {
            result.SyntaxTree.RootNode.TextWithTrivia.Should().Be(code);
        }
        return result;
    }

    public class Trivia
    {
        [Fact]
        public void it_can_parse_an_empty_string()
        {
            var result = Parse("");
            result.SyntaxTree.Should().BeNull();

            // TODO: Test error reporting.
        }

        [Fact]
        public void it_can_parse_a_string_with_only_whitespace()
        {
            var result = Parse(" \t ");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.ChildTokens.First().TextWithTrivia.Should().Be(" \t ");

            // TODO: Test error reporting.
        }

        [Fact]
        public void it_can_parse_a_string_with_only_newlines()
        {
            var result = Parse("\r\n\n\r\n");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.TextWithTrivia.Should().Be("\r\n\n\r\n");

            // TODO: Test error reporting.
            // Regardless of number of newlines, there should be consistent nodes
            // set of whitespace, newline, and punctuation
            //each token kind
            //combination of characters should be checked by the lexer
        }
    }


    public class Lexer
    {
        [Fact]

        public void multiple_whitespaces_are_treated_as_a_single_token()
        {
            var result = Parse("  \t  ");

            result.SyntaxTree.RootNode
            .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
            .MethodNode.ChildTokens.First().Should().BeOfType<HttpSyntaxToken>();

            result.SyntaxTree.RootNode
            .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
            .MethodNode.ChildTokens.Single().TextWithTrivia.Should().Be("  \t  ");
        }

        [Fact]
        public void multiple_newlines_are_parsed_into_different_tokens()
        {
            var result = Parse("\n\v\r\n\n");

            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                .MethodNode.ChildTokens.Select(t => new { t.TextWithTrivia, t.Kind }).Should().BeEquivalentSequenceTo(
                new { TextWithTrivia = "\n", Kind = HttpTokenKind.NewLine },
                new { TextWithTrivia = "\v", Kind = HttpTokenKind.NewLine },
                new { TextWithTrivia = "\r\n", Kind = HttpTokenKind.NewLine },
                new { TextWithTrivia = "\n", Kind = HttpTokenKind.NewLine });
        }

        [Fact]
        public void multiple_punctuations_are_parsed_into_different_tokens()
        {
            var result = Parse(".!?.:/");
            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                .UrlNode.ChildTokens.Select(t => new { t.TextWithTrivia, t.Kind }).Should().BeEquivalentSequenceTo(
                new { TextWithTrivia = ".", Kind = HttpTokenKind.Punctuation },
                new { TextWithTrivia = "!", Kind = HttpTokenKind.Punctuation },
                new { TextWithTrivia = "?", Kind = HttpTokenKind.Punctuation },
                new { TextWithTrivia = ".", Kind = HttpTokenKind.Punctuation },
                new { TextWithTrivia = ":", Kind = HttpTokenKind.Punctuation },
                new { TextWithTrivia = "/", Kind = HttpTokenKind.Punctuation });

        }
    }

    public class Method
    {
        [Fact]
        public void whitespace_is_legal_at_the_beginning_of_a_request()
        {
            var result = Parse("  GET https://example.com");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.ChildTokens.First().Kind
                  .Should().Be(HttpTokenKind.Whitespace);
        }

        [Fact]
        public void newline_is_legal_at_the_beginning_of_a_request()
        {
            var result = Parse(
                """
        
                GET https://example.com
                """);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.ChildTokens.First().Kind.Should().Be(HttpTokenKind.NewLine);
        }


        [Fact]
        public void whitespace_is_legal_at_the_end_of_a_request()
        {
            var result = Parse("GET https://example.com  ");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .UrlNode.ChildTokens.Last().Kind.Should().Be(HttpTokenKind.Whitespace);
        }

        [Fact]
        public void newline_is_legal_at_the_end_of_a_request()
        {
            var result = Parse(
                """
                GET https://example.com

                """);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .UrlNode.ChildTokens.Last().Kind.Should().Be(HttpTokenKind.NewLine);
        }

        [Theory]
        [InlineData("GET https://example.com", "GET")]
        [InlineData("POST https://example.com", "POST")]
        [InlineData("OPTIONS https://example.com", "OPTIONS")]
        [InlineData("TRACE https://example.com", "TRACE")]
        public void common_verbs_are_parsed_correctly(string line, string method)
        {
            var result = Parse(line);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Text.Should().Be(method);
        }

        [Theory]
        [InlineData("https://example.com?hat&ost=foo")]
        [InlineData("https://example.com?q=3081#blah-2%203")]
        public void common_url_structures_are_parsed_correctly(string url)
        {
            var result = Parse($"GET {url}");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .UrlNode.Text.Should().Be(url);
        }

        [Theory]
        [InlineData(@"GET https://example.com", "GET")]
        [InlineData(@"Get https://example.com", "Get")]
        [InlineData(@"OPTIONS https://example.com", "OPTIONS")]
        [InlineData(@"options https://example.com", "options")]
        public void it_can_parse_verbs_regardless_of_their_casing(string line, string method)
        {
            var result = Parse(line);

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode.Text.Should().Be(method);
        }

        [Fact]
        public void http_version_is_parsed_correctly()
        {
            var result = Parse("GET https://example.com HTTP/1.1");

            result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .VersionNode.Text.Should().Be("HTTP/1.1");
        }
        [Fact]
        public void diagnostic_object_is_reported_for_unrecognized_verb()
        {
            var result = Parse("OOOOPS https://example.com");

            result.GetDiagnostics()
                .Should().ContainSingle().Which.Message.Should().Be("Unrecognized HTTP verb OOOOPS");
        }

        [Fact]
        public void request_node_without_method_node_created_correctly()
        {
            var result = Parse("https://example.com");

            result.SyntaxTree.RootNode.ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                .MethodNode.Should().BeNull();
        }

        /*[Fact]
        public void url_node_can_give_url()
        {
            var result = Parse(
                """
        GET https://{{host}}/api/{{version}}comments/1
        """);
            
            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                .Should().ContainSingle<HttpRequestNode>().Which;

            var urlNode = requestNode.UrlNode;
            urlNode.TryGetUri(x => x.Text switch
            {
                "host" => "example.com", 
                "version" => "123-"
            }).ToString().Should().Be("https://example.com/api/123-comments/1");            
        }

        [Fact]
        public void error_is_reported_for_incorrect_uri()
        {
            var result = Parse(
                """
            GET https://{{host}}/api/{{version}}comments/1
            """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                .Should().ContainSingle<HttpRequestNode>().Which;

            var urlNode = requestNode.UrlNode;
            urlNode.TryGetUri(x => x.Text switch
            {
                "host" => "example.com"
            }).Should().BeFalse();

        }*/
    }

    public class Headers
    {
        [Fact]
        public void header_with_body_is_parsed_correctly()
        {
            var result = Parse(
                """
        POST https://example.com/comments HTTP/1.1
        Content-Type: application/xml
        Authorization: token xxx

        <request>
            <name>sample</name>
            <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
        </request>
        """);       

            var requestNode = result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.HeadersNode.HeaderNodes.Count.Should().Be(2);
            requestNode.BodyNode.Text.Should().Be(
                """
        <request>
            <name>sample</name>
            <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
        </request>
        """);
        }

        [Fact]
        public void header_separator_is_present()
        {
            var result = Parse(
                """
        POST https://example.com/comments HTTP/1.1
        Content-Type: application                                                                                                            
        """);

            var requestNode = result.SyntaxTree.RootNode
                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.HeadersNode.HeaderNodes.Single().SeparatorNode.Text.Should().Be(":");
        }

        [Fact]
        public void headers_are_parsed_correctly()
        {
            var result = Parse(
                """
                GET https://example.com HTTP/1.1
                Accept: */*
                Accept-Encoding : gzip, deflate, br
                Accept-Language : en-US,en;q=0.9
                ContentLength:7060
                Cookie: expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;
                """);

            var headersNode = result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .ChildNodes.Should().ContainSingle<HttpHeadersNode>().Which;

            var headerNodes = headersNode.HeaderNodes.ToArray();
            headerNodes.Should().HaveCount(5);

            headerNodes[0].NameNode.Text.Should().Be("Accept");
            headerNodes[0].ValueNode.Text.Should().Be("*/*");

            headerNodes[1].NameNode.Text.Should().Be("Accept-Encoding");
            headerNodes[1].ValueNode.Text.Should().Be("gzip, deflate, br");

            headerNodes[2].NameNode.Text.Should().Be("Accept-Language");
            headerNodes[2].ValueNode.Text.Should().Be("en-US,en;q=0.9");

            headerNodes[3].NameNode.Text.Should().Be("ContentLength");
            headerNodes[3].ValueNode.Text.Should().Be("7060");

            headerNodes[4].NameNode.Text.Should().Be("Cookie");
            headerNodes[4].ValueNode.Text.Should().Be("expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;");
        }
    }

    public class Body
    {
        [Fact]
        public void body_separator_is_present()
        {
            var result = Parse(
            """
            POST https://example.com/comments HTTP/1.1
            Content-Type: application/xml
            Authorization: token xxx

            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);

            var requestNode = result.SyntaxTree.RootNode
                    .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.BodySeparatorNode.ChildTokens.First().Kind.Should().Be(HttpTokenKind.NewLine);
        }

        [Fact]
        public void when_headers_are_not_present_there_should_be_no_header_nodes()
        {
            var result = Parse(
                """
                POST https://example.com/comments HTTP/1.1

                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);


            var requestNode = result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.HeadersNode.HeaderNodes.Count.Should().Be(0);
        }

        [Fact]
        public void body_is_parsed_correctly_when_headers_are_not_present()
        {
            var result = Parse(
                """
                POST https://example.com/comments HTTP/1.1

                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);


            var requestNode = result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.BodyNode.Text.Should().Be(
                """
                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);
        }

        [Fact]
        public void multiple_new_lines_before_body_are_parsed_correctly()
        {
            var result = Parse(
                """
                POST https://example.com/comments HTTP/1.1
                Content-Type: application/xml
                Authorization: token xxx




                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);


            var requestNode = result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.BodyNode.Text.Should().Be(
                """
                <request>
                    <name>sample</name>
                    <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
                </request>
                """);
        }
    }

    public class Comment
    {
        [Fact]
        public void comments_are_parsed_correctly()
        {

            var result = Parse(
                """
                # This is a comment
                GET https://example.com HTTP/1.1"
                """);


            var methodNode = result.SyntaxTree.RootNode
                  .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
                  .MethodNode;

            methodNode.ChildNodes.Should().ContainSingle<HttpCommentNode>().Which.Text.Should().Be(
              "# This is a comment");


        }
    }

    public class Tree
    {
        [Fact]
        public void multiple_request_are_parsed_correctly()
        {
            var result = Parse(
                """
                  GET https://example.com HTTP/1.1

                  ###

                  GET https://example.com HTTP/1.1

                  ###

                  GET https://example.com HTTP/1.1
                  """);


            var requestNodes = result.SyntaxTree.RootNode
                  .ChildNodes;

            requestNodes.Select(r => r.Text).Should()
                .BeEquivalentSequenceTo(new[] { "GET https://example.com HTTP/1.1",
                "GET https://example.com HTTP/1.1", "GET https://example.com HTTP/1.1"});
        }
    }

    public class Variables
    {
        [Fact]
        public void expression_is_parsed_correctly()
        {
            var result = Parse(
                """
                GET https://{{host}}/api/{{version}}comments/1 HTTP/1.1
                Authorization: {{token}}
                """);

            var requestNode = result.SyntaxTree.RootNode.ChildNodes
                .Should().ContainSingle<HttpRequestNode>().Which;

            requestNode.UrlNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Select(e => e.Text)
                .Should().BeEquivalentSequenceTo(new[] { "host", "version" });

            requestNode.HeadersNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>()
                .Should().ContainSingle().Which.Text.Should().Be("token");
        }

        //TODO Test all parsers for expression
    }
}
// TODO: Test string with variable declarations but no requests

/*
[TestClass]
public class TokenTest
{

    [Fact]
    public async Task RequestWithEmbeddedDynamicVariableWithSpacesAsync()
    {
        string text = $"\r{Environment.NewLine}GET https://example.com?id={{{{$randomInt 1 2}}}}&id2=value&id3={{{{$randomInt 2 3}}}} http/1.1";

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Request request = doc.Requests[0];

        Assert.IsNotNull(request.Version);
        Assert.AreEqual("http/1.1", request.Version.Text);

        Assert.AreEqual("https://example.com?id={{$randomInt 1 2}}&id2=value&id3={{$randomInt 2 3}}", request.Url.Text);
    }

    [Fact]
    public async Task MultipleRequestsAsync()
    {
        string text = """
            get http://example.com

            ###

            post http://bing.com
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Assert.AreEqual(2, doc.Requests.Count);
    }

    [Fact]
    public async Task RequestWithHeaderAndBodyAndCommentAsync()
    {
        string text = """
            DELETE https://example.com
            User-Agent: ost
            #ost:hat

            {
                "enabled": true
            }

            ###
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);
        Request first = doc.Requests[0];

        Assert.IsNotNull(first);
        Assert.AreEqual(1, doc.Requests.Count);
        Assert.AreEqual(ItemType.HeaderName, first.Children[2].Type);
        Assert.AreEqual(ItemType.HeaderValue, first.Children[3].Type);
        Assert.AreEqual(ItemType.Comment, first.Children[4].Type);
        Assert.AreEqual(ItemType.EmptyLine, first.Children[5].Type);
        Assert.AreEqual(ItemType.Body, first.Children[6].Type);
        Assert.AreEqual("{\r\n    \"enabled\": true\r\n}\r\n", first.Body);
    }

    [Fact]
    public async Task BodyAfterCommentAsync()
    {
        string text = """
            TraCe https://{{host}}/authors/{{name}}
            Content-Type: at{{contentType}}svin
            #ost
            mads: ost

            {
                "content": "foo bar",
                "created_at": "{{createdAt}}",

                "modified_by": "$test$"
            }


            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);
        Request request = doc.Requests[0];

        Assert.IsNotNull(request.Body);
        Assert.IsTrue(request.Body.Contains("$test$"));
        Assert.IsTrue(request.Body.Trim().EndsWith("}"));
    }

    [Fact]
    public async Task VariableTokenizationAsync()
    {
        string text = $"@name = value";

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);
        ParseItem name = doc.Items[0];
        ParseItem nextItem = doc.Items[1];

        Assert.AreEqual(0, name.Start);
        Assert.AreEqual(5, name.Length);
        Assert.AreEqual(8, nextItem.Start);
        Assert.AreEqual(5, nextItem.Length);
    }

    [Fact]
    public async Task CommentInBetweenHeadersAsync()
    {
        string text = """
            POST https://example.com
            Content-Type:application/json
            #comment
            Accept: gzip
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Assert.AreEqual(8, doc.Items.Count);
    }

    [Theory]
    [InlineData(" foo: bar")]
    [InlineData("foo : bar")]
    [InlineData("foo")]
    public async Task InvalidHeaders_ShouldBeTreatedAsBody_WithError(string contentType)
    {
        string text = $"""
            POST https://example.com
            {contentType}
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Assert.AreEqual(4, doc.Items.Count);
        Assert.AreEqual(ItemType.Body, doc.Items[3].Type);
        Assert.AreEqual(contentType, doc.Items[3].Text);
        Assert.AreEqual(1, doc.Items[3].Errors.Count);
        Assert.AreEqual(string.Format(Strings.NotAValidHttpHeader, contentType), doc.Items[3].Errors[0].Message);
    }

    [Fact]
    public async Task HeaderMissingValue_ShouldBeTreatedAsBody_WithError()
    {
        string text = $"""
            POST https://example.com
            foo:
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Assert.AreEqual(4, doc.Items.Count);
        Assert.AreEqual(ItemType.Body, doc.Items[3].Type);
        Assert.AreEqual("foo:", doc.Items[3].Text);
        Assert.AreEqual(1, doc.Items[3].Errors.Count);
        Assert.AreEqual(string.Format(Strings.HttpHeaderMissingValue, "foo:"), doc.Items[3].Errors[0].Message);
    }

}*/
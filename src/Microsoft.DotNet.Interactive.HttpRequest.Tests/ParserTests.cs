// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public class ParserTests
{
    [Fact]
    public void it_can_parse_an_empty_string()
    {
        var result = HttpRequestParser.Parse("");
        result.SyntaxTree.Should().BeNull();

        // TODO: Test error reporting.
    }

    [Fact]
    public void it_can_parse_a_string_with_only_whitespace()
    {
        var result = HttpRequestParser.Parse(" \t ");

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .MethodNode.ChildTokens.First().TextWithTrivia.Should().Be(" \t ");

        // TODO: Test error reporting.
    }

    [Fact]
    public void it_can_parse_a_string_with_only_newlines()
    {
        var result = HttpRequestParser.Parse("\r\n\n\r\n");

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .MethodNode.TextWithTrivia.Should().Be("\r\n\n\r\n");

        // TODO: Test error reporting.
    }

    [Fact]
    public void whitespace_is_legal_at_the_beginning_of_a_request()
    {
        var result = HttpRequestParser.Parse("  GET https://example.com");

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .MethodNode.ChildTokens.First().TextWithTrivia.Should().Be("  ");
    }

    [Fact]
    public void newline_is_legal_at_the_beginning_of_a_request()
    {
        var result = HttpRequestParser.Parse(
            """
            
            GET https://example.com
            """);

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .MethodNode.ChildTokens.First().TextWithTrivia.Should().Be("\r\n");
    }


    [Fact]
    public void whitespace_is_legal_at_the_end_of_a_request()
    {
        var result = HttpRequestParser.Parse("GET https://example.com  ");

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .UrlNode.ChildTokens.Last().TextWithTrivia.Should().Be("  ");
    }

    [Fact]
    public void newline_is_legal_at_the_end_of_a_request()
    {
        var result = HttpRequestParser.Parse(
            """
            GET https://example.com

            """);

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .UrlNode.ChildTokens.Last().TextWithTrivia.Should().Be("\r\n");
    }

    [Theory]
    [InlineData("GET https://example.com", "GET")]
    [InlineData("POST https://example.com", "POST")]
    [InlineData("OPTIONS https://example.com", "OPTIONS")]
    [InlineData("TRACE https://example.com", "TRACE")]
    public void common_verbs_are_parsed_correctly(string line, string method)
    {
        var result = HttpRequestParser.Parse(line);

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .MethodNode.Text.Should().Be(method);
    }

    [Theory]
    [InlineData("https://example.com?hat&ost=foo")]
    [InlineData("https://example.com?q=3081#blah-2%203")]
    public void common_url_structures_are_parsed_correctly(string url)
    {
        var result = HttpRequestParser.Parse($"GET {url}");

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
        var result = HttpRequestParser.Parse(line);

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .MethodNode.Text.Should().Be(method);
    }

    [Fact]
    public void http_version_is_parsed_correctly()
    {
        var result = HttpRequestParser.Parse("GET https://example.com HTTP/1.1");

        result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .VersionNode.Text.Should().Be("HTTP/1.1");
    }

    [Fact]
    public void headers_are_parsed_correctly()
    {
        var result = HttpRequestParser.Parse(
            """
            GET https://example.com HTTP/1.1
            Accept: */*
            Accept-Encoding : gzip, deflate, br
            Accept-Language : en-US,en;q=0.9
            ContentLength:7060
            Cookie: expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;
            """);

        using var _ = new AssertionScope();

        var headersNode = result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which
              .ChildNodes.Should().ContainSingle<HttpHeadersNode>().Which;

        var headerNodes = headersNode.HeaderNodes.ToArray();
        headerNodes.Should().HaveCount(5);

        headerNodes[0].NameNode.Text.Should().Be("Accept");
        headerNodes[0].SeparatorNode.Text.Should().Be(":");
        headerNodes[0].ValueNode.Text.Should().Be("*/*");

        headerNodes[1].NameNode.Text.Should().Be("Accept-Encoding");
        headerNodes[1].SeparatorNode.Text.Should().Be(":");
        headerNodes[1].ValueNode.Text.Should().Be("gzip, deflate, br");

        headerNodes[2].NameNode.Text.Should().Be("Accept-Language");
        headerNodes[2].SeparatorNode.Text.Should().Be(":");
        headerNodes[2].ValueNode.Text.Should().Be("en-US,en;q=0.9");

        headerNodes[3].NameNode.Text.Should().Be("ContentLength");
        headerNodes[3].SeparatorNode.Text.Should().Be(":");
        headerNodes[3].ValueNode.Text.Should().Be("7060");

        headerNodes[4].NameNode.Text.Should().Be("Cookie");
        headerNodes[4].SeparatorNode.Text.Should().Be(":");
        headerNodes[4].ValueNode.Text.Should().Be("expor=;HSD=Ak_1ZasdqwASDASD;SSID=SASASSDFsdfsdf213123;APISID=WRQWRQWRQWRcc123123;");
    }

    [Fact]
    public void header_with_body_is_parsed_correctly()
    {
        var result = HttpRequestParser.Parse(
            """
            POST https://example.com/comments HTTP/1.1
            Content-Type: application/xml
            Authorization: token xxx

            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);

        using var _ = new AssertionScope();

        var requestNode = result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

        requestNode.HeadersNode.HeaderNodes.Count.Should().Be(2);
        requestNode.BodySeparatorNode.TextWithTrivia.Should().Be("\r\n");
        requestNode.BodyNode.Text.Should().Be(
            """
            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);
    }

    //TODO: Is it an error to include a body with no headers?
    [Fact]
    public void body_without_header_is_parsed_correctly()
    {
        var result = HttpRequestParser.Parse(
            """
            POST https://example.com/comments HTTP/1.1

            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);

        using var _ = new AssertionScope();

        var requestNode = result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

        requestNode.HeadersNode.HeaderNodes.Count.Should().Be(0);
        requestNode.BodySeparatorNode.TextWithTrivia.Should().Be("\r\n");
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
        var result = HttpRequestParser.Parse(
            """
            POST https://example.com/comments HTTP/1.1
            Content-Type: application/xml
            Authorization: token xxx




            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);

        using var _ = new AssertionScope();

        var requestNode = result.SyntaxTree.RootNode
              .ChildNodes.Should().ContainSingle<HttpRequestNode>().Which;

        requestNode.HeadersNode.HeaderNodes.Count.Should().Be(2);
        requestNode.BodySeparatorNode.TextWithTrivia.Should().Be("\r\n\r\n\r\n\r\n");
        requestNode.BodyNode.Text.Should().Be(
            """
            <request>
                <name>sample</name>
                <time>Wed, 21 Oct 2015 18:27:50 GMT</time>
            </request>
            """);
    }
}

// TODO: Test empty string
// TODO: Test string with whitespaces and newline only
// TODO: Test string with variable declarations but no requests

/*
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WebTools.Languages.Rest.VS.Parser;
using Microsoft.WebTools.Languages.Rest.VS.Resources;
using Microsoft.WebTools.Languages.Rest.VS.Test.TestUtils;

[TestClass]
public class TokenTest
{
    [Fact]
    public async Task RequestTextAfterLineBreakAsync()
    {
        string text = $"\r{Environment.NewLine}GET https://example.com";

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Request request = doc.Requests[0];

        Assert.AreEqual("GET", request.Method.Text);
        Assert.AreEqual(3, request.Method.Start);
    }

    [Fact]
    public async Task RequestWithVersionAsync()
    {
        string text = $"\r{Environment.NewLine}GET https://example.com http/1.1";

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Request request = doc.Requests[0];

        Assert.IsNotNull(request.Version);
        Assert.AreEqual("http/1.1", request.Version.Text);
    }

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
    public async Task RequestWithHeaderAndBodyAsync()
    {
        string text = """
            GET https://example.com
            User-Agent: ost

            {"enabled": true}
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);
        Request request = doc.Requests[0];

        Assert.AreEqual(1, doc.Requests.Count);
        Assert.IsNotNull(request.Body);
    }

    [Fact]
    public async Task RequestWithHeaderAndMultilineBodyAsync()
    {
        string text = """
            GET https://example.com
            User-Agent: ost

            {
            "enabled": true
            }
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);
        Request request = doc.Requests[0];

        Assert.IsNotNull(request.Body);
        Assert.AreEqual(21, request.Body.Length);
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task EmptyLinesAsync(string line)
    {
        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(line);
        ParseItem first = doc.Items[0];

        Assert.IsNotNull(first);
        Assert.AreEqual(ItemType.EmptyLine, first.Type);
        Assert.AreEqual(0, first.Start);
        Assert.AreEqual(line, first.Text);
        Assert.AreEqual(line.Length, first.Length);
        Assert.AreEqual(line.Length, first.End);
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

    [Fact]
    public async Task HeaderContainsSpaceAfterColon_ShouldContainBothHeaderAndValue()
    {
        string text = $"""
            POST https://example.com
            foo: bar
            """;

        IRestDocumentSnapshot doc = await TestHelpers.CreateDocumentSnapshotAsync(text);

        Assert.AreEqual(5, doc.Items.Count);
        Assert.AreEqual(ItemType.HeaderName, doc.Items[3].Type);
        Assert.AreEqual(ItemType.HeaderValue, doc.Items[4].Type);
        Assert.AreEqual("bar", doc.Items[4].Text);
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
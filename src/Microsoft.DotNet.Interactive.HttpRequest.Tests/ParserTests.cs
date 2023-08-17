// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public partial class ParserTests : IDisposable
{
    private readonly AssertionScope _assertionScope;

    public ParserTests()
    {
        _assertionScope = new AssertionScope();
    }

    public void Dispose()
    {
        _assertionScope.Dispose();
    }

    private static HttpRequestParseResult Parse(string code)
    {
        var result = HttpRequestParser.Parse(code);
        
        result.SyntaxTree.RootNode.FullText.Should().Be(code);

        return result;
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
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public class ParserTests
{
    [Theory]
    [InlineData("GET https://example.com", "GET")]
    [InlineData("POST https://example.com?hat&ost", "POST")]
    [InlineData("OPTIONS https://example.com", "OPTIONS")]
    [InlineData("TRACE https://example.com?hat&ost", "TRACE")]
    public async Task common_verbs_are_parsed_correctly(string line, string method)
    {
        HttpRequestParseResult result = HttpRequestParser.Parse(line);

        result.SyntaxTree
              .RootNode
              .ChildNodes
              .Should()
              .ContainSingle<HttpRequestNode>()
              .Which.MethodNode.Text.Should().Be(method);
    }

    // [Theory]
    // [InlineData(@"GET https://example.com")]
    // [InlineData(@"get https://example.com")]
    // [InlineData(@"OPTIONS https://example.com")]
    // [InlineData(@"options https://example.com")]
    //     
    // public async Task it_can_parse_verbs_regardless_of_their_casing(string line)
    // {
    //     IRestDocumentSnapshot doc = await CreateDocumentSnapshotAsync(line);
    //     ParseItem request = doc.Items[0];
    //     ParseItem method = doc.Items[1];
    //
    //     throw new NotImplementedException();
    //     Assert.IsNotNull(method);
    //     Assert.AreEqual(ItemType.Request, request.Type);
    //     Assert.AreEqual(ItemType.Method, method.Type);
    //     Assert.AreEqual(0, method.Start);
    //     Assert.IsTrue(line.StartsWith(method.Text));
    // }
    //
    // [Theory]
    // [InlineData(@"Trace https://example.com?hat&ost HTTP/1.1")]
    // public async Task OneLinersAsync(string line)
    // {
    //     IRestDocumentSnapshot doc = await CreateDocumentSnapshotAsync(line);
    //     ParseItem request = doc.Items[0];
    //     ParseItem method = doc.Items[1];
    //
    //     throw new NotImplementedException();
    //     Assert.IsNotNull(method);
    //     Assert.AreEqual(ItemType.Request, request.Type);
    //     Assert.AreEqual(ItemType.Method, method.Type);
    //     Assert.AreEqual(0, method.Start);
    //     Assert.IsTrue(line.StartsWith(method.Text));
    // }
}

// Copyright (c) Microsoft Corporation. All rights reserved.

/*namespace Microsoft.WebTools.Languages.Rest.VS.Test.Parser;

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
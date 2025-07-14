// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Http.Tests.Utility;

internal class HttpRequestNodeSyntaxSpec : SyntaxSpecBase<HttpRequestNode>
{
    public HttpRequestNodeSyntaxSpec(
        HttpCommentNodeSyntaxSpec commentNamedRequest,
        HttpVariableDeclarationAndAssignmentNodeSyntaxSpec variableDeclarationAndAssignment,
        HttpMethodNodeSyntaxSpec method,
        HttpUrlNodeSyntaxSpec url,
        HttpVersionNodeSyntaxSpec version = null,
        HttpHeadersNodeSyntaxSpec headersSection = null,
        HttpBodyNodeSyntaxSpec bodySection = null,
        params Action<HttpRequestNode>[] assertions) : base(assertions)
    {
        CommentNamedRequest = commentNamedRequest;
        Method = method;
        Url = url;

        if (version is not null)
        {   
            Version = version;
        }

        HeadersSection = headersSection;
        BodySection = bodySection;
    }

    public HttpCommentNodeSyntaxSpec CommentNamedRequest { get; }

    public HttpVariableDeclarationAndAssignmentNodeSyntaxSpec VariableDeclarationAndAssignment { get; }

    public HttpMethodNodeSyntaxSpec Method { get; }

    public HttpUrlNodeSyntaxSpec Url { get; }

    public HttpVersionNodeSyntaxSpec Version { get; }

    public HttpHeadersNodeSyntaxSpec HeadersSection { get; }

    public HttpBodyNodeSyntaxSpec BodySection { get; }
    
    public override void Validate(HttpRequestNode requestNode)
    {
        base.Validate(requestNode);

        if (!string.IsNullOrEmpty(CommentNamedRequest?.Text))
        {
            var httpNamedRequestNode = requestNode.DescendantNodesAndTokens().OfType<HttpNamedRequestNode>().SingleOrDefault();

            httpNamedRequestNode.Should().NotBeNull();

            httpNamedRequestNode.Parent.Should().NotBeNull();

            httpNamedRequestNode.Parent.GetType().Should().Be(typeof(HttpCommentNode));

            var commentNode = httpNamedRequestNode.Parent as HttpCommentNode;

            CommentNamedRequest.Validate(syntaxNode: commentNode);
        }

        if (!string.IsNullOrEmpty(Method?.Text))
        {
            var httpMethodNode = requestNode.ChildNodes.OfType<HttpMethodNode>().SingleOrDefault();

            httpMethodNode.Should().NotBeNull();

            Method.Validate(httpMethodNode);
        }
        else
        {
            requestNode.ChildNodes.Should().NotContain(n => n is HttpMethodNode);
        }

        if (!string.IsNullOrEmpty(Url?.Text))
        {
            var urlNode = requestNode.ChildNodes.OfType<HttpUrlNode>().SingleOrDefault();

            urlNode.Should().NotBeNull();

            Url.Validate(urlNode);
        }
        else
        {
            requestNode.ChildNodes.Should().NotContain(n => n is HttpUrlNode);
        }

        if (!string.IsNullOrEmpty(Version?.Text))
        {
            var versionNode = requestNode.ChildNodes.OfType<HttpVersionNode>().SingleOrDefault();

            versionNode.Should().NotBeNull();

            Version.Validate(versionNode);
        }
        else
        {
            requestNode.ChildNodes.Should().NotContain(n => n is HttpVersionNode);
        }

        if (HeadersSection is not null)
        {
            var headersNode = requestNode.ChildNodes.OfType<HttpHeadersNode>().SingleOrDefault();

            headersNode.Should().NotBeNull();

            HeadersSection.Validate(headersNode);
        }
        else
        {
            requestNode.ChildNodes.Should().NotContain(n => n is HttpHeadersNode);
        }

        if (BodySection is not null)
        {
            var bodyNode = requestNode.ChildNodes.OfType<HttpBodyNode>().SingleOrDefault();

            bodyNode.Should().NotBeNull();

            BodySection.Validate(bodyNode);
        }
        else
        {
            requestNode.ChildNodes.Should().NotContain(n => n is HttpBodyNode);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(MaybeNewLines());
        sb.Append(CommentNamedRequest);
        sb.Append(MaybeLineComment());
        sb.Append(MaybeWhitespace());

        sb.Append(Method);
        sb.Append(" ");
        sb.Append(MaybeWhitespace());

        sb.Append(Url);
        sb.Append(" ");

        if (Version is not null)
        {
            sb.Append(MaybeWhitespace());
            sb.Append(Version);
        }

        sb.Append(MaybeWhitespace());
        sb.AppendLine();
        sb.Append(MaybeLineComment()); 

        if (HeadersSection is not null)
        {
            sb.AppendLine(HeadersSection.ToString());
            sb.AppendLine();
        }

        sb.Append(MaybeNewLines());

        if (BodySection is not null)
        {
            sb.AppendLine();
            sb.AppendLine(BodySection.ToString());
            sb.Append(MaybeNewLines());
        }

        return sb.ToString();
    }

    private HttpCommentNodeSyntaxSpec MaybeLineComment()
    {
        var numberOfCommentLines = Randomizer?.NextDouble() switch
        {
            < .4 => 1,
            > .4 and < .7 => 2,
            _ => 0
        };

        var commentText = "";

        for (int i = 0; i < numberOfCommentLines; i++)
        {
            commentText += CommentLine();
        }

        return new(commentText);

        string CommentLine()
        {
            return Randomizer?.NextDouble() switch
            {
                < .25 => "# random line comment followed by a LF\n",
                > .25 and < .5 => "# random line comment followed by a CRLF\r\n",
                > .5 and < .75 => "// random line comment followed by a LF\n",
                > .75  => "// random line comment followed by a CRLF\r\n",
            };
        }
    }
}
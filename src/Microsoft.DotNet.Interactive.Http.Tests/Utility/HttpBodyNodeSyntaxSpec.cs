// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Tests.Utility;

public interface ISyntaxSpec
{
    void Validate(object syntaxNode);
}

internal abstract class SyntaxSpecBase<T> : ISyntaxSpec
    where T : SyntaxNode
{
    private readonly Action<T>[] _assertions;

    protected SyntaxSpecBase(string text, params Action<T>[] assertions)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _assertions = assertions;
    }

    protected SyntaxSpecBase(params Action<T>[] assertions)
    {
        _assertions = assertions;
    }

    public string Text { get; }

    public virtual void Validate(T syntaxNode)
    {
        if (Text is not null)
        {
            syntaxNode.Text.Should().Be(Text, because: $"{syntaxNode.GetType()}.Text");
        }

        if (_assertions is not null)
        {
            foreach (var assertion in _assertions)
            {
                assertion.Invoke(syntaxNode);
            }
        }
    }

    public void Validate(object syntaxNode)
    {
        syntaxNode.Should().BeOfType<T>();
        Validate((T)syntaxNode);
    }

    public override string ToString() => Text;
}

internal class HttpRequestNodeSyntaxSpec : SyntaxSpecBase<HttpRequestNode>
{
    public HttpRequestNodeSyntaxSpec(
        HttpMethodNodeSyntaxSpec method,
        HttpUrlNodeSyntaxSpec url,
        HttpVersionNodeSyntaxSpec version = null,
        HttpHeadersNodeSyntaxSpec headersSection = null,
        HttpBodyNodeSyntaxSpec bodySection = null,
        params Action<HttpRequestNode>[] assertions) : base(assertions)
    {
        Method = method;
        Url = url;

        if (version is not null)
        {
            Version = version;
        }

        HeadersSection = headersSection;
        BodySection = bodySection;
    }

    public HttpMethodNodeSyntaxSpec Method { get; }

    public HttpUrlNodeSyntaxSpec Url { get; }

    public HttpVersionNodeSyntaxSpec Version { get; }

    public HttpHeadersNodeSyntaxSpec HeadersSection { get; }

    public HttpBodyNodeSyntaxSpec BodySection { get; }
    
    public Random ExtraTriviaRandomizer { get; set; }

    public override void Validate(HttpRequestNode requestNode)
    {
        base.Validate(requestNode);

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

    private string MaybeWhitespace() =>
        ExtraTriviaRandomizer?.NextDouble() switch
        {
            < .2  => " ",
            > .2 and < .4 => "  ",
            > .4 and < .6 => "\t",
            > .6 and < .8 => "\t ",
            _ => ""
        };

    private string MaybeNewLines() =>
        ExtraTriviaRandomizer?.NextDouble() switch
        {
            < .2 => "\n",
            > .2 and < .4 => "\r\n",
            _ => ""
        };

    private string MaybeLineComment()
    {
        var numberOfCommentLines = ExtraTriviaRandomizer?.NextDouble() switch
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

        return commentText;

        string CommentLine()
        {
            return ExtraTriviaRandomizer?.NextDouble() switch
            {
                < .25 => "# random line comment followed by a LF\n",
                > .25 and < .5 => "# random line comment followed by a CRLF\r\n",
                > .5 and < .75 => "// random line comment followed by a LF\n",
                > .75  => "// random line comment followed by a CRLF\r\n",
            };
        }
    }
}

internal class HttpMethodNodeSyntaxSpec : SyntaxSpecBase<HttpMethodNode>
{
    public HttpMethodNodeSyntaxSpec(string text, params Action<HttpMethodNode>[] assertions) : base(text, assertions)
    {
    }
}

internal class HttpUrlNodeSyntaxSpec : SyntaxSpecBase<HttpUrlNode>
{
    public HttpUrlNodeSyntaxSpec(string text, params Action<HttpUrlNode>[] assertions) : base(text, assertions)
    {
    }
}

internal class HttpVersionNodeSyntaxSpec : SyntaxSpecBase<HttpVersionNode>
{
    public HttpVersionNodeSyntaxSpec(string text, params Action<HttpVersionNode>[] assertions) : base(text, assertions)
    {
    }
}

internal class HttpBodyNodeSyntaxSpec : SyntaxSpecBase<HttpBodyNode>
{
    public HttpBodyNodeSyntaxSpec(string text, params Action<HttpBodyNode>[] assertions) : base(text, assertions)
    {
    }
}

internal class HttpHeadersNodeSyntaxSpec : SyntaxSpecBase<HttpHeadersNode>
{
    public HttpHeadersNodeSyntaxSpec(string text, params Action<HttpHeadersNode>[] assertions) : base(text, assertions)
    {
    }
}
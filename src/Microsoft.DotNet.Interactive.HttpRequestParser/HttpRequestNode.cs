// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRequestNode : HttpSyntaxNode
{
    internal HttpRequestNode(
        HttpMethodNode methodNode,
        HttpUrlNode urlNode,
        HttpHeadersNode headersNode,
        HttpBodyNode bodyNode,
        string sourceText,
        HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
        MethodNode = methodNode;
        Add(MethodNode);
        UrlNode = urlNode;
        Add(UrlNode);

        if (headersNode is not null)
        {
            HeadersNode = headersNode;
            Add(HeadersNode);
        }

        if (bodyNode is not null)
        {
            BodyNode = bodyNode;
            Add(BodyNode);
        }
    }

    public HttpMethodNode MethodNode { get; }

    public HttpUrlNode UrlNode { get; }

    public HttpHeadersNode HeadersNode { get; }

    public HttpBodyNode BodyNode { get; }
}
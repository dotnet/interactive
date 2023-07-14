﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRequestNode : HttpSyntaxNode
{
    internal HttpRequestNode(
        string sourceText,
        HttpSyntaxTree? syntaxTree,
        HttpMethodNode methodNode,
        HttpUrlNode urlNode,
        HttpVersionNode? versionNode = null,
        HttpHeadersNode? headersNode = null,
        HttpBodyNode? bodyNode = null) : base(sourceText, syntaxTree)
    {
        MethodNode = methodNode;
        Add(MethodNode);

        UrlNode = urlNode;
        Add(UrlNode);

        if (versionNode is not null)
        {
            VersionNode = versionNode;
            Add(VersionNode);
        }

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

    public HttpVersionNode? VersionNode { get; set; }

    public HttpHeadersNode? HeadersNode { get; }

    public HttpBodyNode? BodyNode { get; }
}
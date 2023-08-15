// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRequestNode : HttpSyntaxNode
{
    internal HttpRequestNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree,
        HttpMethodNode? methodNode,
        HttpUrlNode urlNode,
        HttpVersionNode? versionNode = null,
        HttpHeadersNode? headersNode = null,
        HttpBodySeparatorNode? bodySeparatorNode = null,
        HttpBodyNode? bodyNode = null) : base(sourceText, syntaxTree)
    {
        if (methodNode is not null)
        {
            MethodNode = methodNode;
            Add(MethodNode);
        }

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

        if (bodySeparatorNode is not null)
        {
            BodySeparatorNode = bodySeparatorNode;
            Add(bodySeparatorNode);
        }

        if (bodyNode is not null)
        {
            BodyNode = bodyNode;
            Add(BodyNode);
        }
    }

    public HttpMethodNode? MethodNode { get; }

    public HttpUrlNode UrlNode { get; }

    public HttpVersionNode? VersionNode { get; set; }

    public HttpHeadersNode? HeadersNode { get; }

    public HttpBodySeparatorNode? BodySeparatorNode { get; }

    public HttpBodyNode? BodyNode { get; }

    public HttpBindingResult<HttpRequestMessage> TryGetHttpRequestMessage(HttpBindingDelegate bind)
    {
        var request = new HttpRequestMessage();
        var diagnostics = new List<Diagnostic>(base.GetDiagnostics());

        if (MethodNode is { Span.IsEmpty: false })
        {
            request.Method = new HttpMethod(MethodNode.Text);
        }

        var uriBindingResult = UrlNode.TryGetUri(bind);
        if (uriBindingResult.IsSuccessful)
        {
            request.RequestUri = uriBindingResult.Value;
        }
        else
        {
            diagnostics.AddRange(uriBindingResult.Diagnostics);
        }

        var bodyResult = BodyNode?.TryGetBody(bind);
        string body = "";

        if (bodyResult is not null)
        {
            if (bodyResult.IsSuccessful)
            {
                body = bodyResult.Value ?? "";
            }

            diagnostics.AddRange(bodyResult.Diagnostics);
        }

        request.Content = new StringContent(body);

        if (HeadersNode is { HeaderNodes: { } headerNodes })
        {
            foreach (var headerNode in headerNodes)
            {
                var headerName = headerNode.NameNode.Text.ToLowerInvariant();
                var headerValue = headerNode.ValueNode.Text;

                // FIX: (TryGetHttpRequestMessage) better testing

                switch (headerName)
                {
                    case "accept":
                        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(headerValue));
                        break;
                    case "allow" or "content-disposition" or "content-encoding" or "content-language" or "content-length" or "content-location" or "content-md5" or "content-range"
                        or "expires" or "last-modified"
                        or "content-type":
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(headerValue);
                        break;
                    case "user-agent":
                        request.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse(headerValue));
                        break;
                    default:
                        request.Headers.Add(headerNode.NameNode.Text, headerNode.ValueNode.Text);
                        break;
                }
            }
        }

        if (diagnostics.All(d => d.Severity != DiagnosticSeverity.Error))
        {
            return HttpBindingResult<HttpRequestMessage>.Success(request);
        }
        else
        {
            return HttpBindingResult<HttpRequestMessage>.Failure(diagnostics.ToArray());
        }
    }
}
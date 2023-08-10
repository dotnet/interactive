// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
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

        if (MethodNode is { FullSpan.IsEmpty: false })
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

        var headers =
            HeadersNode?.HeaderNodes.Select(h => new KeyValuePair<string, string>(h.NameNode.Text, h.ValueNode.Text)).ToArray()
            ??
            Array.Empty<KeyValuePair<string, string>>();

        foreach (var kvp in headers)
        {
            switch (kvp.Key.ToLowerInvariant())
            {
                case "content-type":
                    if (request.Content is null)
                    {
                        request.Content = new StringContent("");
                    }

                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(kvp.Value);
                    break;
                case "accept":
                    request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(kvp.Value));
                    break;
                case "user-agent":
                    request.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse(kvp.Value));
                    break;
                default:
                    request.Headers.Add(kvp.Key, kvp.Value);
                    break;
            }
        }

        var bodyResult = BodyNode?.TryGetBody(bind);
        string? body = null;

        if (bodyResult is not null)
        {
            if (bodyResult.IsSuccessful)
            {
                body = bodyResult.Value ?? "";
            }

            diagnostics.AddRange(bodyResult.Diagnostics);
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            request.Content = new StringContent(body);
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
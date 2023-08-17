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
    internal HttpRequestNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }
    
    public HttpMethodNode? MethodNode { get; private set; }

    public HttpUrlNode? UrlNode { get; private set; }

    public HttpVersionNode? VersionNode { get; private set; }

    public HttpHeadersNode? HeadersNode { get; private set; }

    public HttpBodySeparatorNode? BodySeparatorNode { get; private set; }

    public HttpBodyNode? BodyNode { get; private set; }

    public void Add(HttpMethodNode node)
    {
        if (MethodNode is not null)
        {
            throw new InvalidOperationException($"{nameof(MethodNode)} was already added.");
        }
        MethodNode = node;
        AddInternal(node);
    }

    public void Add(HttpUrlNode node)
    {
        if (UrlNode is not null)
        {
            throw new InvalidOperationException($"{nameof(UrlNode)} was already added.");
        }
        UrlNode = node;
        AddInternal(node);
    }

    public void Add(HttpVersionNode node)
    {
        if (VersionNode is not null)
        {
            throw new InvalidOperationException($"{nameof(VersionNode)} was already added.");
        }
        VersionNode = node;
        AddInternal(node);
    }

    public void Add(HttpHeadersNode node)
    {
        if (HeadersNode is not null)
        {
            throw new InvalidOperationException($"{nameof(HeadersNode)} was already added.");
        }
        HeadersNode = node;
        AddInternal(node);
    }

    public void Add(HttpBodySeparatorNode node)
    {
        if (BodySeparatorNode is not null)
        {
            throw new InvalidOperationException($"{nameof(BodySeparatorNode)} was already added.");
        }
        BodySeparatorNode = node;
        AddInternal(node);
    }

    public void Add(HttpBodyNode node)
    {
        if (BodyNode is not null)
        {
            throw new InvalidOperationException($"{nameof(BodyNode)} was already added.");
        }
        BodyNode = node;
        base.AddInternal(node);
    }

    public HttpBindingResult<HttpRequestMessage> TryGetHttpRequestMessage(HttpBindingDelegate bind)
    {
        var request = new HttpRequestMessage();
        var diagnostics = new List<Diagnostic>(base.GetDiagnostics());

        if (MethodNode is { Span.IsEmpty: false })
        {
            request.Method = new HttpMethod(MethodNode.Text);
        }

        if (UrlNode?.TryGetUri(bind) is { } uriBindingResult)
        {
            if (uriBindingResult.IsSuccessful)
            {
                request.RequestUri = uriBindingResult.Value;
            }
            else
            {
                diagnostics.AddRange(uriBindingResult.Diagnostics);
            }
        }
        else
        {
            // FIX: (TryGetHttpRequestMessage) add diagnostic
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
                if (headerNode.NameNode is null)
                {
                    // FIX: (TryGetHttpRequestMessage) add diagnostic
                    continue;
                }

                if (headerNode.ValueNode is null)
                {
                    // FIX: (TryGetHttpRequestMessage) add diagnostic
                    continue;
                }

                var headerName = headerNode.NameNode.Text.ToLowerInvariant();
                var headerValue = headerNode.ValueNode.Text;

                // FIX: (TryGetHttpRequestMessage) bind possible expressions in the value node
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
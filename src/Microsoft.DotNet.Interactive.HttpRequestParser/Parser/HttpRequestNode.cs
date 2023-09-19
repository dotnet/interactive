// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

using Diagnostic = CodeAnalysis.Diagnostic;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

internal class HttpRequestNode : HttpSyntaxNode
{
    internal HttpRequestNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpMethodNode? MethodNode { get; private set; }

    public HttpUrlNode? UrlNode { get; private set; }

    public HttpVersionNode? VersionNode { get; private set; }

    public HttpHeadersNode? HeadersNode { get; private set; }

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

    public void Add(HttpBodyNode node)
    {
        if (BodyNode is not null)
        {
            throw new InvalidOperationException($"{nameof(BodyNode)} was already added.");
        }
        BodyNode = node;
        AddInternal(node);
    }

    public void Add(HttpCommentNode node)
    {
        AddInternal(node);
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
                    continue;
                }

                if (headerNode.ValueNode is null)
                {
                    continue;
                }

                var headerName = headerNode.NameNode.Text.ToLowerInvariant();
                var headerValueResult = headerNode.ValueNode.TryGetValue(bind);

                diagnostics.AddRange(headerValueResult.Diagnostics);

                if (headerValueResult.IsSuccessful)
                {
                    var headerValue = headerValueResult.Value!;

                    try
                    {
                        switch (headerName.ToLowerInvariant())
                        {
                            case "content-encoding":
                                request.Content.Headers.Add(headerName, headerValue);
                                break;
                            case "content-language":
                                request.Content.Headers.Add(headerName, headerValue);
                                break;
                            case "content-length":
                                request.Content.Headers.ContentLength = long.Parse(headerValue);
                                break;
                            case "content-type":
                                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(headerValue);
                                break;

                            default:
                                request.Headers.Add(headerName, headerValue);
                                break;
                        }
                    }
                    catch (Exception exception)
                    {
                        var diagnosticInfo = HttpDiagnostics.InvalidHeaderValue(exception.Message);
                        var diagnostic = headerNode.ValueNode.CreateDiagnostic(diagnosticInfo);
                        diagnostics.Add(diagnostic);
                    }
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
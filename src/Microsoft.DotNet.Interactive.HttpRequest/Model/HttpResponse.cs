// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.HttpRequest;

[TypeFormatterSource(
    typeof(HttpResponseFormatterSource),
    PreferredMimeTypes = new[] { HtmlFormatter.MimeType, PlainTextFormatter.MimeType, JsonFormatter.MimeType })]
public sealed class HttpResponse
{
    public int StatusCode { get; }
    public string ReasonPhrase { get; }
    public string Version { get; }
    public Dictionary<string, string[]> Headers { get; }
    public HttpRequest? Request { get; }
    public HttpContent? Content { get; }
    public double? ElapsedMilliseconds { get; internal set; }

    public HttpResponse(
        int statusCode,
        string reasonPhrase,
        string version,
        Dictionary<string, string[]> headers,
        HttpRequest? request = null,
        HttpContent? content = null,
        double? elapsedMilliseconds = null)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Version = version;
        Headers = headers;
        Request = request;
        Content = content;
        ElapsedMilliseconds = elapsedMilliseconds;
    }
}

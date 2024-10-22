// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Http;

[TypeFormatterSource(
    typeof(HttpResponseFormatterSource),
    PreferredMimeTypes = [HtmlFormatter.MimeType])]
public sealed class HttpResponse : PartialHttpResponse
{
    public Dictionary<string, string[]> Headers { get; }
    public HttpRequest? Request { get; }
    public HttpContent? Content { get; }

    public HttpResponse(
        int statusCode,
        string reasonPhrase,
        string version,
        Dictionary<string, string[]> headers,
        HttpRequest? request = null,
        HttpContent? content = null,
        double? elapsedMilliseconds = null) : base(statusCode, reasonPhrase, version, elapsedMilliseconds, content?.ByteLength)
    {
        Headers = headers;
        Request = request;
        Content = content;
        ElapsedMilliseconds = elapsedMilliseconds;
    }
}

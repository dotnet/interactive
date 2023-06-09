// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

#if HTTP_REQUEST_KERNEL
namespace Microsoft.DotNet.Interactive.HttpRequest;
[TypeFormatterSource(typeof(HttpResponseFormatterSource))]
public sealed class HttpResponse
#else
namespace Microsoft.DotNet.Interactive.Formatting.Http;
internal sealed class HttpResponse
#endif
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

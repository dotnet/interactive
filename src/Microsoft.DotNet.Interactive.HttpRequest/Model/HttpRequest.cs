// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;

#if HTTP_REQUEST_KERNEL
namespace Microsoft.DotNet.Interactive.HttpRequest;
public sealed class HttpRequest
#else
namespace Microsoft.DotNet.Interactive.Formatting.Http;
internal sealed class HttpRequest
#endif
{
    public string Method { get; }
    public string Version { get; }
    public Dictionary<string, string[]> Headers { get; }
    public string? Uri { get; }
    public HttpContent? Content { get; }

    public HttpRequest(
        string method,
        string version,
        Dictionary<string, string[]> headers,
        string? uri = null,
        HttpContent? content = null)
    {
        Method = method;
        Version = version;
        Headers = headers;
        Uri = uri;
        Content = content;
    }
}

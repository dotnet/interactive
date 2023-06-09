// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal static class HttpMessageExtensions
{
    internal static async Task<HttpRequest?> ToHttpRequestAsync(
        this HttpRequestMessage? requestMessage,
        CancellationToken cancellationToken = default)
    {
        if (requestMessage is null)
        {
            return null;
        }

        var method = requestMessage.Method.ToString();
        var version = requestMessage.Version.ToString();
        var headers = requestMessage.Headers.ToDictionary();
        var uri = requestMessage.RequestUri?.ToString();

        HttpContent? content = null;
        if (requestMessage.Content is { } requestMessageContent)
        {
#if NETSTANDARD2_0
            var contentRaw = await requestMessageContent.ReadAsStringAsync();
#else
            var contentRaw = await requestMessageContent.ReadAsStringAsync(cancellationToken);
#endif
            var contentByteLength = requestMessageContent.Headers.ContentLength ?? 0;
            var contentHeaders = requestMessageContent.Headers.ToDictionary();
            var contentType = requestMessageContent.Headers.ContentType?.ToString();
            content = new HttpContent(contentRaw, contentByteLength, contentHeaders, contentType);
        }

        return new HttpRequest(method, version, headers, uri, content);
    }

    internal static async Task<HttpResponse?> ToHttpResponseAsync(
        this HttpResponseMessage? responseMessage,
        CancellationToken cancellationToken = default)
    {
        if (responseMessage is null)
        {
            return null;
        }

        var statusCode = (int)responseMessage.StatusCode;
        var reasonPhrase = responseMessage.ReasonPhrase;
        if (string.IsNullOrWhiteSpace(reasonPhrase))
        {
            reasonPhrase = responseMessage.StatusCode.ToString();
        }

        var version = responseMessage.Version.ToString();
        var headers = responseMessage.Headers.ToDictionary();

        HttpRequest? request = null;
        if (responseMessage.RequestMessage is not null)
        {
            request = await responseMessage.RequestMessage.ToHttpRequestAsync(cancellationToken);
        }

        HttpContent? content = null;
        if (responseMessage.Content is { } responseMessageContent)
        {
#if NETSTANDARD2_0
            var contentRaw = await responseMessageContent.ReadAsStringAsync();
#else
            var contentRaw = await responseMessageContent.ReadAsStringAsync(cancellationToken);
#endif
            var contentByteLength = responseMessageContent.Headers.ContentLength ?? 0;
            var contentHeaders = responseMessageContent.Headers.ToDictionary();
            var contentType = responseMessageContent.Headers.ContentType?.ToString();
            content = new HttpContent(contentRaw, contentByteLength, contentHeaders, contentType);
        }

        return new HttpResponse(statusCode, reasonPhrase, version, headers, request, content);
    }

    private static Dictionary<string, string[]> ToDictionary(this HttpHeaders headers)
        => headers.ToDictionary(header => header.Key, header => header.Value.ToArray());
}

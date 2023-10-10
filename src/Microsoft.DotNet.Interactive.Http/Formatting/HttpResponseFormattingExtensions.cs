// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Http;

internal static class HttpResponseFormattingExtensions
{
    private const string ContainerClass = "http-response-message-container";

    private static readonly HtmlString CSS = new($@"
            .{ContainerClass} {{
                display: flex;
                flex-wrap: wrap;
            }}

            .{ContainerClass} > div {{
                margin: .5em;
                padding: 1em;
                border: 1px solid;
            }}

            .{ContainerClass} > div > h2 {{
                margin-top: 0;
            }}

            .{ContainerClass} > div > h3 {{
                margin-bottom: 0;
            }}

            .{ContainerClass} summary {{
                margin: 1em 0;
                font-size: 1.17em;
                font-weight: 700;
            }}

            @keyframes blink {{
                0% {{
                  opacity: .2;
                }}
                20% {{
                  opacity: 1;
                }}
                100% {{
                  opacity: .2;
                }}
            }}

            .ellipsis span {{
                animation-name: blink;
                animation-duration: 1.4s;
                animation-iteration-count: infinite;
                animation-fill-mode: both;
            }}

            .ellipsis span:nth-child(2) {{
                animation-delay: .2s;
            }}

            .ellipsis span:nth-child(3) {{
                animation-delay: .4s;
            }}");

    internal static void FormatAsHtml(this EmptyHttpResponse? response, FormatContext context)
    {
        if (response is null)
        {
            PocketView result = pre("null");
            result.WriteTo(context);
            return;
        }

        PocketView? output = null;

        if (response is HttpResponse fullResponse)
        {
            output = fullResponse.FormatAsHtml();
        }
        else if (response is PartialHttpResponse partialResponse)
        {
            output =
                div[@class: ContainerClass](
                    style[type: "text/css"](CSS),
                    h3[@class: "ellipsis"](
                        $"HTTP/{partialResponse.Version} {partialResponse.StatusCode} {partialResponse.ReasonPhrase} ({partialResponse.ElapsedMilliseconds:0.##} ms)",
                        br,
                        "Loading content ",
                        span("."),
                        span("."),
                        span(".")));
        }
        else if (response is EmptyHttpResponse)
        {
            output =
                div[@class: ContainerClass](
                    style[type: "text/css"](CSS),
                    h3[@class: "ellipsis"](
                        "Awaiting response ",
                        span("."),
                        span("."),
                        span(".")));
        }

        output?.WriteTo(context);
    }

    private static PocketView FormatAsHtml(this HttpResponse response)
    {
        PocketView? output;
        dynamic? requestDiv;
        if (response.Request is { } request)
        {
            var requestUriString = request.Uri?.ToString();
            var requestHyperLink =
                string.IsNullOrWhiteSpace(requestUriString)
                    ? "[Unknown]"
                    : a[href: requestUriString](requestUriString);

            var requestLine =
                h3(
                    $"{request.Method} ", requestHyperLink, $" HTTP/{request.Version}");

            var requestHeaders =
                details(
                    summary("Headers"),
                    HeaderTable(request.Headers, request.Content?.Headers));

            var requestBodyString = request.Content?.Raw ?? string.Empty;
            var requestBodyLength = request.Content?.ByteLength ?? 0;
            var requestContentType = response.Content?.ContentType;
            var requestContentTypePrefix = requestContentType is null ? null : $"{requestContentType}, ";

            var requestBody =
                details(
                    summary($"Body ({requestContentTypePrefix}{requestBodyLength} bytes)"),
                    pre(requestBodyString));

            requestDiv =
                div(
                    h2("Request"),
                    hr(),
                    requestLine,
                    requestHeaders,
                    requestBody);
        }
        else
        {
            requestDiv = div(h2("Request"), hr());
        }

        var responseLine =
            h3(
                $"HTTP/{response.Version} {response.StatusCode} {response.ReasonPhrase} ({response.ElapsedMilliseconds:0.##} ms)");

        var responseHeaders =
            details[open: true](
                summary("Headers"),
                HeaderTable(response.Headers, response.Content?.Headers));

        var responseBodyString = response.Content?.Raw ?? string.Empty;
        var responseBodyLength = response.Content?.ByteLength ?? 0;
        var responseContentType = response.Content?.ContentType;
        var responseContentTypePrefix = responseContentType is null ? null : $"{responseContentType}, ";

        // TODO: Handle raw v/s formatted.
        // TODO: Handle other content types like images, html and xml.
        object responseObjToFormat;
        try
        {
            responseObjToFormat = JsonDocument.Parse(responseBodyString);
        }
        catch (JsonException)
        {
            responseObjToFormat = responseBodyString;
        }

        var responseBody =
            details[open: true](
                summary($"Body ({responseContentTypePrefix}{responseBodyLength} bytes)"),
                responseObjToFormat);

        var responseDiv =
            div(
                h2("Response"),
                hr(),
                responseLine,
                responseHeaders,
                responseBody);

        output =
            div[@class: ContainerClass](
                style[type: "text/css"](CSS),
                requestDiv,
                responseDiv);
        return output;
    }

    internal static void FormatAsPlainText(this EmptyHttpResponse? response, FormatContext context)
    {
        if (response is null)
        {
            context.Writer.WriteLine("null");
            return;
        }

        if (response is HttpResponse fullResponse)
        {
            fullResponse.FormatAsPlainText(context);
        }
        else if (response is PartialHttpResponse partialResponse)
        {
            context.Writer.WriteLine($"Status Code: {partialResponse.StatusCode} {partialResponse.ReasonPhrase}");
            context.Writer.WriteLine($"Elapsed: {partialResponse.ElapsedMilliseconds:0.##} ms");
            context.Writer.WriteLine($"Version: HTTP/{partialResponse.Version}");
            context.Writer.WriteLine();
            context.Writer.WriteLine("Loading content ...");
        }
        else
        {
            context.Writer.WriteLine("Awaiting response ...");
        }
    }

    private static void FormatAsPlainText(this HttpResponse response, FormatContext context)
    {
        if (response.Request is { } request)
        {
            context.Writer.WriteLine($"Request Method: {request.Method}");
            context.Writer.WriteLine($"Request URI: {request.Uri}");
            context.Writer.WriteLine($"Request Version: HTTP/{request.Version}");
            context.Writer.WriteLine();
        }

        context.Writer.WriteLine($"Status Code: {response.StatusCode} {response.ReasonPhrase}");
        context.Writer.WriteLine($"Elapsed: {response.ElapsedMilliseconds:0.##} ms");
        context.Writer.WriteLine($"Version: HTTP/{response.Version}");
        context.Writer.WriteLine($"Content Type: {response.Content?.ContentType}");
        context.Writer.WriteLine($"Content Length: {response.Content?.ByteLength ?? 0} bytes");
        context.Writer.WriteLine();

        var headers = response.Headers;
        var contentHeaders = response.Content?.Headers;
        var allHeaders = contentHeaders is null ? headers : headers.Concat(contentHeaders);

        foreach (var header in allHeaders)
        {
            context.Writer.WriteLine($"{header.Key}: {string.Join("; ", header.Value)}");
        }

        var responseBodyString = response.Content?.Raw ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(responseBodyString))
        {
            // TODO: Handle other content types like images, html and xml.
            switch (response.Content?.ContentType)
            {
                case "application/json":
                case "text/json":
                    var formatted =
                        JsonSerializer.Serialize(
                            JsonDocument.Parse(responseBodyString).RootElement,
                            new JsonSerializerOptions { WriteIndented = true });

                    context.Writer.WriteLine($"Body: {formatted}");
                    break;

                default:
                    context.Writer.WriteLine($"Body: {responseBodyString}");
                    break;
            }
        }
    }

    private static dynamic HeaderTable(Dictionary<string, string[]> headers, Dictionary<string, string[]>? contentHeaders = null)
    {
        var allHeaders = contentHeaders is null ? headers : headers.Concat(contentHeaders);

        var headerTable =
            table(
                thead(
                    tr(
                        th("Name"), th("Value"))),
                tbody(
                    allHeaders.Select(header =>
                        tr(
                            td(header.Key), td(string.Join("; ", header.Value))))));

        return headerTable;
    }
}

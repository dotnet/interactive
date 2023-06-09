// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.HttpRequest;

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
            }}");

    internal static void FormatAsHtml(this HttpResponse? response, FormatContext context)
    {
        if (response is null)
        {
            PocketView result = PocketViewTags.pre("null");
            result.WriteTo(context);
            return;
        }

        dynamic? requestDiv;
        if (response.Request is { } request)
        {
            var requestUriString = request.Uri?.ToString();
            var requestHyperLink =
                string.IsNullOrWhiteSpace(requestUriString)
                    ? "[Unknown]"
                    : PocketViewTags.a[href: requestUriString](requestUriString);

            var requestLine =
                PocketViewTags.h3(
                    $"{request.Method} ", requestHyperLink, $" HTTP/{request.Version}");

            var requestHeaders =
                PocketViewTags.details(
                    PocketViewTags.summary("Headers"),
                    HeaderTable(request.Headers, request.Content?.Headers));

            var requestBodyString = request.Content?.Raw ?? string.Empty;
            var requestBodyLength = request.Content?.ByteLength ?? 0;
            var requestContentType = response.Content?.ContentType;
            var requestContentTypePrefix = requestContentType is null ? null : $"{requestContentType}, ";

            var requestBody =
                PocketViewTags.details(
                    PocketViewTags.summary($"Body ({requestContentTypePrefix}{requestBodyLength} bytes)"),
                    PocketViewTags.pre(requestBodyString));

            requestDiv =
                PocketViewTags.div(
                    PocketViewTags.h2("Request"),
                    PocketViewTags.hr(),
                    requestLine,
                    requestHeaders,
                    requestBody);
        }
        else
        {
            requestDiv = PocketViewTags.div(PocketViewTags.h2("Request"), PocketViewTags.hr());
        }

        var responseLine =
            PocketViewTags.h3(
                $"HTTP/{response.Version} {response.StatusCode} {response.ReasonPhrase} ({response.ElapsedMilliseconds:0.##} ms)");

        var responseHeaders =
            PocketViewTags.details[open: true](
                PocketViewTags.summary("Headers"),
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
            PocketViewTags.details[open: true](
                PocketViewTags.summary($"Body ({responseContentTypePrefix}{responseBodyLength} bytes)"),
                responseObjToFormat);

        var responseDiv =
            PocketViewTags.div(
                PocketViewTags.h2("Response"),
                PocketViewTags.hr(),
                responseLine,
                responseHeaders,
                responseBody);

        PocketView output =
            PocketViewTags.div[@class: ContainerClass](
                PocketViewTags.style[type: "text/css"](CSS),
                requestDiv,
                responseDiv);

        output.WriteTo(context);
    }

    private static dynamic HeaderTable(Dictionary<string, string[]> headers, Dictionary<string, string[]>? contentHeaders = null)
    {
        var allHeaders = contentHeaders is null ? headers : headers.Concat(contentHeaders);

        var headerTable =
            PocketViewTags.table(
                PocketViewTags.thead(
                    PocketViewTags.tr(
                        PocketViewTags.th("Name"), PocketViewTags.th("Value"))),
                PocketViewTags.tbody(
                    allHeaders.Select(header =>
                        PocketViewTags.tr(
                            PocketViewTags.td(header.Key), PocketViewTags.td(string.Join("; ", header.Value))))));

        return headerTable;
    }

    internal static void FormatAsPlainText(this HttpResponse? response, FormatContext context)
    {
        if (response is null)
        {
            context.Writer.WriteLine("null");
            return;
        }

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
}

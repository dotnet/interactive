// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting;

internal static class HttpResponseMessageFormattingExtensions
{
    private const string _containerClass = "http-response-message-container";
    private const string _logContainerClass = "aspnet-logs-container";

    private static readonly HtmlString _flexCss = new($@"
            .{_containerClass} {{
                display: flex;
                flex-wrap: wrap;
            }}

            .{_containerClass} > div {{
                margin: .5em;
                padding: 1em;
                border: 1px solid;
            }}

            .{_containerClass} > div > h2 {{
                margin-top: 0;
            }}

            .{_containerClass} > div > h3 {{
                margin-bottom: 0;
            }}

            .{_logContainerClass} {{
                margin: 0 .5em;
            }}

            .{_containerClass} summary, .{_logContainerClass} summary {{
                margin: 1em 0;
                font-size: 1.17em;
                font-weight: 700;
            }}");

    public  static async Task FormatAsHtml(
        this HttpResponseMessage responseMessage,
        FormatContext context)
    {
        var requestMessage = responseMessage.RequestMessage;
        var requestUri = requestMessage.RequestUri.ToString();
        var requestBodyString = requestMessage.Content is { } ?
            await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) :
            string.Empty;

        var requestLine = PocketViewTags.h3($"{requestMessage.Method} ", PocketViewTags.a[href: requestUri](requestUri), $" HTTP/{requestMessage.Version}");
        var requestHeaders = PocketViewTags.details(PocketViewTags.summary("Headers"), HeaderTable(requestMessage.Headers, requestMessage.Content?.Headers));

        var requestBody = PocketViewTags.details(
            PocketViewTags.summary("Body"),
            PocketViewTags.pre(requestBodyString));

        var responseLine = PocketViewTags.h3($"HTTP/{responseMessage.Version} {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");

        var responseHeaders = PocketViewTags.details[open: true](
            PocketViewTags.summary("Headers"), HeaderTable(responseMessage.Headers, responseMessage.Content.Headers));

        var responseBodyString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        object responseObjToFormat;

        try
        {
            responseObjToFormat = JsonDocument.Parse(responseBodyString);
        }
        catch (JsonException)
        {
            responseObjToFormat = responseBodyString;
        }

        var responseBody = PocketViewTags.details[open: true](
            PocketViewTags.summary("Body"),
            responseObjToFormat);

        PocketView output = PocketViewTags.div[@class: _containerClass](
            PocketViewTags.style[type: "text/css"](_flexCss),
            PocketViewTags.div(PocketViewTags.h2("Request"), PocketViewTags.hr(), requestLine, requestHeaders, requestBody),
            PocketViewTags.div(PocketViewTags.h2("Response"), PocketViewTags.hr(), responseLine, responseHeaders, responseBody));
        
        output.WriteTo(context);

    }

    private static dynamic HeaderTable(HttpHeaders headers, HttpContentHeaders contentHeaders) => PocketViewTags.table(PocketViewTags.thead(PocketViewTags.tr(PocketViewTags.th("Name"), PocketViewTags.th("Value"))), PocketViewTags.tbody((contentHeaders is null ? headers : headers.Concat(contentHeaders)).Select(header => PocketViewTags.tr(PocketViewTags.td(header.Key), PocketViewTags.td(string.Join("; ", header.Value))))));


    public  static async Task FormatAsPlainText(
        this HttpResponseMessage responseMessage,
        FormatContext context)
    {
        var responseBodyString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

        context.Writer.WriteLine($"Status Code: {(int)responseMessage.StatusCode} {responseMessage.StatusCode}");
        context.Writer.WriteLine($"Request URI: {responseMessage.RequestMessage.RequestUri}");
        context.Writer.WriteLine($"Content type: {responseMessage.Content.Headers.ContentType}");
        foreach (var header in responseMessage.Headers)
        {
            context.Writer.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        if (!string.IsNullOrWhiteSpace(responseBodyString))
        {
            switch (responseMessage.Content.Headers.ContentType.MediaType)
            {
                case "application/json":
                case "text/json":
                    var formatted = JsonSerializer.Serialize(JsonDocument.Parse(responseBodyString).RootElement, new JsonSerializerOptions { WriteIndented = true });
                    context.Writer.WriteLine($"Body: {formatted}");
                    break;
                default:
                    context.Writer.WriteLine($"Body: {responseBodyString}");
                    break;
            }
        }
    }
}
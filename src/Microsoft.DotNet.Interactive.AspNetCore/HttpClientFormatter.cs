﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class HttpClientFormatter
    {
        private const string _logKey = "aspnetcore-logs";
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

        public static async Task FormatHttpResponseMessage(
            HttpResponseMessage responseMessage, 
            FormatContext context)
        {
            var requestMessage = responseMessage.RequestMessage;
            var requestUri = requestMessage.RequestUri.ToString();
            var requestBodyString = requestMessage.Content is {} ?
                await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) :
                string.Empty;

            var requestLine = h3($"{requestMessage.Method} ", a[href: requestUri](requestUri), $" HTTP/{requestMessage.Version}");
            var requestHeaders = details(summary("Headers"), HeaderTable(requestMessage.Headers, requestMessage.Content?.Headers));

            var requestBody = details(
                summary("Body"), 
                pre(requestBodyString));

            var responseLine = h3($"HTTP/{responseMessage.Version} {(int) responseMessage.StatusCode} {responseMessage.ReasonPhrase}");

            var responseHeaders = details[open: true](
                summary("Headers"), HeaderTable(responseMessage.Headers, responseMessage.Content.Headers));

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

            var responseBody = details[open: true](
                summary("Body"),
                responseObjToFormat);

            PocketView output = div[@class: _containerClass](
                style[type: "text/css"](_flexCss),
                div(h2("Request"), hr(), requestLine, requestHeaders, requestBody),
                div(h2("Response"), hr(), responseLine, responseHeaders, responseBody));

            output.WriteTo(context);

            if (requestMessage.Options.TryGetValue(new HttpRequestOptionsKey<ConcurrentQueue<LogMessage>>(_logKey), out var aspnetLogs)
                && !aspnetLogs.IsEmpty)
            {
                PocketView logs = details[@class: _logContainerClass](summary("Logs"), aspnetLogs);

                logs.WriteTo(context);
            }
        }

        private static dynamic HeaderTable(HttpHeaders headers, HttpContentHeaders contentHeaders) => table(thead(tr(th("Name"), th("Value"))), tbody((contentHeaders is null ? headers : headers.Concat(contentHeaders)).Select(header => tr(td(header.Key), td(string.Join("; ", header.Value))))));

        public static HttpClient CreateEnhancedHttpClient(string address, InteractiveLoggerProvider interactiveLoggerProvider) =>
            new(new LogCapturingHandler(interactiveLoggerProvider))
            {
                BaseAddress = new Uri(address)
            };

        private class LogCapturingHandler : DelegatingHandler
        {
            private readonly InteractiveLoggerProvider _interactiveLoggerProvider;

            public LogCapturingHandler(InteractiveLoggerProvider interactiveLoggerProvider)
                : base(new SocketsHttpHandler())
            {
                _interactiveLoggerProvider = interactiveLoggerProvider;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var logs = new ConcurrentQueue<LogMessage>();
                request.Options.Set(new HttpRequestOptionsKey<ConcurrentQueue<LogMessage>>(_logKey), logs);

                _interactiveLoggerProvider.Posted += logs.Enqueue;

                try
                {
                    var responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    // Wait to download the body so we catch all the logs. If someone wants to access the response before
                    // downloading the body, they're free to create a their own HttpClient.
                    await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return responseMessage;
                }
                finally
                {
                    _interactiveLoggerProvider.Posted -= logs.Enqueue;
                }
            }
        }
    }
}

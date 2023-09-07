// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.HttpRequest;

namespace Microsoft.DotNet.Interactive.HttpRequest;

public static class HttpResponseMessageFormattingExtensions
{
    public static void RegisterFormatters()
    {
        Formatter.Register<HttpResponseMessage>(
            formatter: (responseMessage, context) =>
            {
                // Prevent SynchronizationContext-induced deadlocks given the following sync-over-async code.
                ExecutionContext.SuppressFlow();

                try
                {
                    responseMessage.FormatAsHtmlAsync(context).Wait();
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }

                return true;
            },
            mimeType: HtmlFormatter.MimeType);

        Formatter.Register<HttpResponseMessage>(
            formatter: (responseMessage, context) =>
            {
                // Prevent SynchronizationContext-induced deadlocks given the following sync-over-async code.
                ExecutionContext.SuppressFlow();

                try
                {
                    responseMessage.FormatAsPlainTextAsync(context).Wait();
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }

                return true;
            },
            mimeType: PlainTextFormatter.MimeType);

        Formatter.SetPreferredMimeTypesFor(
            typeof(HttpResponseMessage),
                HtmlFormatter.MimeType,
                PlainTextFormatter.MimeType);
    }

    private static async Task FormatAsHtmlAsync(
        this HttpResponseMessage? responseMessage,
        FormatContext context)
    {
        var response = await responseMessage.ToHttpResponseAsync();
        response.FormatAsHtml(context);
    }

    private static async Task FormatAsPlainTextAsync(
        this HttpResponseMessage? responseMessage,
        FormatContext context)
    {
        var response = await responseMessage.ToHttpResponseAsync();
        response.FormatAsPlainText(context);
    }
}
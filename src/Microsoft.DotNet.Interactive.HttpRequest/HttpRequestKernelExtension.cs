// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.HttpRequest;

public class HttpRequestKernelExtension : IKernelExtension
{
    public Task OnLoadAsync(Kernel kernel)
    {
        if (kernel.RootKernel is CompositeKernel compositeKernel)
        {
            var httpRequestKernel = new HttpRequestKernel();
            compositeKernel.Add(httpRequestKernel);
            httpRequestKernel.UseValueSharing();
            
            Formatter.Register<HttpResponseMessage>((responseMessage, context) =>
            {
                // Formatter.Register() doesn't support async formatters yet.
                // Prevent SynchronizationContext-induced deadlocks given the following sync-over-async code.
                ExecutionContext.SuppressFlow();
                try
                {
                    FormatHttpResponseMessage(
                        responseMessage,
                        context).Wait();
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }

                return true;
            }, PlainTextFormatter.MimeType);
        }
        return Task.CompletedTask;
    }

    private static async Task FormatHttpResponseMessage(
        HttpResponseMessage responseMessage,
        FormatContext context)
    {
        var responseBodyString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

        context.Writer.WriteLine($"Status Code: {responseMessage.StatusCode}");
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
                    System.Text.Json.JsonDocument.Parse(responseBodyString).RootElement.FormatTo(context, PlainTextFormatter.MimeType);
                    //context.
                    //context.Writer.WriteLine(System.Text.Json.JsonDocument.Parse(responseBodyString).RootElement.ToDisplayString(PlainText
                    //Microsoft.DotNet.Interactive.JsonConverter<string>
                    break;
                default:
                    context.Writer.WriteLine($"Body: {responseBodyString}");
                    break;
            }
        }
    }
}
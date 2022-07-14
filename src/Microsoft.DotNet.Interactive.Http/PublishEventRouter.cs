// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Http.Utility;

namespace Microsoft.DotNet.Interactive.Http;

internal class PublishEventRouter : IRouter
{
    private readonly HtmlNotebookFrontendEnvironment _frontendEnvironment;

    public PublishEventRouter(HtmlNotebookFrontendEnvironment frontendEnvironment)
    {
        _frontendEnvironment = frontendEnvironment ?? throw new ArgumentNullException(nameof(frontendEnvironment));
    }

    public VirtualPathData GetVirtualPath(VirtualPathContext context)
    {
        return null;
    }

    public async Task RouteAsync(RouteContext context)
    {
        if (context.HttpContext.Request.Method == HttpMethods.Post)
        {
            var segments =
                context.HttpContext
                    .Request
                    .Path
                    .Value
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 1 && segments[0] == "publishEvent")
            {
                using var reader = new StreamReader(context.HttpContext.Request.Body);
                var requestText = await reader.ReadToEndAsync();
                var requestObject = JsonDocument.Parse(requestText).RootElement;
                var commandToken = requestObject.GetPropertyFromPath("commandToken")?.GetString();
                var eventEnvelopeText = requestObject.GetPropertyFromPath("eventEnvelope")?.GetRawText();
                _frontendEnvironment.PublishEventFromCommand(commandToken, command =>
                {
                    context.Handler = async httpContext =>
                    {
                        httpContext.Response.StatusCode = 200;
                        await httpContext.Response.CompleteAsync();
                    };
                    var eventEnvelope = KernelEventEnvelope.DeserializeWithCommand(eventEnvelopeText, command);
                    return eventEnvelope.Event;
                });
            }
        }
    }
}
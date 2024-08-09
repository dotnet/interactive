// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Http;

internal class VariableRouter : IRouter
{
    private static readonly JsonSerializerOptions SerializerOptions;

    static VariableRouter()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString |
                             JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new DataDictionaryConverter() }
        };
    }
    private readonly Kernel _kernel;

    public VariableRouter(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    public VirtualPathData GetVirtualPath(VirtualPathContext context)
    {
        return null;
    }

    public async Task RouteAsync(RouteContext context)
    {
        if (context.HttpContext.Request.Method == HttpMethods.Get)
        {
            await SingleVariableRequest(context);
        }
        else if (context.HttpContext.Request.Method == HttpMethods.Post)
        {
            await BatchVariableRequest(context);
        }
    }

    private async Task BatchVariableRequest(RouteContext context)
    {
        var segments =
            context.HttpContext
                .Request
                .Path
                .Value
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 1 && segments[0] == "variables")
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body);
            var source = await reader.ReadToEndAsync();
            var query = JsonDocument.Parse(source).RootElement;
            var response = new Dictionary<string,object>();
            foreach (var kernelProperty in query.EnumerateObject())
            {
                var kernelName = kernelProperty.Name;
                var propertyBag = new Dictionary<string,object>();
                response[kernelName] = propertyBag;
                var targetKernel = GetKernel(kernelName);
                if (targetKernel is null)
                {
                    context.Handler = async httpContext =>
                    {
                        httpContext.Response.StatusCode = 400;
                        await httpContext.Response.WriteAsync($"kernel {kernelName} not found");
                        await httpContext.Response.CompleteAsync();
                    };
                    return;
                }
                    
                if (targetKernel.KernelInfo.SupportedKernelCommands.Any(c =>c.Name == nameof(RequestValue)))
                {
                    foreach (var variableName in kernelProperty.Value.EnumerateArray().Select(v => v.GetString()))
                    {
                        var value = await GetValueAsync(targetKernel, variableName);
                        if (value is {})
                        {
                            propertyBag[variableName] = JsonDocument.Parse(value.Value).RootElement;
                        }
                        else
                        {
                            context.Handler = async httpContext =>
                            {
                                httpContext.Response.StatusCode = 400;
                                await httpContext.Response.WriteAsync($"variable {variableName} not found on kernel {kernelName}");
                                await httpContext.Response.CompleteAsync();
                            };
                            return;
                        }
                    }
                }
                else
                {
                    context.Handler = async httpContext =>
                    {
                        httpContext.Response.StatusCode = 400;
                        await httpContext.Response.WriteAsync($"kernel {kernelName} doesn't support RequestValue");
                        await httpContext.Response.CompleteAsync();
                    };
                    return;
                }
            }

            context.Handler = async httpContext =>
            {
                httpContext.Response.ContentType = JsonFormatter.MimeType;

                await using (var writer = new StreamWriter(httpContext.Response.Body))
                {
                    await writer.WriteAsync(JsonSerializer.Serialize( response, SerializerOptions));
                }

                await httpContext.Response.CompleteAsync();
            };
        }
    }

    private static async Task<FormattedValue> GetValueAsync(Kernel targetKernel, string variableName)
    {
        var result = await targetKernel.SendAsync(new RequestValue(variableName));

        if (result.Events[0] is ValueProduced { Value: { } value })
        {
            return new FormattedValue(JsonFormatter.MimeType, value.ToDisplayString(JsonFormatter.MimeType));
        }

        return null;
    }

    private async Task SingleVariableRequest(RouteContext context)
    {
        var segments =
            context.HttpContext
                .Request
                .Path
                .Value
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (segments.FirstOrDefault() == "variables")
        {
            var kernelName = segments[1];
            var variableName = segments[2];

            var targetKernel = GetKernel(kernelName);
            if (targetKernel?.SupportsCommandType(typeof(RequestValue)) == true)
            {
                var value = await GetValueAsync(targetKernel, variableName);
                if (value is { })
                {
                    context.Handler = async httpContext =>
                    {
                        await using (var writer = new StreamWriter(httpContext.Response.Body))
                        {
                            httpContext.Response.ContentType = JsonFormatter.MimeType;
                            await writer.WriteAsync(value.Value);
                        }

                        await httpContext.Response.CompleteAsync();
                    };
                }
            }
        }
    }

    private Kernel GetKernel(string kernelName) => _kernel.FindKernelByName(kernelName);
}
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
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Http
{
    public class VariableRouter : IRouter
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
                Converters =
                {
                    new TableSchemaFieldTypeConverter(),
                    new TabularDataResourceConverter(),
                    new DataDictionaryConverter()
                }
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
                SingleVariableRequest(context);
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
                    
                    if (targetKernel.SupportsCommandType(typeof(RequestValue)) || targetKernel is ISupportGetValue)
                    {
                        foreach (var variableName in kernelProperty.Value.EnumerateArray().Select(v => v.GetString()))
                        {
                            var value = TryGetValue(targetKernel, variableName);

                            if (value is {} )
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

        private FormattedValue TryGetValue(Kernel targetKernel, string variableName)
        {
            if (targetKernel is ISupportGetValue fromInProcessKernel)
            {
                if (fromInProcessKernel.TryGetValue(variableName, out object value))
                {
                    return new FormattedValue(JsonFormatter.MimeType, value.ToDisplayString(JsonFormatter.MimeType));
                }

                return null;
            }
            
            return null;
        }

        private void SingleVariableRequest(RouteContext context)
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

                if (targetKernel is ISupportGetValue languageKernel)
                {
                    if (languageKernel.TryGetValue(variableName, out object value))
                    {
                        context.Handler = async httpContext =>
                        {
                            await using (var writer = new StreamWriter(httpContext.Response.Body))
                            {
                                httpContext.Response.ContentType = JsonFormatter.MimeType;
                                await writer.WriteAsync(value.ToDisplayString(JsonFormatter.MimeType));
                            }

                            await httpContext.Response.CompleteAsync();
                        };
                    }
                }
            }
        }

        private Kernel GetKernel(string kernelName)
        {
            Kernel targetKernel = null;
            if (_kernel.Name != kernelName)
            {
                if (_kernel is CompositeKernel composite)
                {
                    targetKernel = composite.ChildKernels.FirstOrDefault(k => k.Name == kernelName);
                }
            }
            return targetKernel;
        }
    }
}
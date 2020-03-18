// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Formatting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App.HttpRouting
{
    public class VariableRouter : IRouter
    {
        private readonly IKernel _kernel;

        public VariableRouter(IKernel kernel)
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
                var query = JObject.Parse(source);
                var response = new JObject();
                foreach (var kernelProperty in query.Properties())
                {
                    var kernelName = kernelProperty.Name;
                    var propertyBag = new JObject();
                    response[kernelName] = propertyBag;
                    var targetKernel = GetKernel(kernelName);
                    if (targetKernel is KernelBase kernelBase)
                    {
                        foreach (var variableName in kernelProperty.Value.Values<string>())
                        {
                            if (kernelBase.TryGetVariable(variableName, out var value))
                            {
                                if (value is string)
                                {
                                    propertyBag[variableName] = JToken.FromObject(value);
                                }
                                else
                                {
                                    propertyBag[variableName] = JToken.Parse(value.ToDisplayString(JsonFormatter.MimeType));
                                }
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
                            await httpContext.Response.WriteAsync($"kernel {kernelName} not found");
                            await httpContext.Response.CompleteAsync();
                        };
                        return;
                    }
                }

                context.Handler = async httpContext =>
                {
                    httpContext.Response.ContentType = JsonFormatter.MimeType;

                    await using var writer = new StreamWriter(httpContext.Response.Body) { AutoFlush = false };

                    await writer.WriteAsync(response.ToString());
                    await httpContext.Response.CompleteAsync();
                };
            }
        }

        private void SingleVariableRequest(RouteContext context)
        {
            var segments =
                context.HttpContext
                    .Request
                    .Path
                    .Value
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments[0] == "variables")
            {
                var kernelName = segments[1];
                var variableName = segments[2];

                var targetKernel = GetKernel(kernelName);

                if (targetKernel is KernelBase kernelBase)
                {
                    if (kernelBase.TryGetVariable(variableName, out var value))
                    {
                        context.Handler = async httpContext =>
                        {
                            await using var writer = new StreamWriter(httpContext.Response.Body);

                            httpContext.Response.ContentType = JsonFormatter.MimeType;
                            if (value is string)
                            {
                                await writer.WriteAsync(JsonConvert.ToString(value));
                            }
                            else
                            {
                                await writer.WriteAsync(value.ToDisplayString(JsonFormatter.MimeType));

                            }

                            await httpContext.Response.CompleteAsync();
                        };
                    }
                }
            }
        }

        private IKernel GetKernel(string kernelName)
        {
            IKernel targetKernel = null;
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
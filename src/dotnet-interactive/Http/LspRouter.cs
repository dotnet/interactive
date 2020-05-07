// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.App.Lsp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App.Http
{
    public class LspRouter : IRouter
    {
        private readonly IKernel _kernel;

        public LspRouter(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
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
                        .Split(new[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 2 && segments[0] == "lsp")
                {
                    var targetKernel = _kernel;

                    // TODO: figure out how to switch kernels based on #!fsharp, etc.
                    if (targetKernel is CompositeKernel composite)
                    {
                        targetKernel = composite.ChildKernels.FirstOrDefault(k => k.Name == composite.DefaultKernelName);
                    }

                    if (targetKernel is KernelBase kernelBase)
                    {
                        var methodName = segments[1];
                        var lspBodyReader = new StreamReader(context.HttpContext.Request.Body);
                        var stringBody = await lspBodyReader.ReadToEndAsync();
                        context.Handler = async httpContext =>
                        {
                            bool success;
                            JObject response;
                            try
                            {
                                var request = JObject.Parse(stringBody);
                                (success, response) = await kernelBase.HandleLspMethod(methodName, request);
                            }
                            catch (JsonSerializationException)
                            {
                                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                await httpContext.Response.WriteAsync($"unable to parse request object '{stringBody}'");
                                await httpContext.Response.CompleteAsync();
                                return;
                            }

                            if (success)
                            {
                                using var writer = new StringWriter();
                                LspSerializer.JsonSerializer.Serialize(writer, response);
                                var responseJson = writer.ToString();
                                httpContext.Response.ContentType = "application/json";
                                await httpContext.Response.WriteAsync(responseJson);
                            }
                            else
                            {
                                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                await httpContext.Response.WriteAsync($"method '{methodName}' not found on kernel '{kernelBase.Name}'");
                            }
                        };
                    }
                }
            }
        }
    }
}

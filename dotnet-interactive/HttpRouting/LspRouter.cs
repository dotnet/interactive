// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.App.HttpRouting
{
    public class LspRouter : IRouter
    {
        private readonly IKernel _kernel;
        private readonly JsonSerializerSettings _serializerSettings;

        public LspRouter(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };
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
                        var request = JObject.Parse(stringBody);
                        var response = await kernelBase.LspMethod(methodName, request);
                        var responseJson = (response is {})
                            ? JsonConvert.SerializeObject(response, _serializerSettings)
                            : string.Empty;
                        context.Handler = async httpContext =>
                        {
                            httpContext.Response.ContentType = "application/json";
                            await httpContext.Response.WriteAsync(responseJson);
                        };
                    }
                }
            }
        }
    }
}

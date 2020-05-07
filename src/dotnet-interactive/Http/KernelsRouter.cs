// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.App.Http
{
    public class KernelsRouter : IRouter
    {
        private readonly IKernel _kernel;

        public KernelsRouter(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            if (context.HttpContext.Request.Method == HttpMethods.Get)
            {
                var segments =
                    context.HttpContext
                        .Request
                        .Path
                        .Value
                        .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments[0] == "kernels")
                {
                    var targetKernel = _kernel;
                    var names = new List<string>
                    {
                        targetKernel.Name
                    };

                    if (targetKernel is CompositeKernel compositeKernel)
                    {
                        names.AddRange(compositeKernel.ChildKernels.Select(k => k.Name));
                    }

                    context.Handler = async httpContext =>
                    {
                        httpContext.Response.ContentType = "application/json";
                        await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(names));
                    };
                }
            }

            return Task.CompletedTask;
        }
    }
}
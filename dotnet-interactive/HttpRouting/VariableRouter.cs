// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

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

        public Task RouteAsync(RouteContext context)
        {
            var segments =
                context.HttpContext
                       .Request
                       .Path
                       .Value
                       .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments[0] == "variables")
            {
                var targetKernel = _kernel;

                if (_kernel.Name != segments[1])
                {
                    if (_kernel is CompositeKernel composite)
                    {
                        targetKernel = composite.ChildKernels.First(k => k.Name == segments[1]);
                    }
                }

                if (targetKernel is KernelBase kernelBase)
                {
                    var value = kernelBase.GetVariable(segments[2]);
                    context.Handler = async httpContext =>
                    {
                        httpContext.Response.ContentType = "application/json";
                        await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(value));
                    };
                }
            }

            return Task.CompletedTask;
        }
    }
}
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
                    .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            if (segments[0] == "variables")
            {
                var composite = _kernel as CompositeKernel;

                var target = composite.ChildKernels.First(k => k.Name == segments[1]);

                var variable = target.GetVariable(segments[2]);
                context.Handler = async httpContext =>
                {
                    httpContext.Response.ContentType = "application/jon";
                    await httpContext.Response.WriteAsync( JsonConvert.SerializeObject(variable) );
                };
            }

            return Task.CompletedTask;
        }
    }
}
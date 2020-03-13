// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Formatting;
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
            if (context.HttpContext.Request.Method == HttpMethods.Get)
            {
                var segments =
                    context.HttpContext
                        .Request
                        .Path
                        .Value
                        .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

                if (segments[0] == "variables")
                {
                    var targetKernel = _kernel;

                    if (_kernel.Name != segments[1])
                    {
                        if (_kernel is CompositeKernel composite)
                        {
                            targetKernel = composite.ChildKernels.FirstOrDefault(k => k.Name == segments[1]);
                        }
                    }

                    if (targetKernel is KernelBase kernelBase)
                    {
                        if (kernelBase.TryGetVariable(segments[2], out var value))
                        {
                            context.Handler = async httpContext =>
                            {
                                httpContext.Response.ContentType = JsonFormatter.MimeType;

                                await using var writer = new StreamWriter(httpContext.Response.Body);

                                value.FormatTo(writer, JsonFormatter.MimeType);
                            };
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
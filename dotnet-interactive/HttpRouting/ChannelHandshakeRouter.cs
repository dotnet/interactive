// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Jupyter;

namespace Microsoft.DotNet.Interactive.App.HttpRouting
{
    public class ChannelHandshakeRouter : IRouter
    {
        private readonly JupyterFrontendEnvironment _frontendEnvironment;

        public ChannelHandshakeRouter(JupyterFrontendEnvironment frontendEnvironment)
        {
            _frontendEnvironment = frontendEnvironment;
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
                        .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                if (segments[0] == "channelhandshake")
                {
                    // get the body
                    using var reader = new StreamReader(context.HttpContext.Request.Body);
                    var source = await reader.ReadToEndAsync();
                    var body = new Uri( source);
                    _frontendEnvironment.Host = body;

                    // Do something
                    context.Handler = async httpContext =>
                    {
                        await httpContext.Response.CompleteAsync(); 

                    };

                }
            }
        }
    }
}
﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.App.CommandLine;

namespace Microsoft.DotNet.Interactive.App.HttpRouting
{
    public class DiscoveryRouter : IRouter
    {
        private readonly BrowserFrontendEnvironment _frontendEnvironment;

        public DiscoveryRouter(BrowserFrontendEnvironment frontendEnvironment)
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
                if (segments[0] == "discovery")
                {
                    // get the body
                    using var reader = new StreamReader(context.HttpContext.Request.Body);
                    var source = await reader.ReadToEndAsync();
                    var apiUri = new Uri( source);
                    _frontendEnvironment.ApiUri = apiUri;

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
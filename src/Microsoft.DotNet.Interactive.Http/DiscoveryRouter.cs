// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.DotNet.Interactive.Http
{
    public class DiscoveryRouter : IRouter
    {
        private readonly HtmlNotebookFrontedEnvironment _frontendEnvironment;

        public DiscoveryRouter(HtmlNotebookFrontedEnvironment frontendEnvironment)
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
                        .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (segments[0] == "discovery")
                {
                    using var reader = new StreamReader(context.HttpContext.Request.Body);
                    var source = await reader.ReadToEndAsync();
                    var apiUri = new Uri(source);
                    _frontendEnvironment.SetApiUri(apiUri);

                    context.Handler = async httpContext =>
                    {
                        await httpContext.Response.CompleteAsync();
                    };
                }
            }
        }
    }
}
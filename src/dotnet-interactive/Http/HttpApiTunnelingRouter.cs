// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App.Http
{
    public class HttpApiTunnelingRouter : IRouter
    {
        private readonly HtmlNotebookFrontedEnvironment _frontendEnvironment;

        private ConcurrentDictionary<Uri,string> _bootstrapperScripts = new ConcurrentDictionary<Uri, string>();

        public HttpApiTunnelingRouter(HtmlNotebookFrontedEnvironment frontendEnvironment)
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
                await HandlePostVerb(context);
            }

            if (context.HttpContext.Request.Method == HttpMethods.Get)
            {
                await HandleGetVerb(context);
            }
        }

        private async Task HandleGetVerb(RouteContext context)
        {
            var segments =
                context.HttpContext
                    .Request
                    .Path
                    .Value
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.ToLower())
                    .ToArray();
            if (segments[0] == "apitunnel")
            {
                context.Handler = async httpContext =>
                {
                    httpContext.Response.ContentType = "text/javascript";
                    var key = context.HttpContext.Request.GetUri();
                    if (_bootstrapperScripts.TryGetValue(key, out var scriptCode))
                    {
                        await httpContext.Response.WriteAsync(scriptCode);
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 404;
                    }
                   
                    await httpContext.Response.CompleteAsync();
 
                };

            }
          

           

        }

        private async Task HandlePostVerb(RouteContext context)
        {
            var segments =
                context.HttpContext
                    .Request
                    .Path
                    .Value
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments[0] == "apitunnel")
            {
                using var reader = new StreamReader(context.HttpContext.Request.Body);
                var source = await reader.ReadToEndAsync();

                var requestBody = JObject.Parse(source);
                
                var apiUri = new Uri(requestBody["tunnelUri"].Value<string>());
                var frontendType = requestBody["frontendType"].Value<string>();

                var bootstrapperUri = new Uri(apiUri, $"apitunnel/{frontendType}/{Guid.NewGuid():N}/bootstrapper.js");
                _frontendEnvironment.SetApiUri(apiUri);

                _bootstrapperScripts.GetOrAdd(bootstrapperUri, key => GenerateBootstrapperCode(key, frontendType));
                
                context.Handler = async httpContext =>
                {
                    httpContext.Response.ContentType = "text/plain";
                    var response = new JObject
                    {
                        {"bootstrapperUri",bootstrapperUri.ToString() }
                    };
                    await httpContext.Response.WriteAsync(response.ToString(Newtonsoft.Json.Formatting.None));
                    await httpContext.Response.CompleteAsync();
                };
            }
        }

        private string GenerateBootstrapperCode(Uri key, string frontendType)
        {
            return "to do";
        }
    }
}
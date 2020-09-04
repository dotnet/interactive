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

        private readonly ConcurrentDictionary<Uri,string> _bootstrapperScripts = new ConcurrentDictionary<Uri, string>();

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

        private Task HandleGetVerb(RouteContext context)
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

            return Task.CompletedTask;
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
                var hash = $"{Guid.NewGuid():N}";
                var bootstrapperUri = new Uri(apiUri, $"apitunnel/{frontendType}/{hash}/bootstrapper.js");
                _frontendEnvironment.SetApiUri(apiUri);

                _bootstrapperScripts.GetOrAdd(bootstrapperUri, key => GenerateBootstrapperCode(apiUri, frontendType, hash));
                
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

        private string GenerateBootstrapperCode(Uri externalUri, string frontendType, string hash)
        {
            string template = @"
// ensure `require` is available globally
function bootstrapper_$FRONTENDTYPE$_$HASH$() {
    let loadDotnetInteractiveApi = function () {
        // use probing to find host url and api resources
        // load interactive helpers and language services
        let dotnetInteractiveRequire = require.config({
            context: '$HASH$',
            paths: {
                'dotnet-interactive': '$EXTERNALURI$resources'
            },
            urlArgs: 'cacheBuster=$HASH$'
        }) || require;

        let dotnetInteractiveExtensionsRequire = require.config({
            context: '$HASH$',
            paths: {
                'dotnet-interactive-extensions': '$EXTERNALURI$extensions'
            }
        }) || require;

        if (!window.dotnetInteractiveRequire) {
            window.dotnetInteractiveRequire = dotnetInteractiveRequire;
        }

        if (!window.dotnetInteractiveExtensionsRequire) {
            window.dotnetInteractiveExtensionsRequire = dotnetInteractiveExtensionsRequire;
        }

        window.getExtensionRequire = function (extensionName, extensionCacheBuster) {
            let paths = {};
            paths[extensionName] = `$EXTERNALURI$extensions/${extensionName}/resources/`;

            let internalRequire = require.config({
                context: extensionCacheBuster,
                paths: paths,
                urlArgs: `cacheBuster=${extensionCacheBuster}`
            }) || require;

            return internalRequire
        };

        dotnetInteractiveRequire([
            'dotnet-interactive/dotnet-interactive'
        ],
            function (dotnet) {
                dotnet.init(window);
            },
            function (error) {
                console.log(error);
            }
        );
    }

    if (typeof require !== typeof Function || typeof require.config !== typeof Function) {
        let require_script = document.createElement('script');
        require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
        require_script.setAttribute('type', 'text/javascript');
        require_script.onload = function () {
            loadDotnetInteractiveApi();
        };

        document.getElementsByTagName('head')[0].appendChild(require_script);
    }
    else {
        loadDotnetInteractiveApi();
    }
}

bootstrapper_$FRONTENDTYPE$_$HASH$();
";

            return template
                .Replace("$HASH$", hash)
                .Replace("$EXTERNALURI$", externalUri.AbsoluteUri)
                .Replace("$FRONTENDTYPE$", frontendType);
        }
    }
}
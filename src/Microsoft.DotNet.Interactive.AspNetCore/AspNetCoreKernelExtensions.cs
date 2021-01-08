// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    public static class AspNetCoreKernelExtensions
    {
        private static readonly string[] _namespaces = new[]
        {
            typeof(HttpContext).Namespace, // Microsoft.AspNetCore.Http
            typeof(IEndpointRouteBuilder).Namespace, // Microsoft.AspNetCore.Routing
            typeof(EndpointRouteBuilderExtensions).Namespace, // Microsoft.AspNetCore.Builder
            typeof(InteractiveEndpointRouteBuilderExtensions).Namespace, // Microsoft.DotNet.Interactive.AspNetCore
            typeof(HttpClient).Namespace, // System.Net.Htttp
        };

        // Indirect references are found automatically
        private static readonly Assembly[] _references = new[]
        {
            typeof(Host).Assembly, // Microsoft.Extensions.Hosting
            typeof(WebHost).Assembly, // Microsoft.AspNetCore
            typeof(Controller).Assembly, // Microsoft.AspNetCore.Mvc.ViewFeatures
            typeof(DeveloperExceptionPageMiddleware).Assembly, // Microsoft.AspNetCore.Diagnostics
            typeof(AspNetCoreKernelExtensions).Assembly, // Microsoft.DotNet.Interactive.AspNetCore
        };

        public static CSharpKernel UseAspNetCore(this CSharpKernel kernel)
        {
            var isActive = false;

            var directive = new Command("#!aspnet", "Activate ASP.NET Core")
            {
                Handler = CommandHandler.Create(async () =>
                {
                    if (isActive)
                    {
                        return;
                    }

                    isActive = true;

                    // We could try to manage lifetime, but for now just stop the kernel if you want to stop the host.
                    var interactiveHost = new InteractiveHost();
                    var startHostTask = interactiveHost.StartAsync();

                    var rDirectives = string.Join(Environment.NewLine, _references.Select(a => $"#r \"{a.Location}\""));
                    var usings = string.Join(Environment.NewLine, _namespaces.Select(ns => $"using {ns};"));

                    await kernel.SendAsync(new SubmitCode($"{rDirectives}{Environment.NewLine}{usings}"), CancellationToken.None);
                    await startHostTask;

                    await kernel.SetVariableAsync<IApplicationBuilder>("App", interactiveHost.App);
                    await kernel.SetVariableAsync<IEndpointRouteBuilder>("Endpoints", interactiveHost.Endpoints);
                    await kernel.SetVariableAsync<HttpClient>("HttpClient", HttpClientFormatter.CreateEnhancedHttpClient(interactiveHost.Address));
                })
            };

            kernel.AddDirective(directive);

            Formatter.Register<HttpResponseMessage>((responseMessage, textWriter) =>
            {
                // Formatter.Register() doesn't support async formatters yet.
                // Prevent SynchronizationContext-induced deadlocks given the following sync-over-async code.
                ExecutionContext.SuppressFlow();

                try
                {
                    HttpClientFormatter.FormatHttpResponseMessage(responseMessage, textWriter).Wait();
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }
            }, HtmlFormatter.MimeType);

            return kernel;
        }

        // REVIEW: Should we make this a public CSharp/DotNetKernel extension method?
        private static Task SetVariableAsync<T>(this CSharpKernel kernel, string name, T value)
        {
            return kernel.SetVariableAsync(name, value, typeof(T));
        }
    }
}

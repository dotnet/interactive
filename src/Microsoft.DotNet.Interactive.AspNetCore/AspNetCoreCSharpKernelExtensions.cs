// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
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
    public static class AspNetCoreCSharpKernelExtensions
    {
        private static readonly Assembly[] _references = new[]
        {
            typeof(Host).Assembly, // Microsoft.Extensions.Hosting
            typeof(WebHost).Assembly, // Microsoft.AspNetCore
            typeof(Controller).Assembly, // Microsoft.AspNetCore.Mvc.ViewFeatures
            typeof(DeveloperExceptionPageMiddleware).Assembly, // Microsoft.AspNetCore.Diagnostics
            typeof(AspNetCoreCSharpKernelExtensions).Assembly, // Microsoft.DotNet.Interactive.AspNetCore
        };

        private static readonly string[] _namespaces = new[]
        {
            typeof(HttpContext).Namespace, // Microsoft.AspNetCore.Http
            typeof(IEndpointRouteBuilder).Namespace, // Microsoft.AspNetCore.Routing
            typeof(EndpointRouteBuilderExtensions).Namespace, // Microsoft.AspNetCore.Builder
            typeof(InteractiveEndpointRouteBuilderExtensions).Namespace, // Microsoft.DotNet.Interactive.AspNetCore
            typeof(HttpClient).Namespace, // System.Net.Htttp
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

                    var interactiveLoggerProvider = new InteractiveLoggerProvider();

                    kernel.AddMiddleware(async (command, context, next) =>
                    {
                        // REVIEW: Is there a way to log even when there's no command in progress?
                        // This is currently necessary because the #!log command checks
                        // `if (KernelInvocationContext.Current is {} currentContext)` in its LogEvents.Subscribe
                        // callback and KernelInvocationContext.Current is backed by an AsyncLocal.
                        // Is there a way to log to diagnostic output without KernelInvocationContext.Current?
                        using (interactiveLoggerProvider.SubscribePocketLogerWithCurrentEC())
                        {
                            await next(command, context);
                        }
                    });

                    // The middleware doesn't cover the current command's executions so we need this to capture startup logs.
                    using (interactiveLoggerProvider.SubscribePocketLogerWithCurrentEC())
                    {
                        // We could try to manage the host's lifetime, but for now just stop the kernel if you want to stop the host.
                        var interactiveHost = new InteractiveHost(interactiveLoggerProvider);
                        var startHostTask = interactiveHost.StartAsync();

                        var rDirectives = string.Join(Environment.NewLine, _references.Select(a => $"#r \"{a.Location}\""));
                        var usings = string.Join(Environment.NewLine, _namespaces.Select(ns => $"using {ns};"));

                        await startHostTask;

                        await kernel.SendAsync(new SubmitCode($"{rDirectives}{Environment.NewLine}{usings}"), CancellationToken.None);

                        var httpClient = HttpClientFormatter.CreateEnhancedHttpClient(interactiveHost.Address, interactiveLoggerProvider);
                        await kernel.SetVariableAsync<IApplicationBuilder>("App", interactiveHost.App);
                        await kernel.SetVariableAsync<IEndpointRouteBuilder>("Endpoints", interactiveHost.Endpoints);
                        await kernel.SetVariableAsync<HttpClient>("HttpClient", httpClient);
                    }
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
    }
}

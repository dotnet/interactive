﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
        private static readonly Assembly[] _references = 
        {
            typeof(Host).Assembly, // Microsoft.Extensions.Hosting
            typeof(WebHost).Assembly, // Microsoft.AspNetCore
            typeof(Controller).Assembly, // Microsoft.AspNetCore.Mvc.ViewFeatures
            typeof(DeveloperExceptionPageMiddleware).Assembly, // Microsoft.AspNetCore.Diagnostics
            typeof(AspNetCoreCSharpKernelExtensions).Assembly, // Microsoft.DotNet.Interactive.AspNetCore
        };

        private static readonly string[] _namespaces =
        {
            typeof(HttpContext).Namespace, // Microsoft.AspNetCore.Http
            typeof(IEndpointRouteBuilder).Namespace, // Microsoft.AspNetCore.Routing
            typeof(EndpointRouteBuilderExtensions).Namespace, // Microsoft.AspNetCore.Builder
            typeof(InteractiveEndpointRouteBuilderExtensions).Namespace, // Microsoft.DotNet.Interactive.AspNetCore
            typeof(HttpClient).Namespace, // System.Net.Htttp
        };

        public static CSharpKernel UseAspNetCore(this CSharpKernel kernel)
        {
            InteractiveHost interactiveHost = null;

            var directive = new Command("#!aspnet", "Activate ASP.NET Core")
            {
                Handler = CommandHandler.Create(async () =>
                {
                    if (interactiveHost is {})
                    {
                        return;
                    }

                    var interactiveLoggerProvider = new InteractiveLoggerProvider();

                    kernel.AddMiddleware(async (command, context, next) =>
                    {
                        // REVIEW: Is there a way to log even when there's no command in progress?
                        // This is currently necessary because the #!log command uses KernelInvocationContext.Current in
                        // its LogEvents.Subscribe callback and KernelInvocationContext.Current is backed by an AsyncLocal.
                        // Is there a way to log to diagnostic output without KernelInvocationContext.Current?
                        using (command is SubmitCode ? interactiveLoggerProvider.SubscribePocketLogerWithCurrentEC() : null)
                        {
                            await next(command, context).ConfigureAwait(false);
                        }
                    });

                    // The middleware doesn't cover the current command's executions so we need this to capture startup logs.
                    using (interactiveLoggerProvider.SubscribePocketLogerWithCurrentEC())
                    {
                        // We could try to manage the host's lifetime, but for now just stop the kernel if you want to stop the host.
                        interactiveHost = new InteractiveHost(interactiveLoggerProvider);
                        var startHostTask = interactiveHost.StartAsync();

                        var rDirectives = string.Join(Environment.NewLine, _references.Select(a => $"#r \"{a.Location}\""));
                        var usings = string.Join(Environment.NewLine, _namespaces.Select(ns => $"using {ns};"));

                        await kernel.SendAsync(new SubmitCode($"{rDirectives}{Environment.NewLine}{usings}"), CancellationToken.None).ConfigureAwait(false);

                        await startHostTask.ConfigureAwait(false);

                        var httpClient = HttpClientFormatter.CreateEnhancedHttpClient(interactiveHost.Address, interactiveLoggerProvider);
                        await kernel.SetVariableAsync<IApplicationBuilder>("App", interactiveHost.App).ConfigureAwait(false);
                        await kernel.SetVariableAsync<IEndpointRouteBuilder>("Endpoints", interactiveHost.Endpoints).ConfigureAwait(false);
                        await kernel.SetVariableAsync<HttpClient>("HttpClient", httpClient).ConfigureAwait(false);
                    }
                })
            };

            kernel.AddDirective(directive);

            kernel.AddDirective(new Command("#!aspnet-stop", "Stop ASP.NET Core host")
            {
                Handler = CommandHandler.Create(async () =>
                {
                    if (interactiveHost is null)
                    {
                        return;
                    }

                    await interactiveHost.DisposeAsync();

                    interactiveHost = null;
                })
            });

            Formatter.Register<HttpResponseMessage>((responseMessage, context) =>
            {
                // Formatter.Register() doesn't support async formatters yet.
                // Prevent SynchronizationContext-induced deadlocks given the following sync-over-async code.
                ExecutionContext.SuppressFlow();

                try
                {
                    HttpClientFormatter.FormatHttpResponseMessage(
                        responseMessage, 
                        context).Wait();
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }

                return true;
            }, HtmlFormatter.MimeType);

            return kernel;
        }
    }
}

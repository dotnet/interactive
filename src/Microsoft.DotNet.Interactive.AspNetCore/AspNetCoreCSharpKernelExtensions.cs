// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.Interactive.AspNetCore;

public static class AspNetCoreCSharpKernelExtensions
{
    private static readonly Assembly[] _references =
    {
        typeof(Host).Assembly, // Microsoft.Extensions.Hosting
        typeof(WebApplication).Assembly, // Microsoft.AspNetCore
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
        typeof(HttpClient).Namespace, // System.Net.Http
    };

    public static CSharpKernel UseAspNetCore(this CSharpKernel kernel)
    {
        InteractiveHost interactiveHost = null;

        var startDirective = new KernelActionDirective("#!aspnet")
        {
            Description = "Activate ASP.NET Core"
        };

        kernel.AddDirective(startDirective, Start);

        var stopDirective = new KernelActionDirective("#!aspnet-stop")
        {
            Description = "Stop ASP.NET Core host"
        };

        kernel.AddDirective(stopDirective, Stop);

        kernel.RegisterForDisposal(() =>
        {
            interactiveHost?.Dispose();
            interactiveHost = null;
        });

        async Task Start(KernelCommand _, KernelInvocationContext context)
        {
            if (interactiveHost is not null)
            {
                return;
            }

            var interactiveLoggerProvider = new InteractiveLoggerProvider();

            kernel.AddMiddleware(async (command, ctx, next) =>
            {
                // REVIEW: Is there a way to log even when there's no command in progress?
                // This is currently necessary because the #!log command uses KernelInvocationContext.Current in
                // its LogEvents.Subscribe callback and KernelInvocationContext.Current is backed by an AsyncLocal.
                // Is there a way to log to diagnostic output without KernelInvocationContext.Current?
                using (command is SubmitCode ? interactiveLoggerProvider.SubscribePocketLogerWithCurrentEC() : null)
                {
                    await next(command, ctx).ConfigureAwait(false);
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

                var httpClient = EnhancedHttpClient.Create(interactiveHost.Address, interactiveLoggerProvider);
                await kernel.SetValueAsync("App", interactiveHost.App, typeof(IApplicationBuilder)).ConfigureAwait(false);
                await kernel.SetValueAsync("Endpoints", interactiveHost.Endpoints, typeof(IEndpointRouteBuilder)).ConfigureAwait(false);
                await kernel.SetValueAsync("HttpClient", httpClient, typeof(HttpClient)).ConfigureAwait(false);
            }
        }

        async Task Stop(KernelCommand _, KernelInvocationContext context)
        {
            if (interactiveHost is null)
            {
                return;
            }

            await interactiveHost.DisposeAsync();

            interactiveHost = null;
        }

        return kernel;
    }
}
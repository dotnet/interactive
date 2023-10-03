// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Interactive.Http;

internal static class AspNetExtensions
{
    public static IServiceCollection AddKernel<T>(this IServiceCollection services, T kernel) where T : Kernel
    {
        services.AddSingleton(kernel);
        services.AddSingleton<Kernel>(kernel);

        return services;
    }

    public static IWebHostBuilder UseDotNetInteractiveHttpApi(this IWebHostBuilder builder, bool enableHttpApi,
        HttpPort httpPort, HttpProbingSettings httpProbingSettings, IServiceCollection services)
    {
        var httpStartupOptions = new HttpOptions(enableHttpApi, httpPort);

        builder.ConfigureServices(c =>
            {
                c.AddSingleton(httpStartupOptions);
            })
            .ConfigureServices(c =>
            {
                if (enableHttpApi && httpProbingSettings is not null)
                {
                    c.AddSingleton(httpProbingSettings);
                }

                c.AddSingleton(httpStartupOptions);

                if (services is not null)
                {
                    foreach (var serviceDescriptor in services)
                    {
                        c.Add(serviceDescriptor);
                    }
                }
            });
        return builder;
    }
    public static IServiceCollection AddDotnetInteractiveHttpApi(this IServiceCollection services)
    {
        services.AddSingleton(c => new KernelHubConnection(c.GetRequiredService<Kernel>(), c.GetRequiredService<SignalRBackchannelKernelClient>()));
        services.AddRouting();
        services.AddCors(options =>
        {
            options.AddPolicy("default", builder =>
            {
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed((host) => true);

            });
        });
        services.AddSignalR();

        return services;
    }

    public static IApplicationBuilder UseDotNetInteractiveHttpApi<T>(this IApplicationBuilder app, T kernel, Assembly staticResourceRoot, HttpProbingSettings httpProbingSettings, HttpPort httpPort) where T : Kernel
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new FileProvider(kernel, staticResourceRoot)
        });

        app.UseWebSockets();
        app.UseCors("default");
        app.UseRouting();
        app.UseRouter(r =>
        {
            r.Routes.Add(new VariableRouter(kernel));
            r.Routes.Add(new KernelsRouter(kernel));
            var htmlNotebookFrontendEnvironment = kernel.FrontendEnvironment as HtmlNotebookFrontendEnvironment;

            if (htmlNotebookFrontendEnvironment is { } )
            {
                r.Routes.Add(new DiscoveryRouter(htmlNotebookFrontendEnvironment));
                r.Routes.Add(new HttpApiTunnelingRouter(htmlNotebookFrontendEnvironment));
                r.Routes.Add(new PublishEventRouter(htmlNotebookFrontendEnvironment));
                r.Routes.Add(new ClientExecutionRouter(htmlNotebookFrontendEnvironment));
            }

            if (htmlNotebookFrontendEnvironment is null || htmlNotebookFrontendEnvironment.RequiresAutomaticBootstrapping)
            {
                var enableHttp = new SubmitCode("#!enable-http", kernel.Name);
                kernel.DeferCommand(enableHttp);
            }

            kernel = kernel.UseHttpApi(httpPort, httpProbingSettings);

        });
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<KernelHub>("/kernelhub");
        });
        return app;
    }
}
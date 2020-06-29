// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.Http;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Pocket;
using Serilog;

namespace Microsoft.DotNet.Interactive.App
{
    public class Startup
    {
        public Startup(
            IHostEnvironment env,
            StartupOptions startupOptions)
        {
            Environment = env;
            StartupOptions = startupOptions;

            var configurationBuilder = new ConfigurationBuilder();

            Configuration = configurationBuilder.Build();
        }

        protected IConfigurationRoot Configuration { get; }

        protected IHostEnvironment Environment { get; }

        public StartupOptions StartupOptions { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            using var _ = Pocket.Logger.Log.OnEnterAndExit();
            if (StartupOptions.EnableHttpApi)
            {
                services.AddSingleton((c) => new KernelHubConnection(c.GetRequiredService<CompositeKernel>()));
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
            }
        }

        public void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime lifetime,
            IServiceProvider serviceProvider)
        {
            var operation = Pocket.Logger.Log.OnEnterAndExit();
            if (StartupOptions.EnableHttpApi)
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new EmbeddedFileProvider(typeof(Startup).Assembly)
                });
                app.UseWebSockets();
                app.UseCors("default");
                app.UseRouting();
                app.UseRouter(r =>
                {
                    operation.Info("configuring routing");
                    var frontendEnvironment = serviceProvider.GetService<HtmlNotebookFrontedEnvironment>();
                    if (frontendEnvironment != null)
                    {
                        r.Routes.Add(new DiscoveryRouter(frontendEnvironment));
                    }

                    var kernel = serviceProvider.GetRequiredService<Kernel>();
                    var startupOptions = serviceProvider.GetRequiredService<StartupOptions>();
                    if (startupOptions.EnableHttpApi)
                    {
                        var httpProbingSettings = serviceProvider.GetRequiredService<HttpProbingSettings>();

                        kernel = kernel.UseHttpApi(startupOptions, httpProbingSettings);
                        var enableHttp = new SubmitCode("#!enable-http", kernel.Name);
                        enableHttp.PublishInternalEvents();
                        kernel.DeferCommand(enableHttp);
                    }

                    r.Routes.Add(new VariableRouter(kernel));
                    r.Routes.Add(new KernelsRouter(kernel));
                });
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<KernelHub>("/kernelhub");
                });
            }
        }
    }
}
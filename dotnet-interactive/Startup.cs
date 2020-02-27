// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FSharp.Compiler;
using Microsoft.AspNetCore.Builder;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.HttpRouting;
using Microsoft.DotNet.Interactive.App.SignalR;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

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

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            Configuration = configurationBuilder.Build();
        }

        protected IConfigurationRoot Configuration { get; }

        protected IHostEnvironment Environment { get; }

        public StartupOptions StartupOptions { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
        }

        public void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime lifetime,
            IServiceProvider serviceProvider)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles( new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(typeof(Startup).Assembly)
            });
            app.UseRouting();
            app.UseRouter(r =>
            {
                r.Routes.Add(new VariableRouter( serviceProvider.GetRequiredService<IKernel>()));
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<KernelHub>("/kernel");
            });
        }
    }
}
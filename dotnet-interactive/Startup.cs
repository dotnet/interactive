// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FSharp.Compiler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            IHostApplicationLifetime lifetime)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRouter(r => { r.Routes.Add(new SanityCheckRouter()); });
            app.UseEndpoints(builder =>
            {
                builder.MapHub<KernelHub>("/kernel");
            });
        }
    }

    public class SanityCheckRouter : IRouter
    {
        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            context.Handler = async httpContext => { await httpContext.Response.WriteAsync("hello dude"); };
            return Task.CompletedTask;
        }
    }

    public class KernelHub : Hub
    {
        private readonly Func<IKernel> _kernelFactory;

        public KernelHub()
        {
            //_kernelFactory = kernelFactory ?? throw new ArgumentNullException(nameof(kernelFactory));
            
        }

        
    }
}
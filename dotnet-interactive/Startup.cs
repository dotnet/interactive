// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FSharp.Compiler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.SignalR;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

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
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRouter(r =>
            {
                var kernel = serviceProvider.GetRequiredService<IKernel>();
                r.Routes.Add(new VariableRouter(kernel));
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<KernelHub>("/kernel");
            });
        }
    }

    public class VariableRouter : IRouter
    {
        private readonly IKernel _kernel;

        public VariableRouter(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            var segments =
                context.HttpContext
                    .Request
                    .Path
                    .Value
                    .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            if (segments[0] == "variables")
            {
                var composite = _kernel as CompositeKernel;

                var target = composite.ChildKernels.First(k => k.Name == segments[1]);

                var variable = target.GetVariable(segments[2]);
                context.Handler = async httpContext =>
                {
                    httpContext.Response.ContentType = "application/jon";
                    await httpContext.Response.WriteAsync( JsonConvert.SerializeObject(variable) );
                };
            }

            return Task.CompletedTask;
        }
    }
}
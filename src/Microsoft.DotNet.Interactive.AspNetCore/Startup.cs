// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class Startup
    {
        public IApplicationBuilder App { get; private set; }
        public IEndpointRouteBuilder Endpoints { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .SetIsOriginAllowed(_ => true);
                    });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            App = app.New();
            App.UseRouting();
            App.UseCors("AllowAll");
            App.UseEndpoints(endpoints =>
            {
                Endpoints = endpoints;
            });

            app.Use(next =>
                httpContext =>
                   App.Build()(httpContext));
        }
    }
}

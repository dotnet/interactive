// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public static class AspNetKernelExtensions
    {
        public static CSharpKernel UseAspNet(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode(@"
Environment.SetEnvironmentVariable($""ASPNETCORE_{WebHostDefaults.PreventHostingStartupKey}"", ""true"");

private static int __AspNet_NextEndpointOrder;

public static IEndpointConventionBuilder MapAction(
    this IEndpointRouteBuilder endpoints,
    string pattern,
    RequestDelegate requestDelegate)
{
    var order = __AspNet_NextEndpointOrder--;
    var builder = endpoints.MapGet(pattern, requestDelegate);
    builder.Add(b => ((RouteEndpointBuilder)b).Order = order);
    return builder;
}

//#if false
IApplicationBuilder App = null;
IEndpointRouteBuilder Endpoints = null;

var __AspNet_HostRunAsyncTask = Host.CreateDefaultBuilder()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.Configure(app => {
            App = app.New();
            App.UseRouting();
            App.UseEndpoints(endpoints =>
            {
                Endpoints = endpoints;
            });

            app.Use(next =>
                httpContext =>
                   App.Build()(httpContext));
        });
    }).Build().RunAsync();
//#endif
");

            kernel.DeferCommand(command);

            return kernel;
        }
    }
}

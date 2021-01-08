﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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

class WriteLineHandler : DelegatingHandler
{
    public WriteLineHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($""(HttpClient Request) {request}"");
        if (request.Content != null)
        {
            Console.WriteLine(await request.Content.ReadAsStringAsync());
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine($""(HttpClient Response) {response}"");
        if (response.Content != null)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        return response;
    }
}

var HttpClient = new HttpClient(new WriteLineHandler(new SocketsHttpHandler()));
HttpClient.BaseAddress = new Uri(""http://localhost:5000/"");

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
");

            kernel.DeferCommand(command);

            return kernel;
        }
    }
}

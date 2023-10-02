// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Pocket;

namespace Microsoft.DotNet.Interactive.App;

internal class Startup
{
    public Startup(
        IHostEnvironment env,
        HttpOptions httpOptions)
    {
        Environment = env;
        HttpOptions = httpOptions;

        var configurationBuilder = new ConfigurationBuilder();

        Configuration = configurationBuilder.Build();
    }

    protected IConfigurationRoot Configuration { get; }

    protected IHostEnvironment Environment { get; }

    public HttpOptions HttpOptions { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        using var _ = Logger.Log.OnEnterAndExit();

        if (HttpOptions.EnableHttpApi)
        {
            services.AddDotnetInteractiveHttpApi();
        }
    }

    public void Configure(
        IApplicationBuilder app,
        IHostApplicationLifetime lifetime,
        IServiceProvider serviceProvider)
    {
        var operation = Logger.Log.OnEnterAndExit();
        if (HttpOptions.EnableHttpApi)
        {
            operation.Info("configuring routing");
            app.UseDotNetInteractiveHttpApi(
                serviceProvider.GetRequiredService<Kernel>(), 
                typeof(Program).Assembly,
                serviceProvider.GetRequiredService<HttpProbingSettings>(),
                HttpOptions.HttpPort);
        }
    }
}
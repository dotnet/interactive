// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class InteractiveHost : IAsyncDisposable
    {
        private IHost _host;
        private Startup _startup;

        public string Address { get; private set; }
        public IApplicationBuilder App => _startup?.App;
        public IEndpointRouteBuilder Endpoints => _startup?.Endpoints;

        public async Task StartAsync()
        {
            if (_host is {})
            {
                throw new InvalidOperationException("Already started.");
            }

            Environment.SetEnvironmentVariable($"ASPNETCORE_{WebHostDefaults.PreventHostingStartupKey}", "true");

            _startup = new Startup();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .ConfigureKestrel(kestrelOptions => kestrelOptions.Listen(IPAddress.Loopback, 0))
                    .UseStartup(_ => _startup))
                .ConfigureLogging(loggingBuilder => loggingBuilder
                    .SetMinimumLevel(LogLevel.Trace)
                    .ClearProviders()
                    .AddProvider(new InteractiveLoggerProvider()));

            _host = hostBuilder.Build();
            await _host.StartAsync();

            var kestrelServer = _host.Services.GetRequiredService<IServer>();
            Address = kestrelServer.Features.Get<IServerAddressesFeature>().Addresses.First();
        }

        public ValueTask DisposeAsync()
        {
            if (_host is IAsyncDisposable asyncDisposableHost)
            {
                return asyncDisposableHost.DisposeAsync();
            }

            _host.Dispose();
            return default;
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.TestHost;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.Extensions.DependencyInjection;
using CommandLineParser = Microsoft.DotNet.Interactive.App.CommandLine.CommandLineParser;

namespace Microsoft.DotNet.Interactive.App.Tests;

internal class InProcessTestServer : IDisposable
{
    private Lazy<TestServer> _host;
    private readonly ServiceCollection _serviceCollection = new();

    public static async Task<InProcessTestServer> StartServer(string args, Action<IServiceCollection> servicesSetup = null)
    {
        var server = new InProcessTestServer();

        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var rootCommand = CommandLineParser.Create(
            server._serviceCollection,
            startupOptions =>
            {
                servicesSetup?.Invoke(server._serviceCollection);
                var builder = Program.ConstructWebHostBuilder(
                    startupOptions,
                    server._serviceCollection);

                server._host = new Lazy<TestServer>(() => new TestServer(builder));
                completionSource.SetResult(true);
            });

        await rootCommand.Parse(args)
                         .InvokeAsync(new()
                         {
                             Output = new StringWriter(), 
                             Error = new StringWriter()
                         });

        await completionSource.Task;
        return server;
    }

    private InProcessTestServer()
    {
    }

    public FrontendEnvironment FrontendEnvironment => _host.Value.Services.GetRequiredService<Kernel>().FrontendEnvironment;

    public HttpClient HttpClient => _host.Value.CreateClient();

    public Kernel Kernel => _host.Value.Services.GetService<Kernel>();

    public void Dispose()
    {
        KernelCommandEnvelope.RegisterDefaults();
        KernelEventEnvelope.RegisterDefaults();
        Kernel?.Dispose();
        _host.Value.Dispose();
        _host = null;
    }
}
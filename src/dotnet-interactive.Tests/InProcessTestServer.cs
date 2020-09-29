// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.TestHost;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.Extensions.DependencyInjection;

using Pocket;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal class InProcessTestServer : IDisposable
    {
        private Lazy<TestServer> _host;
        private readonly ServiceCollection _serviceCollection = new ServiceCollection();

        public static async  Task<InProcessTestServer> StartServer(string args, Action<IServiceCollection> servicesSetup = null)
        {
            var server = new InProcessTestServer();

            var completionSource = new TaskCompletionSource<bool>();
            var parser = CommandLineParser.Create(
                server._serviceCollection,
                (startupOptions, invocationContext) =>
                {
                    servicesSetup?.Invoke(server._serviceCollection);
                    var builder = Program.ConstructWebHostBuilder(
                        startupOptions,
                        server._serviceCollection);

                    server._host = new Lazy<TestServer>(() => new TestServer(builder));
                    completionSource.SetResult(true);
                });

            await parser.InvokeAsync(args, new TestConsole());
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
            _host.Value.Dispose();
            _host = null;
        }
    }
}
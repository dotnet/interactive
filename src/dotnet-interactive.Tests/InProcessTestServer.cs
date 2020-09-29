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
        private TestServer _host;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ServiceCollection _serviceCollection = new ServiceCollection();

        public static Task<InProcessTestServer> StartServer(string args, Action<IServiceCollection> servicesSetup = null)
        {
            var server = new InProcessTestServer();

            var completionSource = new TaskCompletionSource<InProcessTestServer>();
            var parser = CommandLineParser.Create(
                server._serviceCollection,
                (startupOptions, invocationContext) =>
                {
                    servicesSetup?.Invoke(server._serviceCollection);
                    var builder = Program.ConstructWebHostBuilder(
                        startupOptions,
                        server._serviceCollection);

                    completionSource.SetResult(server);
                    server._host = new TestServer(builder);
                   
                });

            parser.Invoke(args, server.Console);



            return completionSource.Task;
        }

        private InProcessTestServer()
        {
        }
        public FrontendEnvironment FrontendEnvironment => _host.Services.GetRequiredService<Kernel>().FrontendEnvironment;

        public IConsole Console { get; } = new TestConsole();

        public HttpClient HttpClient => _host.CreateClient();

        public Kernel Kernel => _host.Services.GetService<Kernel>();

        public void Dispose()
        {
            _disposables.Dispose();
            _host.Dispose();
        }
    }
}
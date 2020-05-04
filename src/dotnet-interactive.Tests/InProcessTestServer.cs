// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
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

        public static InProcessTestServer StartServer(string args)
        {
            var server = new InProcessTestServer();

            IWebHostBuilder builder = null;

            var parser = CommandLineParser.Create(
                server._serviceCollection,
                (startupOptions, invocationContext) =>
                {
                    builder = Program.ConstructWebHostBuilder(
                        startupOptions, 
                        server._serviceCollection);
                });

            parser.Invoke(args, server.Console);

            server._host = new TestServer(builder);
            server.Kernel = server._host.Services.GetRequiredService<IKernel>();
            server.FrontendEnvironment = server._host.Services.GetRequiredService<BrowserFrontendEnvironment>();
            return server;
        }

        private InProcessTestServer()
        {
        }
        public BrowserFrontendEnvironment FrontendEnvironment { get; private set; }

        public IConsole Console { get; } = new TestConsole();

        public HttpClient HttpClient => _host.CreateClient();

        public IKernel Kernel { get; private set; }

        public void Dispose()
        {
            _disposables.Dispose();
            _host.Dispose();
        }
    }
}
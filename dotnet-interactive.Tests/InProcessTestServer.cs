// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Pocket;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal class InProcessTestServer<TStartup> : IDisposable
        where TStartup : class
    {
        private TestServer _host;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ServiceCollection _serviceCollection = new ServiceCollection();

        public static async Task<InProcessTestServer<TStartup>> StartServer(string args)
        {
            var server = new InProcessTestServer<TStartup>();
            await server.StartServerInner(args);
            return server;
        }

        public HttpClient Client => _host.CreateClient();

        public IKernel Kernel { get; private set; }

        private InProcessTestServer()
        {
            
        }


        private async Task StartServerInner(string args)
        {
         
            IWebHostBuilder builder = null;
            await CommandLineParser.Create(_serviceCollection,
                startServer: (startupOptions, invocationContext) =>
                {
                    builder = Program.ConstructWebHostBuilder(startupOptions, _serviceCollection);
                }).InvokeAsync(args);

   
            
            _host = new TestServer(builder);
            Kernel = _host.Services.GetRequiredService<IKernel>();

        }

      
        public void Dispose()
        {
            _disposables.Dispose();
            _host.Dispose();
        }

     
    }
}
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

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal class InProcessTestServer<TStartup> : IDisposable
        where TStartup : class
    {
        private TestServer _host;
        private readonly IDisposable _extraDisposable;
        private readonly ServiceCollection _serviceCollection = new ServiceCollection();

        public static async Task<InProcessTestServer<TStartup>> StartServer(string args, IDisposable disposable = null)
        {
            var server = new InProcessTestServer<TStartup>( disposable);
            await server.StartServerInner(args);
            return server;
        }

        public HttpClient Client => _host.CreateClient();

        public IKernel Kernel { get; private set; }

        private InProcessTestServer() : this(null)
        {
            
        }

        private InProcessTestServer(IDisposable disposable)
        {
            _extraDisposable = disposable;

            
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
            _extraDisposable?.Dispose();
            _host.Dispose();
        }

     
    }
}
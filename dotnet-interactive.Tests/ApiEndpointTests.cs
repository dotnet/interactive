// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class InProcessTestServer<TStartup> : IDisposable
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

    public class FunctionalTestBase  
    {

        public FunctionalTestBase()
        {
            // Suppress errors globally here
        }

        public Task<InProcessTestServer<T>> StartServer<T>() where T : class
        {
          
            return InProcessTestServer<T>.StartServer("http --default-kernel csharp");
        }
    }
    public class SignalRTests : FunctionalTestBase
    {

        [Fact]
        public async Task can_get_variable_from_kernel ()
        {
            using var server = await InProcessTestServer<Startup>.StartServer("http --default-kernel csharp");
            var kernel = server.Kernel;
            var client = server.Client;

            await kernel.SendAsync(new SubmitCode("var a = 123;", "csharp"));

            var response = await client.GetAsync("/variables/csharp/a");

            var responseContent = await response.Content.ReadAsStringAsync();

            var value = JToken.Parse(responseContent).Value<int>();

            // read
            value.Should().Be(123);

        }
    }
}

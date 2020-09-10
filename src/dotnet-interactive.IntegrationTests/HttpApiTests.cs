// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.IntegrationTests.Utility;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    public class HttpApiTests
    {
        [IntegrationFact]
        public async Task kernel_servers_javascript_api()
        {
            var port = GetFreePort();
            using var listenerReady = new ManualResetEvent(false);
            var kernelServerProcess = ProcessHelper.Start(command:
                "dotnet",
                args: $@"interactive http --default-kernel csharp --http-port {port}",
                new DirectoryInfo(Directory.GetCurrentDirectory()),
                output: line =>
                {
                    if (line == "Application started. Press Ctrl+C to shut down.")
                    {
                        listenerReady.Set();
                    }
                });

            listenerReady.WaitOne(timeout: TimeSpan.FromSeconds(20));

            using var client = new HttpClient();

            var response = await client.GetAsync($"http://localhost:{port}/resources/dotnet-interactive.js");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Content.Headers.ContentType.MediaType.Should().Be("application/javascript");

            // kill
            kernelServerProcess.Kill();
            kernelServerProcess.WaitForExit(2000).Should().BeTrue();
        }

        private static int GetFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
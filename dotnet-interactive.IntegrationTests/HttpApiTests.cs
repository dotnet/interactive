// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
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
            var addressFilter = new Regex(@"(.*)(Now\slistening\son:)(.*)(?<address>http(s?)://\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d+)");
            var listeningAddress = string.Empty;
            var port = GetFreePort();
            using var listenerStarted = new ManualResetEvent(false);
            var kernelServerProcess = ProcessHelper.Start(command:
                "dotnet",
                args: $@"interactive http --default-kernel csharp --http-port {port}",
                new DirectoryInfo(Directory.GetCurrentDirectory()),
                output: line =>
                {
                    var matches = addressFilter.Match(line);
                    if (matches.Success)
                    {
                        listeningAddress = matches.Groups["address"].Value;
                        listenerStarted.Set();
                    }

                });

            listenerStarted.WaitOne(timeout: TimeSpan.FromSeconds(20));

            using var client = new HttpClient();

            listeningAddress.Should().NotBeNullOrWhiteSpace();

            var response = await client.GetAsync($"{listeningAddress}/resources/dotnet-interactive.js");

            await response.ShouldSucceed();

            response.Content.Headers.ContentType.MediaType.Should().Be("application/javascript");

            // kill
            kernelServerProcess.StandardInput.Close(); // simulate Ctrl+C
            await Task.Delay(TimeSpan.FromSeconds(2)); // allow logs to be flushed
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
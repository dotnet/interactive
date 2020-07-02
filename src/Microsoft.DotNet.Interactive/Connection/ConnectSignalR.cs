// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectSignalR : ConnectKernelCommand<SignalRConnectionOptions>
    {
        public ConnectSignalR() : base("signalr", "Connects to a kernel using signal R")
        {
            AddOption(new Option<string>("--hub-url", "The URL of the SignalR hub"));
        }

        public override async Task<Kernel> CreateKernelAsync(
            SignalRConnectionOptions options,
            KernelInvocationContext context)
        {
            var connection = new HubConnectionBuilder()
                             .WithUrl(options.HubUrl)
                             .Build();

            await connection.StartAsync();

            var client = connection.CreateKernelClient();
            var proxyKernel = new ProxyKernel(options.KernelName, client);
            await connection.SendAsync("connect");

            proxyKernel.RegisterForDisposal(client);
            proxyKernel.RegisterForDisposal(async () =>
            {
                await connection.DisposeAsync();
            });

            return proxyKernel;
        }
    }
}
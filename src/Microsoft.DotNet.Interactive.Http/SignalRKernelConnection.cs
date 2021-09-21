// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;

#nullable enable
namespace Microsoft.DotNet.Interactive.Http
{
    public class SignalRKernelConnection : KernelConnection
    {
        public string? HubUrl { get; set; }

        public override async Task<Kernel> ConnectKernelAsync()
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(HubUrl)
                .Build();

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("connect");

            var receiver = new KernelCommandAndEventSignalRHubConnectionReceiver(hubConnection);
            var sender = new KernelCommandAndEventSignalRHubConnectionSender(hubConnection);
            var proxyKernel = new ProxyKernel(KernelName, receiver, sender);

            var _ = proxyKernel.StartAsync();

            proxyKernel.RegisterForDisposal(receiver);
            proxyKernel.RegisterForDisposal(async () =>
            {
                await hubConnection.DisposeAsync();
            });

            return proxyKernel;
        }
    }
}
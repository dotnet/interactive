// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Http
{
    public class ConnectSignalR : ConnectKernelCommand<SignalRKernelConnection>
    {
        public ConnectSignalR() : base("signalr", "Connects to a kernel using SignalR")
        {
            AddOption(new Option<string>("--hub-url", "The URL of the SignalR hub"));
        }

        public override Task<Kernel> ConnectKernelAsync(
            SignalRKernelConnection kernelConnection,
            KernelInvocationContext context)
        {
            return kernelConnection.ConnectKernelAsync();
        }
    }
}
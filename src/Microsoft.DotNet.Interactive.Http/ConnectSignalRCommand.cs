// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.Http
{
    public class ConnectSignalRCommand : ConnectKernelCommand<SignalRKernelConnector>
    {
        public ConnectSignalRCommand() : base("signalr", "Connects to a kernel using SignalR")
        {
            AddOption(new Option<string>("--hub-url", "The URL of the SignalR hub"));
        }

        public override Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo, SignalRKernelConnector kernelConnector,
            KernelInvocationContext context)
        {
            return kernelConnector.ConnectKernelAsync(kernelInfo);
        }
    }
}
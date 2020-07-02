// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;

#nullable enable

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectNamedPipe : ConnectKernelCommand<NamedPipeConnectionOptions>
    {
        public ConnectNamedPipe() : base("named-pipe",
                                         "Connects to a kernel using named pipes")
        {
            AddOption(new Option<string>("--pipe-name", "The name of the named pipe"));
        }

        public override async Task<Kernel> CreateKernelAsync(NamedPipeConnectionOptions options, KernelInvocationContext context)
        {
            var clientStream = new NamedPipeClientStream(
                ".",
                options.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

            await clientStream.ConnectAsync();
            clientStream.ReadMode = PipeTransmissionMode.Message;
            var client = clientStream.CreateKernelClient();
            var proxyKernel = new ProxyKernel(options.KernelName, client);

            proxyKernel.RegisterForDisposal(client);
            return proxyKernel;
        }
    }

    public class NamedPipeConnectionOptions : KernelConnectionOptions
    {
        public string? PipeName { get; set; }
    }
}
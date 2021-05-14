// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;

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


            var proxyKernel = CreateProxyKernel2(options, clientStream);

            return proxyKernel;
        }

        private static ProxyKernel2 CreateProxyKernel2(NamedPipeConnectionOptions options, NamedPipeClientStream clientStream)
        {
            var receiver = new KernelCommandAndEventTextStreamReceiver(new StreamReader(clientStream));

            var sender = new KernelCommandAndEventTextStreamSender(new StreamWriter(clientStream));
            var proxyKernel = new ProxyKernel2(options.KernelName, receiver, sender);

            var _ = proxyKernel.RunAsync();
            return proxyKernel;
        }

        private static ProxyKernel CreateProxyKernel(NamedPipeConnectionOptions options, NamedPipeClientStream clientStream)
        {
            var client = clientStream.CreateKernelClient();
            var proxyKernel = new ProxyKernel(options.KernelName, client);
            return proxyKernel;
        }
    }
}
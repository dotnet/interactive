// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.DotNet.Interactive.Connection
{
    public class NamedPipeConnection : KernelConnection
    {
        public string? PipeName { get; set; }
        public override async Task<Kernel> ConnectKernelAsync()
        {
            if (string.IsNullOrWhiteSpace(PipeName))
            {
                throw new InvalidComObjectException($"{nameof(PipeName)} cannot be null or whitespaces.");
            }

            var clientStream = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

            await clientStream.ConnectAsync();
            clientStream.ReadMode = PipeTransmissionMode.Message;


            var proxyKernel = CreateProxyKernel(clientStream);

            return proxyKernel;
        }

        private ProxyKernel CreateProxyKernel(PipeStream clientStream)
        {
            var receiver = new KernelCommandAndEventPipeStreamReceiver(clientStream);

            var sender = new KernelCommandAndEventPipeStreamSender(clientStream);
            var proxyKernel = new ProxyKernel(KernelName, receiver, sender);

            var _ = proxyKernel.RunAsync();
            return proxyKernel;
        }
    }
}
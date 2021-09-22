// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.DotNet.Interactive.Connection
{
    public class NamedPipeKernelConnector : KernelConnector
    {
        public string PipeName { get; }
        public override async Task<Kernel> ConnectKernelAsync()
        {
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

            var _ = proxyKernel.StartAsync();
            return proxyKernel;
        }

        public NamedPipeKernelConnector(string kernelName, string pipeName) : base(kernelName)
        {
            PipeName = pipeName;
        }
    }
}
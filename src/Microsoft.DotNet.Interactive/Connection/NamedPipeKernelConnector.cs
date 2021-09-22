// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents;

#nullable enable

namespace Microsoft.DotNet.Interactive.Connection
{
    public class NamedPipeKernelConnector : KernelConnector
    {
        private MultiplexingKernelCommandAndEventReceiver? _receiver;
        private KernelCommandAndEventPipeStreamSender? _sender;

        public string PipeName { get; }
        public override async Task<Kernel> ConnectKernelAsync(KernelName kernelName)
        {
            ProxyKernel? proxyKernel;

            if (_receiver is not null)
            {
                proxyKernel = new ProxyKernel(kernelName.Name,_receiver.CreateChildReceiver(), _sender);
            }
            else
            {
                var clientStream = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

                await clientStream.ConnectAsync();
                clientStream.ReadMode = PipeTransmissionMode.Message;

                _receiver = new MultiplexingKernelCommandAndEventReceiver(new KernelCommandAndEventPipeStreamReceiver(clientStream));
                _sender = new KernelCommandAndEventPipeStreamSender(clientStream);

        
                proxyKernel = new ProxyKernel(kernelName.Name, _receiver, _sender);
            }

            var _ = proxyKernel.StartAsync();
            return proxyKernel; ;
        }
        
        public NamedPipeKernelConnector(string pipeName)
        {
            PipeName = pipeName;
        }
    }
}
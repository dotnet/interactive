// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive
{
    public class KernelHost 
    {
        
        public KernelConnector DefaultConnector { get; }

        public KernelHost(KernelConnector defaultConnector)
        {
            DefaultConnector = defaultConnector;
        }

        public KernelHost(IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver): this(new DefaultKernelConnector(defaultSender, defaultReceiver))
        {
            
        }

        private class DefaultKernelConnector : KernelConnector
        {
            private readonly IKernelCommandAndEventSender _defaultSender;
            private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;

            public DefaultKernelConnector(IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
            {
                _defaultSender = defaultSender;
                _defaultReceiver = defaultReceiver;
            }

            public override Task<Kernel> ConnectKernelAsync(KernelName kernelName)
            {
                var proxy = new ProxyKernel(kernelName.Name, _defaultReceiver.CreateChildReceiver(), _defaultSender);
                var _ = proxy.StartAsync();
                return Task.FromResult((Kernel)proxy);
            }
        }

        public static void  ConfigureAndStart(Kernel kernel, IKernelCommandAndEventSender sender, MultiplexingKernelCommandAndEventReceiver receiver){
            
            var host = new KernelHost(sender, receiver);

            kernel.SetHost(host);

            var _ = receiver.ConnectAsync(kernel);
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
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

       
    }

    public static class KernelHostExtensions
    {
        public static Task ConfigureAndStartHostAsync
            (this Kernel kernel, IKernelCommandAndEventSender sender, MultiplexingKernelCommandAndEventReceiver receiver)
        {

            var host = new KernelHost(sender, receiver);

            kernel.SetHost(host);

            return receiver.ConnectAsync(kernel);
        }

        public static Task ConfigureAndStartHostAsync(this Kernel kernel, TextWriter writer, TextReader reader)
        {
            var sender = new KernelCommandAndEventTextStreamSender(writer);
            var receiver = new MultiplexingKernelCommandAndEventReceiver(new KernelCommandAndEventTextReceiver(reader));

            return ConfigureAndStartHostAsync(kernel, sender, receiver);
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectClientKernel : ConnectKernelCommand<KernelConnectionOptions>
    {
        private readonly KernelClientBase _clientSideKernelClient;

        public ConnectClientKernel(KernelClientBase clientSideKernelClient)
            : base("client", "Connects to a client-side kernel")
        {
            _clientSideKernelClient = clientSideKernelClient;
        }

        public override Task<Kernel> CreateKernelAsync(
            KernelConnectionOptions options,
            KernelInvocationContext context)
        {
            Kernel proxyKernel = new IndiscriminateProxyKernel(options.KernelName, _clientSideKernelClient);

            return Task.FromResult(proxyKernel);
        }
    }
}
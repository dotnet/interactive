// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class NamedPipeTransport
    {
        public static KernelServer CreateServer(Kernel kernel, NamedPipeServerStream local)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }
            var input = new PipeStreamInputStream(local);
            var output = new PipeStreamOutputStream(local);
            var kernelServer = new KernelServer(kernel, input, output);
            kernel.RegisterForDisposal(kernelServer);
            return kernelServer;
        }

        public static KernelClient CreateClient(NamedPipeClientStream remote)
        {
            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            var input = new PipeStreamInputStream(remote);
            var output = new PipeStreamOutputStream(remote);
            var kernelClient = new KernelClient(input, output);
            return kernelClient;
        }
    }
}
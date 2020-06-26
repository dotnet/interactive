using System;
using System.IO.Pipes;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class NamedPipeTransport
    {
        public static KernelServer CreateServer(Kernel kernel, NamedPipeServerStream server)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }
            var input = new PipeStreamInputStream(server);
            var output = new PipeStreamOutputStream(server);
            var kernelServer = new KernelServer(kernel, input, output);
            kernel.RegisterForDisposal(kernelServer);
            return kernelServer;
        }

        public static KernelClient CreateClient(Kernel kernel, NamedPipeClientStream remote)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            var input = new PipeStreamInputStream(remote);
            var output = new PipeStreamOutputStream(remote);
            var kernelClient = new KernelClient(input, output);
            kernel.RegisterForDisposal(kernelClient);
            return kernelClient;
        }
    }
}
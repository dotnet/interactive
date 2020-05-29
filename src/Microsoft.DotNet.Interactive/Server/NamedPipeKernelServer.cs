// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Server
{
    public class NamedPipeKernelServer : StandardIOKernelServer
    {
        private readonly NamedPipeServerStream _serverStream;

        private NamedPipeKernelServer(
            IKernel kernel, 
            NamedPipeServerStream serverStream) : base(kernel, new StreamReader(serverStream), new StreamWriter(serverStream)) 
        {
            // TODO: Dispose this.
            _serverStream = serverStream;
        }

        public static NamedPipeKernelServer WaitForConnection(IKernel kernel, string pipeName)
        {
            var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            serverStream.WaitForConnection();
            return new NamedPipeKernelServer(kernel, serverStream);
        }
    }
}

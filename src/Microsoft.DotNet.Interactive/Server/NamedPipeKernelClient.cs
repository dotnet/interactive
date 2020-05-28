// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Server
{
    public class NamedPipeKernelClient : StandardIOKernelServer
    {
        private readonly NamedPipeClientStream _clientStream;

        private NamedPipeKernelClient(
            IKernel kernel,
            NamedPipeClientStream clientStream) : base(kernel, new StreamReader(clientStream), new StreamWriter(clientStream))
        {
            _clientStream = clientStream;
        }

        public NamedPipeKernelClient(
            IKernel kernel,
            string pipeName) : this(kernel, new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
        {
        }

        public Task ConnectAsync()
        {
            return _clientStream.ConnectAsync();
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO.Pipes;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class ConnectableKernel
    {
        public static  KernelServer CreateKernelServer(this Kernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            var input = new TextReaderInputStream(Console.In);
            var output = new TextWriterOutputStream(Console.Out);
            var kernelServer = new KernelServer(kernel, input, output);

            kernel.RegisterForDisposal(kernelServer);
            return kernelServer;
        }

        public static KernelClient CreateKernelClient(this Process remote)
        {
            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            if (!remote.StartInfo.RedirectStandardInput || !remote.StartInfo.RedirectStandardOutput)
            {
                throw new InvalidOperationException("StandardInput and StandardOutput must be redirected");
            }

            var input = new TextReaderInputStream(remote.StandardOutput);
            var output = new TextWriterOutputStream(remote.StandardInput);
            var kernelClient = new KernelClient(input, output);

            return kernelClient;
        }

        public static KernelServer CreateKernelServer(this Kernel kernel, NamedPipeServerStream local)
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

        public static T EnableApiOverNamedPipe<T>(this T kernel, string pipeName) where T : Kernel
        {
            var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            serverStream.WaitForConnection();
            CreateKernelServer(kernel, serverStream);
            return kernel;
        }

        public static KernelClient CreateKernelClient(this NamedPipeClientStream remote)
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
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class ConnectableKernel
    {
        public static IKernelCommandAndEventReceiver CreateStdInCommandAndEventReceiver()
        {
            Console.InputEncoding = Encoding.UTF8;
            return new KernelCommandAndEventTextReceiver(Console.In);
        }

        public static IKernelCommandAndEventSender CreateStdOutCommandAndEventSender()
        {
            Console.OutputEncoding = Encoding.UTF8;
            return new KernelCommandAndEventTextStreamSender(Console.Out);
        }

        public static  KernelServer CreateKernelServer(this Kernel kernel, DirectoryInfo workingDir)
        {
            return kernel.CreateKernelServer(CreateStdInCommandAndEventReceiver(), CreateStdOutCommandAndEventSender(), workingDir);
        }

        public static KernelServer CreateKernelServer(this Kernel kernel, IKernelCommandAndEventReceiver receiver, IKernelCommandAndEventSender sender, DirectoryInfo workingDir)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }
            
            var kernelServer = new KernelServer(kernel, receiver, sender, workingDir);

            kernel.RegisterForDisposal(kernelServer);
            return kernelServer;
        }

        public static T UseNamedPipeKernelServer<T>(this T kernel, string pipeName, DirectoryInfo workingDir) where T : Kernel
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(pipeName));
            }

            var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Task.Run(() =>
            {
                serverStream.WaitForConnection();
                var kernelCommandAndEventPipeStreamReceiver = new KernelCommandAndEventPipeStreamReceiver(serverStream);
                var kernelCommandAndEventPipeStreamSender = new KernelCommandAndEventPipeStreamSender(serverStream);
                var kernelServer = new KernelServer(kernel, kernelCommandAndEventPipeStreamReceiver,
                    kernelCommandAndEventPipeStreamSender, workingDir);
                var _ = kernelServer.RunAsync();
                kernel.RegisterForDisposal(kernelServer);
                kernel.RegisterForDisposal(serverStream);
            });
            return kernel;
        }
    }
}
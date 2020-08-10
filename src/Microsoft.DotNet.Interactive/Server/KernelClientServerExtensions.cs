﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class ConnectableKernel
    {
        public static  KernelServer CreateKernelServer(this Kernel kernel, DirectoryInfo workingDir)
        {
            return kernel.CreateKernelServer(Console.In, Console.Out, workingDir);
        }

        public static KernelServer CreateKernelServer(this Kernel kernel, TextReader inputStream, TextWriter outputStream, DirectoryInfo workingDir)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            var input = new TextReaderInputStream(inputStream);
            var output = new TextWriterOutputStream(outputStream);
            var kernelServer = new KernelServer(kernel, input, output, workingDir);

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

        public static T UseNamedPipeKernelServer<T>(this T kernel, string pipeName, DirectoryInfo workingDir) where T : Kernel
        {
            if (kernel == null)
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
                var input = new PipeStreamInputStream(serverStream);
                var output = new PipeStreamOutputStream(serverStream);
                var kernelServer = new KernelServer(kernel, input, output, workingDir);
                kernel.RegisterForDisposal(kernelServer);
            });
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

        public static KernelClient CreateKernelClient(this HubConnection hubConnection)
        {
            if (hubConnection == null) throw new ArgumentNullException(nameof(hubConnection));

            var input = new SignalRInputTextStream(hubConnection);
            var output = new SignalROutputTextStream(hubConnection);
            var kernelClient = new KernelClient(input, output);
            return kernelClient;
        }
    }
}
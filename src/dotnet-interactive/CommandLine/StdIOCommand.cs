// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;
using Pocket;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public static class StdIOCommand
    {
        public static async Task<int> Do(StartupOptions startupOptions, KernelBase kernel, IConsole console)
        {
            var disposable = Program.StartToolLogging(startupOptions);
            var server = CreateServer(kernel, console);
            kernel.RegisterForDisposal(disposable);
            await server.Input.LastAsync();
            return 0;
        }

        internal static StandardIOKernelServer CreateServer(KernelBase kernel, IConsole console)
        {
            var server = new StandardIOKernelServer(
                kernel,
                Console.In,
                Console.Out);

            if (kernel is KernelBase kernelBase)
            {
                kernelBase.RegisterForDisposal(server);
            }

            return server;
        }

        public static async Task<int> DoNamedPipeServer(StartupOptions startupOptions, KernelBase kernel, string pipeName)
        {
            Console.WriteLine("Starting named pipe server, waiting for connection...");
            var disposable = Program.StartToolLogging(startupOptions);
            var server = CreateNamedPipeServer(kernel, pipeName);
            await server.WaitForConnectionAsync();
            Console.WriteLine("Named pipe server received connection!");
            kernel.RegisterForDisposal(disposable);
            await server.Input.LastAsync();
            return 0;
        }

        internal static NamedPipeKernelServer CreateNamedPipeServer(KernelBase kernel, string pipeName)
        {
            var server = new NamedPipeKernelServer(
                kernel,
                pipeName);

            if (kernel is KernelBase kernelBase)
            {
                kernelBase.RegisterForDisposal(server);
            }

            return server;
        }

        public static async Task<int> DoNamedPipeClient(StartupOptions startupOptions, KernelBase kernel, string pipeName)
        {
            Console.WriteLine("Waiting to connect to named pipe...");
            var disposable = Program.StartToolLogging(startupOptions);
            var client = CreateNamedPipeClient(kernel, pipeName);
            await client.ConnectAsync();
            Console.WriteLine("Named pipe connected!");
            kernel.RegisterForDisposal(disposable);
            await client.Input.LastAsync();
            return 0;
        }

        internal static NamedPipeKernelClient CreateNamedPipeClient(KernelBase kernel, string pipeName)
        {
            var client = new NamedPipeKernelClient(
                kernel,
                pipeName);

            if (kernel is KernelBase kernelBase)
            {
                kernelBase.RegisterForDisposal(client);
            }

            return client;
        }
    }
}
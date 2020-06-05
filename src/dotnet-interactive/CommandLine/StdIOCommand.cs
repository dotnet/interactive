// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;

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
    }
}
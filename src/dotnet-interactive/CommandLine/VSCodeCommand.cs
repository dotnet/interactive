// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    internal static class VSCodeCommand
    {
        public static async Task<int> Do(StartupOptions startupOptions, KernelServer kernelServer, IConsole console)
        {
            var disposable = Program.StartToolLogging(startupOptions);
            var run = kernelServer.RunAsync();
            kernelServer.NotifyIsReady();
            await run;
            return 0;
        }

        public static async Task<int> Do(StartupOptions startupOptions, Kernel kernel, IKernelCommandAndEventSender sender, IKernelCommandAndEventReceiver receiver, IConsole console)
        {
            var disposable = Program.StartToolLogging(startupOptions);
            var eventsSubs = kernel.KernelEvents.Subscribe(e =>
            {
                var _ = sender.SendAsync(e, CancellationToken.None);
            });
            var cancellationTokenSource = new CancellationTokenSource();
            kernel.RegisterForDisposal(disposable);
            kernel.RegisterForDisposal(eventsSubs);
            kernel.RegisterForDisposal(cancellationTokenSource);
            var run = Task.Run(async () =>
            {
                await foreach (var commandOrEvent in receiver.CommandsAndEventsAsync(cancellationTokenSource.Token))
                {
                    if (commandOrEvent.IsParseError)
                    {
                        var _ = sender.SendAsync(commandOrEvent.Event, cancellationTokenSource.Token);
                    }
                    else
                    {
                        await commandOrEvent.DispatchAsync(kernel, cancellationTokenSource.Token);
                    }
                }
            }, cancellationTokenSource.Token);

            await sender.NotifyIsReadyAsync(CancellationToken.None);
            await run;
            return 0;
        }
    }
}

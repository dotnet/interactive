// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class StdIoConnection : KernelConnection
    {
        public string[] Command { get; set; } = Array.Empty<string>();

        public DirectoryInfo? WorkingDirectory { get; set; }

        public bool WaitForKernelReadyEvent { get; set; }

        public override async Task<Kernel> ConnectKernelAsync()
        {
            string command = Command[0];
            string arguments = string.Join(" ", Command.Skip(1));
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = WorkingDirectory?.FullName ?? Environment.CurrentDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var process = new Process { StartInfo = psi };
            process.Start();

            var receiver = new KernelCommandAndEventTextReceiver(process.StandardOutput);
            var sender = new KernelCommandAndEventTextStreamSender(process.StandardInput);

            var kernel = new ProxyKernel(KernelName, receiver, sender);

            kernel.RegisterForDisposal(() =>
            {
                process.Kill();
                process.Dispose();
            });

            var _ = kernel.RunAsync();

            if (WaitForKernelReadyEvent)
            {
                TaskCompletionSource<bool> ready = new();
                var sub = kernel.KernelEvents.OfType<KernelReady>().Subscribe(_ =>
                {
                    ready.SetResult(true);
                });

                await ready.Task;
                sub.Dispose();
            }

            return kernel;
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectStdIO : ConnectKernelCommand<StdIOConnectionOptions>
    {
        public ConnectStdIO() : base("stdio",
                                     "Connects to a kernel using the stdio protocol")
        {
            AddOption(new Option<string[]>("--command", "The command to execute")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = true,
            });
            AddOption(new Option<DirectoryInfo>("--working-directory", () => new DirectoryInfo(Directory.GetCurrentDirectory()), "The working directory"));
            AddOption(new Option<bool>("--wait-for-kernel-ready-event", () => false, "Wait for a kernel ready event before continuing"));
        }

        public override Task<Kernel> CreateKernelAsync(StdIOConnectionOptions options, KernelInvocationContext context)
        {
            return CreateStdioKernelAsync(options.KernelName, options.Command[0], string.Join(" ", options.Command.Skip(1)), options.WorkingDirectory, options.WaitForKernelReadyEvent);
        }

        public static async Task<Kernel> CreateStdioKernelAsync(string kernelName, string command, string arguments, DirectoryInfo workingDirectory, bool waitForKernelReadyEvent = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory.FullName,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var process = new Process { StartInfo = psi };
            process.Start();

            var receiver = new KernelCommandAndEventTextReceiver(process.StandardOutput);
            var sender = new KernelCommandAndEventTextStreamSender(process.StandardInput);

            var kernel = new ProxyKernel(kernelName, receiver, sender);

            kernel.RegisterForDisposal(() =>
            {
                process.Kill();
                process.Dispose();
            });

            var _ = kernel.RunAsync();

            if (waitForKernelReadyEvent)
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

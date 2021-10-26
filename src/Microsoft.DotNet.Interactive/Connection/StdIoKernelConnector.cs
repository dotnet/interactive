// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Connection
{
   public class StdIoKernelConnector : IKernelConnector
    {
        public string[] Command { get; }

        public DirectoryInfo WorkingDirectory { get; }

        public async Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
        {
            var command = Command[0];
            var arguments = string.Join(" ", Command.Skip(1));
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = WorkingDirectory.FullName,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            var process = new Process { StartInfo = psi };
            process.EnableRaisingEvents = true;
            var stdErr = new StringBuilder();
            process.ErrorDataReceived += (o, args) =>
            {
                stdErr.Append(args.Data);
            };
            await Task.Yield();
            process.Start();
            process.BeginErrorReadLine();
            var receiver = new MultiplexingKernelCommandAndEventReceiver(new KernelCommandAndEventTextReceiver(process.StandardOutput));
            var sender = new KernelCommandAndEventTextStreamSender(process.StandardInput);
            var kernel = new ProxyKernel(kernelInfo.LocalName, receiver, sender);
            kernel.RegisterForDisposal(() =>
            {
                process.Kill();
                process.Dispose();
            });

            var r = receiver.CreateChildReceiver();
            var _ = kernel.StartAsync();

            var checkReady = Task.Run(async () =>
            {
                await foreach (var eoc in r.CommandsAndEventsAsync(CancellationToken.None))
                {
                    if (eoc.Event is KernelReady)
                    {
                        return;
                    }
                }
            });

            var checkProcessError = Task.Run(async () =>
            {
                while (!checkReady.IsCompleted)
                {
                    await Task.Delay(200);
                    if (process.HasExited)
                    {
                        if (process.ExitCode != 0)
                        {
                            throw new CommandLineInvocationException(
                                new CommandLineResult(process.ExitCode, error: stdErr.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
                        }
                    }
                }
            });

            await Task.WhenAny(checkProcessError, checkReady);
            
            return kernel;
        }

        public StdIoKernelConnector(string[] command, DirectoryInfo? workingDirectory = null)
        {
            Command = command;
            WorkingDirectory = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);
        }
    }
}

﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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

namespace Microsoft.DotNet.Interactive.Connection;

public class StdIoKernelConnector : KernelConnectorBase
{
    private MultiplexingKernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventTextStreamSender? _sender;
    private Process? _process;

    public string[] Command { get; }

    public DirectoryInfo WorkingDirectory { get; }

    public override async Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
    {
        if (_receiver is not null)
        {
            var kernel = new ProxyKernel(kernelInfo.LocalName, _receiver.CreateChildReceiver(), _sender);
            var _ = kernel.StartAsync();
            return kernel;
        }
        else
        {
            // QUESTION: (ConnectKernelAsync) tests?
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
                StandardOutputEncoding = Encoding.UTF8
            };
            _process = new Process { StartInfo = psi };
            _process.EnableRaisingEvents = true;
            var stdErr = new StringBuilder();
            _process.ErrorDataReceived += (o, args) => { stdErr.Append(args.Data); };
            await Task.Yield();

            _process.Start();
            _process.BeginErrorReadLine();

            _receiver = new MultiplexingKernelCommandAndEventReceiver(
                new KernelCommandAndEventTextReceiver(_process.StandardOutput));
            _sender = new KernelCommandAndEventTextStreamSender(_process.StandardInput);
            var kernel = new ProxyKernel(kernelInfo.LocalName, _receiver, _sender);
        
            var r = _receiver.CreateChildReceiver();
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
                    if (_process.HasExited)
                    {
                        if (_process.ExitCode != 0)
                        {
                            throw new CommandLineInvocationException(
                                new CommandLineResult(_process.ExitCode,
                                    error: stdErr.ToString().Split(new[] { '\r', '\n' },
                                        StringSplitOptions.RemoveEmptyEntries)));
                        }
                    }
                }
            });

            await Task.WhenAny(checkProcessError, checkReady);
            return kernel;
        }


    }

    public StdIoKernelConnector(string[] command, DirectoryInfo? workingDirectory = null)
    {
        Command = command;
        WorkingDirectory = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);
    }

    public void Dispose()
    {
        _receiver?.Dispose();

        if (_process is not null && _process.HasExited == false)
        {
            // todo: ensure killing process tree
            _process?.Kill(true);
            _process?.Dispose();
            _process = null;
        }
    }
}
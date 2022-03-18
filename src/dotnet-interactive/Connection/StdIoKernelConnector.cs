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
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class StdIoKernelConnector : IKernelConnector, IDisposable
{
    private MultiplexingKernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventTextStreamSender? _sender;
    private Process? _process;
    private Uri _remoteHostUri;

    public StdIoKernelConnector(string[] command, DirectoryInfo? workingDirectory = null)
    {
        Command = command;
        WorkingDirectory = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);
    }

    public string[] Command { get; }

    public DirectoryInfo WorkingDirectory { get; }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        if (_receiver is not null)
        {
            var kernel = new ProxyKernel(
                kernelName, 
                _receiver.CreateChildReceiver(), 
                _sender,
                new Uri(_remoteHostUri, kernelName));
            
            kernel.EnsureStarted();
            
            return kernel;
        }
        else
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
                StandardOutputEncoding = Encoding.UTF8
            };
            _process = new Process { StartInfo = psi };
            _process.EnableRaisingEvents = true;
            var stdErr = new StringBuilder();
            _process.ErrorDataReceived += (o, args) => { stdErr.Append(args.Data); };
            await Task.Yield();

            _process.Start();
            _process.BeginErrorReadLine();
            _remoteHostUri = new Uri($"kernel://pid-{_process.Id}");

            _receiver = new MultiplexingKernelCommandAndEventReceiver(
                new KernelCommandAndEventTextReaderReceiver(_process.StandardOutput));
            _sender = new KernelCommandAndEventTextStreamSender(_process.StandardInput);

            var proxyKernel = new ProxyKernel(kernelName, _receiver, _sender, new Uri(_remoteHostUri, kernelName));
        
            var r = _receiver.CreateChildReceiver();
            
            proxyKernel.EnsureStarted();

            var checkReady = Task.Run(async () =>
            {
                await foreach (var commandOrEvent in r.CommandsAndEventsAsync(CancellationToken.None))
                {
                    if (commandOrEvent.Event is KernelReady)
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

            return proxyKernel;
        }
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
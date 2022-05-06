// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using CompositeDisposable = Pocket.CompositeDisposable;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class StdIoKernelConnector : IKernelConnector, IDisposable
{
    private MultiplexingKernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventTextStreamSender? _sender;
    private Process? _process;
    private Uri? _remoteHostUri;
    private RefCountDisposable? _refCountDisposable = null;

    public StdIoKernelConnector(string[] command, DirectoryInfo? workingDirectory = null)
    {
        Command = command;
        WorkingDirectory = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);
    }

    public string[] Command { get; }

    public DirectoryInfo WorkingDirectory { get; }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        ProxyKernel? proxyKernel;

        if (_receiver is null)
        {
            var command = Command[0];
            var arguments = string.Join(" ", Command.Skip(1));

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = WorkingDirectory.FullName,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            var stdErr = new StringBuilder();
            _process.ErrorDataReceived += (_, args) =>
            {
                stdErr.Append(args.Data);
            };

            await Task.Yield();

            _process.Start();
            _process.BeginErrorReadLine();
            _remoteHostUri = KernelHost.CreateHostUriForProcessId(_process.Id);

            _receiver = new MultiplexingKernelCommandAndEventReceiver(
                new KernelCommandAndEventTextStreamReceiver(_process.StandardOutput));
            _sender = new KernelCommandAndEventTextStreamSender(
                _process.StandardInput,
                _remoteHostUri);

            _refCountDisposable = new RefCountDisposable(new CompositeDisposable
            {
                KillRemoteKernelProcess,
                () => _receiver.Dispose()
            });

            proxyKernel = new ProxyKernel(
                kernelName,
                _sender,
                _receiver, 
                new Uri(_remoteHostUri, kernelName));
            
            proxyKernel.RegisterForDisposal(_refCountDisposable);

            var checkReady = Task.Run(async () =>
            {
                await foreach (var commandOrEvent in _receiver.CommandsAndEventsAsync(CancellationToken.None))
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
                            var stdErrString = stdErr.ToString()
                                                     .Split(new[] { '\r', '\n' },
                                                            StringSplitOptions.RemoveEmptyEntries);

                            throw new CommandLineInvocationException(
                                new CommandLineResult(_process.ExitCode,
                                                      error: stdErrString));
                        }
                    }
                }
            });

            if (await Task.WhenAny(checkProcessError, checkReady) == checkProcessError &&
                checkProcessError.Exception is { } ex)
            {
                throw ex;
            }
        }
        else
        {
            proxyKernel = new ProxyKernel(
                kernelName,
                _sender,
                _receiver.CreateChildReceiver(), new Uri(_remoteHostUri!, kernelName));

            proxyKernel.RegisterForDisposal(_refCountDisposable!.GetDisposable());
        }

        proxyKernel.EnsureStarted();

        return proxyKernel;
    }

    private void KillRemoteKernelProcess()
    {
        if (_process is { HasExited: false })
        {
            // todo: ensure killing process tree
            _process?.Kill(true);
            _process?.Dispose();
            _process = null;
        }
    }

    public void Dispose() => _refCountDisposable?.Dispose();
}
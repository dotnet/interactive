// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using CompositeDisposable = Pocket.CompositeDisposable;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class StdIoKernelConnector : IKernelConnector, IDisposable
{
    private KernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventSender? _sender;
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

            var stdOutObservable = new Subject<string>();
            _process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    stdOutObservable.OnNext(args.Data);
                }
            };
            var stdErr = new StringBuilder();
            _process.ErrorDataReceived += (_, args) =>
            {
                stdErr.Append(args.Data);
            };

            await Task.Yield();

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _remoteHostUri = KernelHost.CreateHostUriForProcessId(_process.Id);

            _receiver = KernelCommandAndEventReceiver.FromObservable(stdOutObservable);

            bool kernelReadyReceived = false;
            _receiver.Select(coe => coe.Event)
                                   .OfType<KernelReady>()
                                   .Take(1)
                                   .Subscribe(e => kernelReadyReceived = true);

            _sender = KernelCommandAndEventSender.FromTextWriter(
                _process.StandardInput,
                _remoteHostUri);

            _refCountDisposable = new RefCountDisposable(new CompositeDisposable
            {
                KillRemoteKernelProcess,
                _receiver.Dispose
            });

            proxyKernel = new ProxyKernel(
                kernelName,
                _sender,
                _receiver,
                new Uri(_remoteHostUri, kernelName));

            proxyKernel.RegisterForDisposal(_refCountDisposable);

            while (!kernelReadyReceived)
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
        }
        else
        {
            proxyKernel = new ProxyKernel(
                kernelName,
                _sender,
                _receiver,
                new Uri(_remoteHostUri!, kernelName));

            proxyKernel.RegisterForDisposal(_refCountDisposable!.GetDisposable());
        }

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
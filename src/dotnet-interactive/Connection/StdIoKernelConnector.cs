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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

using Pocket;

using static Pocket.Logger<Microsoft.DotNet.Interactive.App.Connection.StdIoKernelConnector>;

using CompositeDisposable = Pocket.CompositeDisposable;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class StdIoKernelConnector : IKernelConnector, IDisposable
{
    private KernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventSender? _sender;
    private Process? _process;
    private RefCountDisposable? _refCountDisposable = null;

    public StdIoKernelConnector(string[] command, Uri kernelHostUri, DirectoryInfo? workingDirectory = null)
    {
        Command = command;
        KernelHostUri = kernelHostUri;
        WorkingDirectory = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);
    }

    public string[] Command { get; }

    public DirectoryInfo WorkingDirectory { get; }

    public Uri KernelHostUri { get; }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        ProxyKernel? proxyKernel;

        using var activity = Log.OnEnterAndExit();

        if (_receiver is null)
        {
            var command = Command[0];
            var arguments = Command.Skip(1).ToArray();
            if (KernelHostUri is { })
            {
                arguments = arguments.Concat(new[]
                {
                    "--kernel-host",
                    KernelHostUri.Authority
                }).ToArray();
            }

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", arguments),
                    EnvironmentVariables =
                    {
                        ["DOTNET_INTERACTIVE_SKIP_FIRST_TIME_EXPERIENCE"]  = "1",
                        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"]  = "1",
                        ["DOTNET_DbgEnableMiniDump"] = "0" // https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dumps
                    },
                    WorkingDirectory = WorkingDirectory.FullName,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true,
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

            activity.Info("Process id: {0}", _process.Id);

            _receiver = KernelCommandAndEventReceiver.FromObservable(stdOutObservable);

            bool kernelReadyReceived = false;
            _receiver.Select(coe => coe.Event)
                                   .OfType<KernelReady>()
                                   .Take(1)
                                   .Subscribe(e =>
                                   {
                                       kernelReadyReceived = true;
                                   });

            _sender = KernelCommandAndEventSender.FromTextWriter(
               _process.StandardInput,
               KernelHostUri);

            _refCountDisposable = new RefCountDisposable(new CompositeDisposable
            {
                SendQuitCommand,
                KillRemoteKernelProcess,
                _receiver.Dispose
            });

            proxyKernel = new ProxyKernel(
                kernelName,
                _sender,
                _receiver,
                KernelHostUri);

            proxyKernel.RegisterForDisposal(_refCountDisposable);

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

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
                KernelHostUri);

            proxyKernel.RegisterForDisposal(_refCountDisposable!.GetDisposable());
        }

        return proxyKernel;
    }

    public async Task<ProxyKernel> CreateRootKernelProxyAsync(string localName)
    {
        var kernel = await CreateKernelAsync(localName);

        return (ProxyKernel)kernel;
    }

    private void SendQuitCommand()
    {
        if (_sender is not null)
        {
            var _ = _sender.SendAsync(new Quit(), CancellationToken.None);
        }
    }

    private void KillRemoteKernelProcess()
    {
        if (_process is { HasExited: false })
        {
#if NETSTANDARD2_0
            // TODO: Kill entire process tree.
            _process?.Kill();
#else
            _process?.Kill(entireProcessTree: true);
#endif

            _process?.Dispose();
            _process = null;
        }
    }

    public void Dispose() => _refCountDisposable?.Dispose();
}

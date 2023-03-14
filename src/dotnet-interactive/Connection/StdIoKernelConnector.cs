﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
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

public class StdIoKernelConnector : IKernelConnector
{
    private readonly string[] _command;
    private readonly string _rootProxyKernelLocalName;
    private readonly Uri _kernelHostUri;
    private readonly DirectoryInfo _workingDirectory;

    private readonly Dictionary<string, KernelInfo> _remoteKernelInfos;
    private KernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventSender? _sender;
    private Process? _process;
    private RefCountDisposable? _refCountDisposable;
    private KernelReady? _kernelReady;

    public int? ProcessId => _process?.Id;

    public StdIoKernelConnector(
        string[] command,
        string rootProxyKernelLocalName,
        Uri kernelHostUri,
        DirectoryInfo? workingDirectory = null)
    {
        _command = command;
        _rootProxyKernelLocalName = rootProxyKernelLocalName;
        _kernelHostUri = kernelHostUri;
        _workingDirectory = workingDirectory ?? new DirectoryInfo(Environment.CurrentDirectory);

        _remoteKernelInfos = new Dictionary<string, KernelInfo>();
    }

    /// <remarks>
    /// TODO: Does it even make sense to implement <see cref="IKernelConnector"/> here considering that we have
    /// concepts (such as a single root <see cref="ProxyKernel"/>, and zero or more (child) <see cref="ProxyKernel"/>s)
    /// that the <see cref="IKernelConnector"/> abstraction does not understand / support? Should the '#!connect stdio'
    /// command be removed?
    /// 
    /// The current implementation only supports creating / retrieving the root <see cref="ProxyKernel"/>.
    /// </remarks>
    async Task<Kernel> IKernelConnector.CreateKernelAsync(string kernelName)
        => await CreateRootProxyKernelAsync();

    public async Task<ProxyKernel> CreateRootProxyKernelAsync()
    {
        ProxyKernel rootProxyKernel;

        if (_receiver is null)
        {
            using var activity = Log.OnEnterAndExit();

            var command = _command[0];
            var arguments = _command.Skip(1).ToArray();
            if (_kernelHostUri is { })
            {
                arguments = arguments.Concat(new[]
                {
                    "--kernel-host",
                    _kernelHostUri.Authority
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
                    WorkingDirectory = _workingDirectory.FullName,
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
            _kernelReady = null;
            _receiver.Select(coe => coe.Event)
                                   .OfType<KernelReady>()
                                   .Take(1)
                                   .Subscribe(e =>
                                   {
                                       _kernelReady = e;

                                   });

            _receiver.Select(coe => coe.Event)
                                   .OfType<KernelInfoProduced>()
                                   .Subscribe(e =>
                                   {
                                       var info = e.KernelInfo;
                                       var name = info.LocalName;

                                       lock (_remoteKernelInfos)
                                       {
                                           _remoteKernelInfos[name] = info;
                                       }
                                   });

            _sender = KernelCommandAndEventSender.FromTextWriter(
               _process.StandardInput,
               _kernelHostUri);

            _refCountDisposable = new RefCountDisposable(new CompositeDisposable
            {
                SendQuitCommand,
                KillRemoteKernelProcess,
                _receiver.Dispose
            });

            rootProxyKernel = new ProxyKernel(
                _rootProxyKernelLocalName,
                _sender,
                _receiver,
                _kernelHostUri);

            rootProxyKernel.RegisterForDisposal(_refCountDisposable);

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            while (_kernelReady is null)
            {
                await Task.Delay(20);

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
            rootProxyKernel = new ProxyKernel(
                _rootProxyKernelLocalName,
                _sender,
                _receiver,
                _kernelHostUri);

            rootProxyKernel.RegisterForDisposal(_refCountDisposable);
        }

        if (_kernelReady is { })
        {
            var kernelInfo = _kernelReady.KernelInfos.Single(k => k.Uri == _kernelHostUri);
            rootProxyKernel.UpdateKernelInfo(kernelInfo);
        }

        return rootProxyKernel;
    }

    public async Task<ProxyKernel> CreateProxyKernelAsync(string remoteName, string? localNameOverride = null)
    {
        using var rootProxyKernel = await CreateRootProxyKernelAsync();

        ProxyKernel proxyKernel;

        if (_remoteKernelInfos.TryGetValue(remoteName, out var remoteInfo))
        {
            proxyKernel = await CreateProxyKernelAsync(remoteInfo, localNameOverride);
        }
        else
        {
            var result = await rootProxyKernel.SendAsync(new RequestKernelInfo(remoteName));

            var remoteInfos = result.Events
                .OfType<KernelInfoProduced>()
                .Select(e => e.KernelInfo)
                .Where(info => info.LocalName == remoteName)
                .ToArray();

            if (remoteInfos.Length == 1)
            {
                remoteInfo = remoteInfos[0];
                proxyKernel = await CreateProxyKernelAsync(remoteInfo, localNameOverride);
            }
            else
            {
                var message = $"Found {remoteInfos.Length} remote {nameof(Kernel)}s matching name '{remoteName}'.";
                var failureEvents = result.Events.OfType<CommandFailed>().ToArray();
                var innerException = failureEvents.Length == 1
                    ? failureEvents[0].Exception
                    : new AggregateException(failureEvents.Select(f => f.Exception));

                throw new InvalidOperationException(message, innerException);
            }
        }

        return proxyKernel;
    }

    public async Task<ProxyKernel> CreateProxyKernelAsync(KernelInfo remoteInfo, string? localNameOverride = null)
    {
        using var _ = await CreateRootProxyKernelAsync();

        var localName =
            string.IsNullOrWhiteSpace(localNameOverride) ? remoteInfo.LocalName : localNameOverride;

        var proxyKernel =
            new ProxyKernel(
                localName,
                _sender,
                _receiver,
                remoteInfo.Uri);

        UpdateKernelInfo(proxyKernel, remoteInfo);

        proxyKernel.RegisterForDisposal(_refCountDisposable!.GetDisposable());

        return proxyKernel;
    }

    private static void UpdateKernelInfo(ProxyKernel proxyKernel, KernelInfo remoteInfo)
    {
        proxyKernel.KernelInfo.DisplayName = remoteInfo.DisplayName;
        proxyKernel.KernelInfo.IsComposite = remoteInfo.IsComposite;
        proxyKernel.KernelInfo.LanguageName = remoteInfo.LanguageName;
        proxyKernel.KernelInfo.LanguageVersion = remoteInfo.LanguageVersion;

        foreach (var directive in remoteInfo.SupportedDirectives)
        {
            proxyKernel.KernelInfo.SupportedDirectives.Add(directive);
        }

        foreach (var command in remoteInfo.SupportedKernelCommands)
        {
            proxyKernel.KernelInfo.SupportedKernelCommands.Add(command);
        }
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
}

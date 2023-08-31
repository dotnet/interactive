// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

public class StdIoKernelConnector
{
    private readonly string[] _command;
    private readonly string _rootProxyKernelLocalName;
    private readonly Uri _kernelHostUri;
    private readonly DirectoryInfo _workingDirectory;

    private readonly ConcurrentDictionary<string, KernelInfo> _remoteKernelInfoCache;
    private KernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventSender? _sender;
    private Process? _process;
    private RefCountDisposable? _refCountDisposable;

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

        _remoteKernelInfoCache = new ConcurrentDictionary<string, KernelInfo>();
    }

    public async Task<ProxyKernel> CreateRootProxyKernelAsync()
    {
        ProxyKernel rootProxyKernel;

        if (_receiver is null)
        {
            using var activity = Log.OnEnterAndExit();

            var command = _command[0];
            var arguments = _command.Skip(1).ToArray();
            arguments = arguments.Concat(new[]
            {
                "--kernel-host",
                _kernelHostUri.Authority
            }).ToArray();

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", arguments),
                    EnvironmentVariables =
                    {
                        ["DOTNET_INTERACTIVE_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
                        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
                        ["DOTNET_DbgEnableMiniDump"] = "0", // https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dumps
                        ["DOTNET_CLI_UI_LANGUAGE"] = GetCurrentUICulture(),
                        ["DOTNET_CLI_CULTURE"] = GetCurrentCulture()
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

            KernelReady? kernelReady = null;
            _receiver.Select(coe => coe.Event)
                                   .OfType<KernelReady>()
                                   .Take(1)
                                   .Subscribe(e =>
                                   {
                                       kernelReady = e;
                                       UpdateRemoteKernelInfoCache(kernelReady.KernelInfos);
                                   });

            _receiver.Select(coe => coe.Event)
                                   .OfType<KernelInfoProduced>()
                                   .Subscribe(e =>
                                   {
                                       UpdateRemoteKernelInfoCache(e.KernelInfo);
                                   });

            var writer = new StreamWriter(_process.StandardInput.BaseStream);
            _sender = KernelCommandAndEventSender.FromTextWriter(writer, _kernelHostUri);

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

            while (kernelReady is null)
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

        var remoteRootKernelInfo = GetCachedKernelInfoForRemoteRoot();
        rootProxyKernel.UpdateKernelInfo(remoteRootKernelInfo);
        return rootProxyKernel;
    }

    // Get the current culture from Visual Studio
    private string GetCurrentCulture()
    {
        CultureInfo culture = Thread.CurrentThread.CurrentCulture;
        return culture.Name;
    }

    private string GetCurrentUICulture()
    {
        CultureInfo culture = Thread.CurrentThread.CurrentUICulture;
        return culture.Name;
    }

    private void UpdateRemoteKernelInfoCache(IEnumerable<KernelInfo> infos)
    {
        foreach (var info in infos)
        {
            UpdateRemoteKernelInfoCache(info);
        }
    }

    private void UpdateRemoteKernelInfoCache(KernelInfo info)
    {
        var name = info.LocalName;
        _remoteKernelInfoCache[name] = info;
    }

    private KernelInfo GetCachedKernelInfoForRemoteRoot()
        => _remoteKernelInfoCache.Values.Single(k => k.Uri == _kernelHostUri);

    private bool TryGetCachedKernelInfoByRemoteName(string remoteName, [NotNullWhen(true)] out KernelInfo? remoteInfo)
        => _remoteKernelInfoCache.TryGetValue(remoteName, out remoteInfo);

    public async Task<ProxyKernel> CreateProxyKernelAsync(string remoteName, string? localNameOverride = null)
    {
        using var rootProxyKernel = await CreateRootProxyKernelAsync();

        ProxyKernel proxyKernel;

        if (TryGetCachedKernelInfoByRemoteName(remoteName, out var remoteInfo))
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

        proxyKernel.UpdateKernelInfo(remoteInfo);

        proxyKernel.RegisterForDisposal(_refCountDisposable!.GetDisposable());

        return proxyKernel;
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

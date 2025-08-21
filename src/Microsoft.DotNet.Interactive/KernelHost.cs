// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Pocket;

namespace Microsoft.DotNet.Interactive;

public class KernelHost : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TaskCompletionSource<Unit> _disposed = new();
    private readonly CompositeKernel _kernel;
    private readonly IKernelCommandAndEventSender _defaultSender;
    private readonly IKernelCommandAndEventReceiver _receiver;
    private IDisposable? _kernelEventSubscription;
    private readonly Func<string, Task<ProxyKernel>> _defaultConnector;
    private EventLoopScheduler? _eventLoop;

    internal ConcurrentDictionary<string, KernelInvocationContext> ContextsByRootToken { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    internal KernelHost(
        CompositeKernel kernel,
        IKernelCommandAndEventSender sender,
        IKernelCommandAndEventReceiver receiver,
        Uri hostUri)
    {
        Uri = hostUri ?? throw new ArgumentNullException(nameof(hostUri));
        _kernel = kernel;
        _defaultSender = sender;
        _receiver = receiver;
        _defaultConnector = async name =>
        {
            var connector = new DefaultKernelConnector(
                _defaultSender,
                _receiver);
            return await connector.CreateKernelAsync(name);
        };
        _kernel.SetHost(this);
    }

    private class DefaultKernelConnector
    {
        private readonly IKernelCommandAndEventSender _sender;
        private readonly IKernelCommandAndEventReceiver? _receiver;

        public DefaultKernelConnector(
            IKernelCommandAndEventSender sender,
            IKernelCommandAndEventReceiver receiver)
        {
            _sender = sender;
            _receiver = receiver;
        }

        public Task<ProxyKernel> CreateKernelAsync(string kernelName)
        {
            var proxy = new ProxyKernel(
                kernelName,
                _sender,
                _receiver,
                new Uri(_sender.RemoteHostUri, kernelName));

            return Task.FromResult(proxy);
        }
    }

    public async Task ConnectAsync()
    {
        _eventLoop = new EventLoopScheduler(a => new Thread(a)
        {
            Name = "KernelHost command dispatcher",
            IsBackground = true,
        });

        _kernelEventSubscription = _kernel.KernelEvents.Subscribe(e =>
        {
            if (e is ReturnValueProduced { Value: DisplayedValue })
            {
                return;
            }

            if (e is KernelInfoProduced kernelInfoProduced)
            {
                if (e.Command.DestinationUri is { } destinationUri &&
                    _kernel.ChildKernels.TryGetByUri(destinationUri, out var kernelByUri))
                {
                    kernelByUri.KernelInfo.UpdateSupportedKernelCommandsFrom(kernelInfoProduced.KernelInfo);
                }
                else if (e.Command.TargetKernelName is { } targetKernelName &&
                         _kernel.ChildKernels.TryGetByAlias(targetKernelName, out var kernelByName))
                {
                    kernelByName.KernelInfo.UpdateSupportedKernelCommandsFrom(kernelInfoProduced.KernelInfo);
                }
            }

            var _ = _defaultSender.SendAsync(e, _cancellationTokenSource.Token);
        });

        _receiver.Subscribe(commandOrEvent =>
        {
            if (commandOrEvent.IsParseError)
            {
                var _ = _defaultSender.SendAsync(commandOrEvent.Event, _cancellationTokenSource.Token);
            }
            else if (commandOrEvent.Command is { })
            {
                // this needs to be dispatched this way so that it does not block the current thread, which we see in certain bidirectional command scenarios (RequestInput sent by the SubmissionParser during magic command token interpolation) in stdio mode only (i.e. System.Console.In implementation details), and it has proven non-reproducible in tests.
                _eventLoop?.Schedule(async () =>
                {
                    try
                    {
                        await _kernel.SendAsync(commandOrEvent.Command, _cancellationTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        Logger.Log.Error(e);
                    }
                });
            }
        });

        var kernelInfos = new List<KernelInfo>();

        _kernel.VisitSubkernelsAndSelf(k =>
        {
            if (!k.KernelInfo.IsProxy)
            {
                kernelInfos.Add(k.KernelInfo);
            }
        });

        await _defaultSender.SendAsync(
            new KernelReady(kernelInfos.ToArray()),
            _cancellationTokenSource.Token);
    }

    public async Task ConnectAndWaitAsync()
    {
        await ConnectAsync();

        await _disposed.Task;
    }

    public void Dispose()
    {
        _eventLoop?.Dispose();
        _kernelEventSubscription?.Dispose();

        if (_cancellationTokenSource.Token.CanBeCanceled)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        _disposed.TrySetResult(Unit.Default);
    }

    public Uri Uri { get; }

    public async Task<ProxyKernel> ConnectProxyKernelAsync(
        string localName,
        Func<string, Task<ProxyKernel>> createKernelAsync,
        Uri remoteKernelUri,
        string[]? aliases = null)
    {
        if (createKernelAsync is null)
        {
            throw new ArgumentNullException(nameof(createKernelAsync));
        }

        var proxyKernel = await createKernelAsync(localName);

        proxyKernel.KernelInfo.RemoteUri = remoteKernelUri;

        if (aliases is not null)
        {
            proxyKernel.KernelInfo.NameAndAliases.UnionWith(aliases);
        }

        _kernel.Add(proxyKernel);

        return proxyKernel;
    }

    public async Task<ProxyKernel> ConnectProxyKernelOnDefaultConnectorAsync(
        string localName,
        Uri remoteKernelUri,
        string[]? aliases = null) =>
        await ConnectProxyKernelAsync(
            localName,
            _defaultConnector,
            remoteKernelUri,
            aliases);

    public static Uri CreateHostUriForCurrentProcessId() => CreateHostUriForProcessId(Process.GetCurrentProcess().Id);

    public static Uri CreateHostUriForProcessId(int processId) =>
        new($"kernel://pid-{processId}");

    public static Uri CreateHostUri(string name) => new($"kernel://{name}");
}
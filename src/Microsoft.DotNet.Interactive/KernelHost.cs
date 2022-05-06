// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class KernelHost : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new ();
        private readonly CompositeKernel _kernel;
        private readonly IKernelCommandAndEventSender _defaultSender;
        private readonly MultiplexingKernelCommandAndEventReceiver? _defaultReceiver;
        private readonly IKernelCommandAndEventReceiver2? _receiver2;
        private Task<Task>? _receiverLoop;
        private IDisposable? _kernelEventSubscription;
        private readonly IKernelConnector _defaultConnector;

        internal KernelHost(
            CompositeKernel kernel,
            IKernelCommandAndEventSender defaultSender,
            MultiplexingKernelCommandAndEventReceiver defaultReceiver,
            Uri hostUri)
        {
            Uri = hostUri ?? throw new ArgumentNullException(nameof(hostUri));
            _kernel = kernel;
            _defaultSender = defaultSender;
            _defaultReceiver = defaultReceiver;
            _defaultConnector = new DefaultKernelConnector(
                _defaultSender, 
                _defaultReceiver);
            _kernel.SetHost(this);
        }

        internal KernelHost(
            CompositeKernel kernel,
            IKernelCommandAndEventSender sender,
            IKernelCommandAndEventReceiver2 receiver,
            Uri hostUri)
        {
            Uri = hostUri ?? throw new ArgumentNullException(nameof(hostUri));
            _kernel = kernel;
            _defaultSender = sender;
            _receiver2 = receiver;
            _defaultConnector = new DefaultKernelConnector(
                _defaultSender,
                _receiver2);
            _kernel.SetHost(this);
        }

        private class DefaultKernelConnector : IKernelConnector
        {
            private readonly IKernelCommandAndEventSender _sender;
            private readonly MultiplexingKernelCommandAndEventReceiver? _receiver;
            private readonly IKernelCommandAndEventReceiver2? _receiver2;

            public DefaultKernelConnector(
                IKernelCommandAndEventSender sender,
                MultiplexingKernelCommandAndEventReceiver receiver)
            {
                _sender = sender;
                _receiver = receiver;
            }

            public DefaultKernelConnector(
                IKernelCommandAndEventSender sender,
                IKernelCommandAndEventReceiver2 receiver)
            {
                _sender = sender;
                _receiver2 = receiver;
            }

            public Task<Kernel> CreateKernelAsync(string kernelName)
            {
                ProxyKernel proxy;

                if (_receiver is not null)
                {
                    proxy = new ProxyKernel(
                        kernelName,
                        _sender,
                        _receiver.CreateChildReceiver(), new Uri(_sender.RemoteHostUri, kernelName));
                }
                else
                {
                    proxy = new ProxyKernel(
                        kernelName,
                        _sender,
                        _receiver2,
                        new Uri(_sender.RemoteHostUri, kernelName));
                }

                proxy.EnsureStarted();

                return Task.FromResult<Kernel>(proxy);
            }
        }

        public async Task ConnectAsync()
        {
            if (_receiverLoop is { })
            {
                throw new InvalidOperationException("The host is already connected.");
            }

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
                        kernelByUri.KernelInfo.UpdateFrom(kernelInfoProduced.KernelInfo);
                    }
                    else if (e.Command.TargetKernelName is { } targetKernelName &&
                             _kernel.ChildKernels.TryGetByAlias(targetKernelName, out var kernelByName))
                    {
                        kernelByName.KernelInfo.UpdateFrom(kernelInfoProduced.KernelInfo);
                    }
                }

                var _ = _defaultSender.SendAsync(e, _cancellationTokenSource.Token);
            });

            if (_defaultReceiver is not null)
            {
                _receiverLoop = Task.Factory.StartNew(
                    ReceiverLoop,
                    _cancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

                async Task ReceiverLoop()
                {
                    await foreach (var commandOrEvent in _defaultReceiver.CommandsAndEventsAsync(_cancellationTokenSource.Token))
                    {
                        if (commandOrEvent.IsParseError)
                        {
                            var _ = _defaultSender.SendAsync(commandOrEvent.Event, _cancellationTokenSource.Token);
                        }
                        else if (commandOrEvent.Command is { })
                        {
                            var _ = _kernel.SendAsync(commandOrEvent.Command, _cancellationTokenSource.Token);
                        }
                    }
                }
            }
            else if (_receiver2 is not null)
            {
                _receiver2.Subscribe(commandOrEvent =>
                {
                    if (commandOrEvent.IsParseError)
                    {
                        var _ = _defaultSender.SendAsync(commandOrEvent.Event, _cancellationTokenSource.Token);
                    }
                    else if (commandOrEvent.Command is { })
                    {
                        var _ = _kernel.SendAsync(commandOrEvent.Command, _cancellationTokenSource.Token);
                    }
                });
            }

            await _defaultSender.SendAsync(
                new KernelReady(),
                _cancellationTokenSource.Token);
        }

        public async Task ConnectAndWaitAsync()
        {
            await ConnectAsync();
            await _receiverLoop!;
        }

        public void Dispose()
        {
            _kernelEventSubscription?.Dispose();

            if (_cancellationTokenSource.Token.CanBeCanceled)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }

        public Uri Uri { get; }

        public async Task<ProxyKernel> ConnectProxyKernelAsync(
            string localName,
            IKernelConnector kernelConnector,
            Uri remoteKernelUri,
            string[]? aliases = null)
        {
            if (kernelConnector is null)
            {
                throw new ArgumentNullException(nameof(kernelConnector));
            }

            var proxyKernel = (ProxyKernel)await kernelConnector.CreateKernelAsync(localName);

            proxyKernel.KernelInfo.RemoteUri = remoteKernelUri;

            if (aliases is not null)
            {
                proxyKernel.KernelInfo.NameAndAliases.UnionWith(aliases);
            }

            _kernel.Add(proxyKernel);

            proxyKernel.EnsureStarted();

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
}
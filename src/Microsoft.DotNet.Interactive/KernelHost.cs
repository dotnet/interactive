// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
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
        private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;
        private Task<Task> _receiverLoop;
        private IDisposable _kernelEventSubscription;
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
            _defaultConnector = new DefaultKernelConnector(_defaultSender, _defaultReceiver);
            _kernel.SetHost(this);
        }

        private class DefaultKernelConnector : IKernelConnector
        {
            private readonly IKernelCommandAndEventSender _defaultSender;
            private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;

            public DefaultKernelConnector(IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
            {
                _defaultSender = defaultSender;
                _defaultReceiver = defaultReceiver;
            }

            public Task<Kernel> CreateKernelAsync(string kernelName)
            {
                var proxy = new ProxyKernel(
                    kernelName, 
                    _defaultReceiver.CreateChildReceiver(), 
                    _defaultSender);
                
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
                    // FIX: (ConnectAsync) update index
                }

                var _ = _defaultSender.SendAsync(e, _cancellationTokenSource.Token);
            });

            _receiverLoop = Task.Factory.StartNew(
                ReceiverLoop, 
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning, 
                TaskScheduler.Default);

            await _defaultSender.NotifyIsReadyAsync(_cancellationTokenSource.Token);

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
                    else
                    {

                    }
                }
            }
        }

        public async Task ConnectAndWaitAsync()
        {
            await ConnectAsync();
            await _receiverLoop;
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
            string[] aliases = null)
        {
            var proxyKernel = (ProxyKernel)await kernelConnector.CreateKernelAsync(localName);

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
            Uri remoteKernelUri = null, 
            string[] aliases = null) =>
            // FIX: (CreateProxyKernelOnDefaultConnectorAsync) what to do with this Uri? It can potentially connect through an intermediate proxy, but the remote needs to be able to resolve it
            await ConnectProxyKernelAsync(
                localName, 
                _defaultConnector, 
                aliases);
    }
}
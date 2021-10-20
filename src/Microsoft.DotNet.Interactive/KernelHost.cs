// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive
{
    public class KernelHost : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new ();
        private readonly CompositeKernel _kernel;
        private readonly IKernelCommandAndEventSender _defaultSender;
        private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;
        private Task<Task> _runningLoop;
        private IDisposable _kernelEventSubscription;
        private readonly Dictionary<Kernel, KernelInfo> _kernelInfos = new();
        public IKernelConnector DefaultConnector { get; }


        public KernelHost(CompositeKernel kernel,IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
        {
            _kernel = kernel;
            _defaultSender = defaultSender;
            _defaultReceiver = defaultReceiver;
            DefaultConnector = new DefaultKernelConnector(_defaultSender, _defaultReceiver);
            Uri = KernelUri.Parse($"kernel://.net/{Guid.NewGuid():N}");
            _kernel.SetHost(this);

        }
        
        public static KernelHost InProcess(CompositeKernel kernel, Func<CommandOrEvent, Task> onSend = null)
        {

            var receiver = new MultiplexingKernelCommandAndEventReceiver(new InProcessCommandAndEventReceiver());

            var sender = new InProcessCommandAndEventSender();
            if (onSend is not null)
            {
                sender.OnSend(onSend);
            }
            return new KernelHost(kernel, sender, receiver);
        }

        internal IKernelCommandAndEventSender DefaultSender => _defaultSender;

        internal MultiplexingKernelCommandAndEventReceiver DefaultReceiver => _defaultReceiver;

        private class DefaultKernelConnector : IKernelConnector
        {
            private readonly IKernelCommandAndEventSender _defaultSender;
            private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;

            public DefaultKernelConnector(IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
            {
                _defaultSender = defaultSender;
                _defaultReceiver = defaultReceiver;
            }

            public Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
            {
                var proxy = new ProxyKernel(kernelInfo.LocalName, _defaultReceiver.CreateChildReceiver(), _defaultSender);
                var _ = proxy.StartAsync();
                return Task.FromResult((Kernel)proxy);
            }
        }

        public async Task ConnectAsync()
        {
            if (_runningLoop is { })
            {
                throw new InvalidOperationException("The host is already connected.");
            }

            _kernelEventSubscription =  _kernel.KernelEvents.Subscribe(e =>
            {
                var _ = _defaultSender.SendAsync(e, _cancellationTokenSource.Token);
            });

            _runningLoop = Task.Factory.StartNew(async () =>
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
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            await _defaultSender.NotifyIsReadyAsync(_cancellationTokenSource.Token);
        }

        public async Task ConnectAndWaitAsync()
        {
            await ConnectAsync();
            await _runningLoop;
        }

        public void Dispose()
        {
            _kernelEventSubscription?.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public bool TryGetKernelInfo(Kernel kernel, out KernelInfo kernelInfo)
        {
            return _kernelInfos.TryGetValue(kernel, out kernelInfo);
        }

        public void AddKernelInfo(Kernel kernel, KernelInfo kernelInfo)
        {
            kernelInfo.Uri = Uri.Append(kernel.Name);
            _kernelInfos.Add(kernel,kernelInfo);
        }

        public KernelUri Uri { get;  }

        private class InProcessCommandAndEventSender : IKernelCommandAndEventSender
        {
            private Func<CommandOrEvent, Task> _onSendAsync;

            public Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
            {
                _onSendAsync?.Invoke(new CommandOrEvent(KernelCommandEnvelope.Deserialize(KernelCommandEnvelope.Serialize(kernelCommand)).Command));
                return Task.CompletedTask;
            }

            public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
            {
                _onSendAsync?.Invoke(new CommandOrEvent(KernelEventEnvelope.Deserialize(KernelEventEnvelope.Serialize(kernelEvent)).Event));
                return Task.CompletedTask;
            }

            public void OnSend(Action<CommandOrEvent> onSend)
            {
                _onSendAsync = (commandOrEvent) =>
                {

                    onSend(commandOrEvent);
                    return Task.CompletedTask;
                };
            }

            public void OnSend(Func<CommandOrEvent, Task> onSendAsync)
            {
                _onSendAsync = onSendAsync;
            }
        }

        private class InProcessCommandAndEventReceiver : KernelCommandAndEventReceiverBase
        {
            private readonly BlockingCollection<CommandOrEvent> _commandsOrEvents;

            public InProcessCommandAndEventReceiver()
            {
                _commandsOrEvents = new BlockingCollection<CommandOrEvent>();
            }

            public void Write(CommandOrEvent commandOrEvent)
            {
                if (commandOrEvent.Command is { })
                {
                    _commandsOrEvents.Add(new CommandOrEvent(KernelCommandEnvelope
                        .Deserialize(KernelCommandEnvelope.Serialize(commandOrEvent.Command)).Command));
                }
                else if (commandOrEvent.Event is { })
                {
                    _commandsOrEvents.Add(new CommandOrEvent(KernelEventEnvelope
                        .Deserialize(KernelEventEnvelope.Serialize(commandOrEvent.Event)).Event));
                }
            }

            protected override Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_commandsOrEvents.Take(cancellationToken));
            }
        }

        private void RegisterRemoteUriForProxy(string proxyLocalKernelName, KernelUri remoteKernelUri)
        {
            var childKernel = _kernel.FindKernel(proxyLocalKernelName);
            if (childKernel is ProxyKernel proxyKernel)
            {
                if (proxyKernel == null)
                {
                    throw new ArgumentNullException(nameof(proxyKernel));
                }

                if (remoteKernelUri == null)
                {
                    throw new ArgumentNullException(nameof(remoteKernelUri));
                }
            
                if (TryGetKernelInfo(proxyKernel, out var kernelInfo))
                {
                    kernelInfo.RemoteUri = remoteKernelUri;
                }else
                {
                    throw new ArgumentException($"Unknown kernel name : {proxyKernel.Name}");
                }
            }
            else
            {
                throw new ArgumentException($"Cannot find Kernel {proxyLocalKernelName} or it is not a valid ProxyKernel");
            }
        }

        public async Task<ProxyKernel> CreateProxyKernelOnDefaultConnectorAsync(KernelInfo kernelInfo)
        {
            var childKernel = await DefaultConnector.ConnectKernelAsync(kernelInfo) as ProxyKernel;
            _kernel.Add(childKernel, kernelInfo.Aliases);
            RegisterRemoteUriForProxy(kernelInfo.LocalName, kernelInfo.RemoteUri);
            return childKernel;
        }
    }

   
}

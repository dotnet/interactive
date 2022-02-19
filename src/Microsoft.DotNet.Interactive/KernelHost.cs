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
        private readonly Dictionary<Uri,Kernel> _destinationUriToKernel = new ();
        private readonly KernelConnectorBase _defaultConnector;
        private readonly Dictionary<Uri, Kernel> _originUriToKernel = new();

        public KernelHost(CompositeKernel kernel, IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver, Uri hostUri)
        {
            _kernel = kernel;
            _defaultSender = defaultSender;
            _defaultReceiver = defaultReceiver;
            _defaultConnector = new DefaultKernelConnector(_defaultSender, _defaultReceiver);
            Uri = hostUri;
            _kernel.SetHost(this);
        }

        public KernelHost(CompositeKernel kernel,IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver) : this(kernel, defaultSender, defaultReceiver, new Uri("kernel://dotnet", UriKind.Absolute))
        {
        }

        public static KernelHost InProcess(CompositeKernel kernel)
        {
            // QUESTION: (InProcess) does this need to be here? the implementation looks incomplete.
            var receiver = new MultiplexingKernelCommandAndEventReceiver(new InProcessCommandAndEventReceiver());

            var sender = new InProcessCommandAndEventSender();
          
            return new KernelHost(kernel, sender, receiver);
        }

        private class DefaultKernelConnector : KernelConnectorBase
        {
            private readonly IKernelCommandAndEventSender _defaultSender;
            private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;

            public DefaultKernelConnector(IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
            {
                _defaultSender = defaultSender;
                _defaultReceiver = defaultReceiver;
            }

            public override Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
            {
                var proxy = new ProxyKernel(kernelInfo.LocalName, _defaultReceiver.CreateChildReceiver(), _defaultSender);
                var _ = proxy.StartAsync();
                return Task.FromResult((Kernel)proxy);
            }

            public void Dispose()
            {
                _defaultReceiver.Dispose();
            }
        }

        public async Task ConnectAsync()
        {
            if (_runningLoop is { })
            {
                throw new InvalidOperationException("The host is already connected.");
            }

            _kernelEventSubscription = _kernel.KernelEvents.Subscribe(e =>
            {
                if (e is ReturnValueProduced { Value: DisplayedValue })
                {
                    return;
                }
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
            }, _cancellationTokenSource.Token, 
                                                 TaskCreationOptions.LongRunning, TaskScheduler.Default);

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

        internal void AddKernelInfo(Kernel kernel, KernelInfo kernelInfo)
        {
            kernelInfo.OriginUri = new Uri(Uri, kernel.Name);
            _kernelInfos.Add(kernel,kernelInfo);
            _originUriToKernel[kernelInfo.OriginUri] = kernel;
        }

        public Uri Uri { get;  }

        private class InProcessCommandAndEventSender : IKernelCommandAndEventSender
        {
            // QUESTION: (InProcessCommandAndEventSender) does this need to be tested for compliance with other implementations?
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
                _onSendAsync = commandOrEvent =>
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
            // QUESTION: (InProcessCommandAndEventReceiver) does this need to be tested for compliance with other implementations?
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

        internal void RegisterDestinationUriForProxy(ProxyKernel proxyKernel, Uri destinationUri)
        {
            if (proxyKernel == null)
            {
                throw new ArgumentNullException(nameof(proxyKernel));
            }

            if (destinationUri == null)
            {
                throw new ArgumentNullException(nameof(destinationUri));
            }

            if (TryGetKernelInfo(proxyKernel, out var kernelInfo))
            {
                if (kernelInfo.DestinationUri is { })
                {
                    _destinationUriToKernel.Remove(kernelInfo.DestinationUri);
                }

                kernelInfo.DestinationUri = destinationUri;
                _destinationUriToKernel[kernelInfo.DestinationUri] = proxyKernel;
            }
            else
            {
                throw new ArgumentException($"Unknown kernel name : {proxyKernel.Name}");
            }
        }

        internal void RegisterDestinationUriForProxy(string proxyLocalKernelName, Uri destinationUri)
        {
            var childKernel = _kernel.FindKernel(proxyLocalKernelName);
            if (childKernel is ProxyKernel proxyKernel)
            {
                RegisterDestinationUriForProxy(proxyKernel, destinationUri);
            }
            else
            {
                throw new ArgumentException($"Cannot find Kernel {proxyLocalKernelName} or it is not a valid ProxyKernel");
            }
        }

        public async Task<ProxyKernel> CreateProxyKernelOnDefaultConnectorAsync(KernelInfo kernelInfo)
        {
            var childKernel = await CreateProxyKernelOnConnectorAsync(kernelInfo,_defaultConnector);
            return childKernel;
        }

        public async Task<ProxyKernel> CreateProxyKernelOnConnectorAsync(KernelInfo kernelInfo, KernelConnectorBase kernelConnectorBase )
        {
            var childKernel = await kernelConnectorBase.ConnectKernelAsync(kernelInfo) as ProxyKernel;
            _kernel.Add(childKernel, kernelInfo.Aliases);
            RegisterDestinationUriForProxy(kernelInfo.LocalName, kernelInfo.DestinationUri);
            return childKernel;
        }

        public bool TryGetKernelByDestinationUri(Uri destinationUri, out Kernel kernel)
        {
            return _destinationUriToKernel.TryGetValue(destinationUri, out kernel);
        }

        public bool TryGetKernelByOriginUri(Uri originUri, out Kernel kernel)
        {
            return _originUriToKernel.TryGetValue(originUri, out kernel);
        }
    }
}

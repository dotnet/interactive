// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Connection
{
    public sealed class ProxyKernel : Kernel
    {
        private readonly IKernelCommandAndEventReceiver _receiver;
        private readonly IKernelCommandAndEventSender _sender;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private ExecutionContext _executionContext;
        private readonly Dictionary<string,(KernelCommand command, ExecutionContext executionContext, TaskCompletionSource<bool> completionSource)> _inflight = new();
        private int _started = 0;

        public ProxyKernel(string name, IKernelCommandAndEventReceiver receiver, IKernelCommandAndEventSender sender) : base(name)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));

            RegisterForDisposal(() =>
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            });
        }


        public Task StartAsync()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) == 1)
            {
                throw new InvalidOperationException($"ProxyKernel {Name} is already started.");
            }
            
            return Task.Run(async () => { await ReceiveAndDispatchCommandsAndEvents(); }, _cancellationTokenSource.Token);
        }

        private async Task ReceiveAndDispatchCommandsAndEvents()
        {
            await foreach (var d in _receiver.CommandsAndEventsAsync(_cancellationTokenSource.Token))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (d.Event is not null)
                {
                    DelegatePublication(d.Event);
                }
                else if (d.Command is not null)
                {
                    var _ = Task.Run(async () =>
                    {
                        var eventSubscription = RootKernel.KernelEvents
                            .Where(e => e.Command.GetToken() == d.Command.GetToken() && e.Command.GetType() == d.Command.GetType())
                            .Subscribe(async e =>
                            {
                                await _sender.SendAsync(e, _cancellationTokenSource.Token);
                            });

                        await RootKernel.SendAsync(d.Command, _cancellationTokenSource.Token);
                        eventSubscription.Dispose();
                    }, _cancellationTokenSource.Token);
                }
            }
        }

        internal override Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            switch (command)
            {
                case DirectiveCommand { DirectiveNode: KernelNameDirectiveNode }:
                    return base.HandleAsync(command, context);
            }

            _executionContext = ExecutionContext.Capture();
            var kernelUri = KernelUri.Parse(command.TargetKernelName);
            var remoteTargetKernelName = kernelUri.GetRemoteKernelName();
            var localTargetKernelName = command.TargetKernelName;
            command.TargetKernelName = remoteTargetKernelName;
            var token = command.GetToken();
          
            var completionSource = new TaskCompletionSource<bool>();
            _inflight[token] = (command, _executionContext, completionSource);

            var _ = _sender.SendAsync(command, context.CancellationToken);
            return completionSource.Task.ContinueWith(_ =>
            {
                command.TargetKernelName = localTargetKernelName;
            });
        }

        protected internal override void DelegatePublication(KernelEvent kernelEvent)
        {
            var token = kernelEvent.Command.GetToken(); 
            
            var hasPending =  _inflight.TryGetValue(token,out var pending);

            if (hasPending && pending.executionContext is { } ec)
            {
                ExecutionContext.Run(ec, _ =>
                {
                    PublishEvent(kernelEvent);
                }, null);
            }
            else
            {
                PublishEvent(kernelEvent);
            }

            switch (kernelEvent)
            {
                case CommandFailed _ when hasPending && pending.command.IsEquivalentTo(kernelEvent.Command):
                case CommandSucceeded _ when hasPending && pending.command.IsEquivalentTo(kernelEvent.Command):
                    _inflight.Remove(token);
                    pending.completionSource.TrySetResult(true);
                    break;
            }
        }
    }
}
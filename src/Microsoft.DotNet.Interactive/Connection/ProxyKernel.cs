// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public Task RunAsync()
        {
            return Task.Run(async () => { await ReceiveAndDispatchCommandsAndEvents(); }, _cancellationTokenSource.Token);
        }

        private async Task ReceiveAndDispatchCommandsAndEvents()
        {
            await foreach (var d in _receiver.CommandsOrEventsAsync(_cancellationTokenSource.Token))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (d.Event is not null)
                {
                    if (_executionContext is { } ec)
                    {
                        ExecutionContext.Run(ec, _ =>
                        {
                            PublishEvent(d.Event);
                        },null);
                    }
                    else
                    {
                        PublishEvent(d.Event);
                    }
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


        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            switch (command)
            {
                case DirectiveCommand { DirectiveNode: KernelNameDirectiveNode }:
                    await base.HandleAsync(command, context);
                    return;
            }

            _executionContext = ExecutionContext.Capture();
            var kernelUri = KernelUri.Parse(command.TargetKernelName);
            var remoteTargetKernelName = kernelUri.GetRemoteKernelName();
            var localTargetKernelName = command.TargetKernelName;
            command.TargetKernelName = remoteTargetKernelName;
            var token = command.GetToken();
            var completionSource = new TaskCompletionSource<bool>();
            var sub = KernelEvents
                .Where(e => e.Command.GetToken() == token)
                .Subscribe(kernelEvent =>
                {
                    switch (kernelEvent)
                    {
                        case CommandFailed _:
                        case CommandSucceeded _:
                            completionSource.TrySetResult(true);
                            _executionContext = null;
                            break;

                    }
                });

            var _ = _sender.SendAsync(command, context.CancellationToken);
            await completionSource.Task;
            sub.Dispose();

            command.TargetKernelName = localTargetKernelName;
        }
    }
}
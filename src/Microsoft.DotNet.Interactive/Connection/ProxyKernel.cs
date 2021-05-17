// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Connection
{
    public sealed class ProxyKernel : Kernel
    {
        private readonly KernelClientBase _client;

        public ProxyKernel(string name, KernelClientBase client) : base(name)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            RegisterForDisposal(client.KernelEvents.Subscribe(OnKernelEvents));
        }

        private void OnKernelEvents(KernelEvent kernelEvent)
        {
            PublishEvent(kernelEvent);
        }

        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            var targetKernelName = command.TargetKernelName;
            command.TargetKernelName = null;
            await _client.SendAsync(command);
            command.TargetKernelName = targetKernelName;
        }
    }

    public sealed class ProxyKernel2 : Kernel
    {
        private readonly IKernelCommandAndEventReceiver _receiver;
        private readonly IKernelCommandAndEventSender _sender;
        private readonly CancellationTokenSource _cancellationTokenSource = new ();

        public ProxyKernel2(string name, IKernelCommandAndEventReceiver receiver, IKernelCommandAndEventSender sender) : base(name)
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
            await foreach (var d in _receiver.CommandsOrEventsAsync())
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (d.Event is not null)
                {
                    PublishEvent(d.Event);
                }
                else if (d.Command is not null)
                {
                    var kernel = RootKernel;
                    var result = await kernel.SendAsync(d.Command);
                    result.KernelEvents.Subscribe(async e => await _sender.SendAsync(e));
                }
            }
        }


        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            switch (command)
            {
                case DirectiveCommand {DirectiveNode: KernelNameDirectiveNode}:
                    await base.HandleAsync(command, context);
                    return;
            }

            var targetKernelName = command.TargetKernelName;
            command.TargetKernelName = null;
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
                            break;

                    }
                });
            
            var _ =  _sender.SendAsync(command);
            await completionSource.Task;
            sub.Dispose();

            command.TargetKernelName = targetKernelName;
        }
    }
}
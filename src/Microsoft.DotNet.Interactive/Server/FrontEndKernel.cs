// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Server
{
    public class FrontEndKernel : Kernel
    {
        private readonly IKernelCommandAndEventSender _sender;

        public FrontEndKernel(string name, IKernelCommandAndEventSender sender)
            : base(name)
        {
            _sender = sender;
        }

        public void ForwardEvent(KernelEvent @event)
        {
            PublishEvent(@event);
        }

        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            switch (command)
            {
                case DirectiveCommand { DirectiveNode: KernelNameDirectiveNode }:
                    await base.HandleAsync(command, context);
                    return;
            }

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

            var _ = _sender.SendAsync(command, context.CancellationToken);
            await completionSource.Task;
            sub.Dispose();
        }
    }
}

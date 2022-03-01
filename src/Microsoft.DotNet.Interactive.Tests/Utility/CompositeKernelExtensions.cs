// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class CompositeKernelExtensions
{
    public static CompositeKernel UseInProcessHost(this CompositeKernel kernel)
    {
        var receiver = new MultiplexingKernelCommandAndEventReceiver(new BlockingCommandAndEventReceiver());

        var sender = new InProcessCommandAndEventSender();

        var host = kernel.UseHost(sender, receiver);

        kernel.RegisterForDisposal(host);

        return kernel;
    }

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
    }
}
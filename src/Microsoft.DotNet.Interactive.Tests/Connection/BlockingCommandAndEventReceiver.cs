// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

public class BlockingCommandAndEventReceiver :
    KernelCommandAndEventReceiverBase
{
    private readonly BlockingCollection<CommandOrEvent> _commandsOrEvents;

    public BlockingCommandAndEventReceiver()
    {
        _commandsOrEvents = new BlockingCollection<CommandOrEvent>();
    }

    public IKernelCommandAndEventSender CreateSender()
    {
        return new Sender(this);
    }

    public void Write(CommandOrEvent commandOrEvent)
    {
        if (commandOrEvent.Command is { })
        {
            var command = new CommandOrEvent(
                RoundTripSerializeCommand(commandOrEvent).Command);

            _commandsOrEvents.Add(command);
        }
        else if (commandOrEvent.Event is { })
        {
            var @event = new CommandOrEvent(
                RoundTripSerializeEvent(commandOrEvent).Event);

            _commandsOrEvents.Add(@event);
        }
    }

    private static IKernelEventEnvelope RoundTripSerializeEvent(CommandOrEvent commandOrEvent) =>
        KernelEventEnvelope.Deserialize(
            KernelEventEnvelope.Serialize(
                commandOrEvent.Event));

    private static IKernelCommandEnvelope RoundTripSerializeCommand(CommandOrEvent commandOrEvent) =>
        KernelCommandEnvelope.Deserialize(
            KernelCommandEnvelope.Serialize(
                commandOrEvent.Command));

    protected override Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_commandsOrEvents.Take(cancellationToken));
    }

    public class Sender : IKernelCommandAndEventSender
    {
        private readonly BlockingCommandAndEventReceiver _receiver;

        public Sender(BlockingCommandAndEventReceiver receiver)
        {
            _receiver = receiver;
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            await Task.Yield();
            _receiver.Write(new CommandOrEvent(kernelCommand));
        }

        public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            await Task.Yield();
            _receiver.Write(new CommandOrEvent(kernelEvent));
        }
    }
}
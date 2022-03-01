// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection;

public class BlockingCommandAndEventReceiver : KernelCommandAndEventReceiverBase
{
    private readonly BlockingCollection<CommandOrEvent> _commandsOrEvents;

    public BlockingCommandAndEventReceiver()
    {
        _commandsOrEvents = new BlockingCollection<CommandOrEvent>();
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

    private static IKernelEventEnvelope RoundTripSerializeEvent(CommandOrEvent commandOrEvent)
    {
        return KernelEventEnvelope
            .Deserialize(KernelEventEnvelope.Serialize(commandOrEvent.Event));
    }

    private static IKernelCommandEnvelope RoundTripSerializeCommand(CommandOrEvent commandOrEvent) =>
        KernelCommandEnvelope.Deserialize(
            KernelCommandEnvelope.Serialize(commandOrEvent.Command));

    protected override Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_commandsOrEvents.Take(cancellationToken));
    }
}
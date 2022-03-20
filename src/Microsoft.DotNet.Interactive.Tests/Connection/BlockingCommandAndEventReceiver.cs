// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

public class BlockingCommandAndEventReceiver : KernelCommandAndEventReceiverBase
{
    private readonly BlockingCollection<CommandOrEvent> _commandsOrEvents;

    public BlockingCommandAndEventReceiver(Uri hostUri)
    {
        HostUri = hostUri;
        _commandsOrEvents = new BlockingCollection<CommandOrEvent>();
    }

    public Uri HostUri { get; }

    public IKernelCommandAndEventSender CreateSender(Uri remoteHostUri)
    {
        return new Sender(this, remoteHostUri);
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

        static IKernelEventEnvelope RoundTripSerializeEvent(CommandOrEvent commandOrEvent) =>
            KernelEventEnvelope.Deserialize(
                KernelEventEnvelope.Serialize(
                    commandOrEvent.Event));

        static IKernelCommandEnvelope RoundTripSerializeCommand(CommandOrEvent commandOrEvent) =>
            KernelCommandEnvelope.Deserialize(
                KernelCommandEnvelope.Serialize(
                    commandOrEvent.Command));
    }

    protected override async Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        var commandOrEvent = _commandsOrEvents.Take(cancellationToken);
        return commandOrEvent;
    }

    private class Sender : IKernelCommandAndEventSender
    {
        public Uri RemoteHostUri { get; }
        private readonly BlockingCommandAndEventReceiver _receiver;

        public Sender(BlockingCommandAndEventReceiver receiver, Uri remoteHostUri)
        {
            RemoteHostUri = remoteHostUri;
            _receiver = receiver;
        }

        public Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            _receiver.Write(new CommandOrEvent(kernelCommand));

            return Task.CompletedTask;
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            _receiver.Write(new CommandOrEvent(kernelEvent));

            return Task.CompletedTask;
        }
    }
}
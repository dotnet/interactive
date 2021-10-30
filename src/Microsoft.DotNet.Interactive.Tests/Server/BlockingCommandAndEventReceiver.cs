// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    internal class BlockingCommandAndEventReceiver : KernelCommandAndEventReceiverBase
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
}
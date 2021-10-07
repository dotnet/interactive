// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
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
            _commandsOrEvents.Add(commandOrEvent);
        }

        protected override Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_commandsOrEvents.Take(cancellationToken));
        }
    }
}
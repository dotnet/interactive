// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    internal class BlockingCommandAndEventReceiver : IKernelCommandAndEventReceiver
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
        public IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken)
        {
            return _commandsOrEvents.GetConsumingEnumerable(cancellationToken).ToAsyncEnumerable();
        }
    }
}
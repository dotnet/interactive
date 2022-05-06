// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public abstract class KernelCommandAndEventReceiverBase : IKernelCommandAndEventReceiver
    {
        private readonly Subject<CommandOrEvent> _subject = new();

        protected abstract Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken);

        public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var commandOrEvent = await ReadCommandOrEventAsync(cancellationToken);

                if (commandOrEvent is null)
                {
                    continue;
                }
               
                yield return commandOrEvent;
            }
        }

        public IObservable<CommandOrEvent> CommandsAndEvents => _subject;
    }
}
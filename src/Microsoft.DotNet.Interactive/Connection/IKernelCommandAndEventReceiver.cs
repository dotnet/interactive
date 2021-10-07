// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public interface IKernelCommandAndEventReceiver
    {
        IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken);
    }

    public static class KernelCommandAndEventReceiverExtensions
    {
        public static Task ConnectAsync(this IKernelCommandAndEventReceiver receiver, Kernel kernel, Func<KernelEvent, CancellationToken, Task> onParseErrorAsync = null,
            CancellationToken cancellationToken = default)

        {
            return Task.Run(async () =>
            {
                await foreach (var commandOrEvent in receiver.CommandsAndEventsAsync(cancellationToken))
                {
                    if (commandOrEvent.IsParseError)
                    {
                        var _ = onParseErrorAsync?.Invoke(commandOrEvent.Event, cancellationToken);
                    }
                    else
                    {
                        await commandOrEvent.DispatchAsync(kernel, cancellationToken);
                    }
                }
            }, cancellationToken);
        }
    }
}
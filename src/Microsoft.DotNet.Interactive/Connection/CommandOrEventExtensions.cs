// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public static class CommandOrEventExtensions
    {
        public static Task DispatchAsync(this CommandOrEvent commandOrEvent, Kernel kernel, CancellationToken cancellationToken)
        {
            if (commandOrEvent.Command is { })
            {
                var _ = kernel.SendAsync(commandOrEvent.Command, cancellationToken);
            }
            else if (commandOrEvent.Event is { })
            {
                kernel.DelegatePublication(commandOrEvent.Event);
            }

            return Task.CompletedTask;
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public static class KernelCommandAndEventSenderExtensions
    {
        public static void NotifyIsReady(this IKernelCommandAndEventSender sender)
        {
            sender.NotifyIsReadyAsync( CancellationToken.None)
                .Wait();
        }

        public static Task NotifyIsReadyAsync(this IKernelCommandAndEventSender sender, CancellationToken cancellationToken)
        {
            return sender.SendAsync(new KernelReady(), cancellationToken);
        }
    }
}
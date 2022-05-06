// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection;

public abstract class KernelCommandAndEventDeserializingReceiverBase : KernelCommandAndEventReceiverBase
{
    protected abstract Task<string> ReadMessageAsync(CancellationToken cancellationToken);

    protected override async Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
    {
        var message = await ReadMessageAsync(cancellationToken);

        return Serializer.DeserializeCommandOrEvent(message);
    }
}
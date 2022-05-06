// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Connection;

public interface IKernelCommandAndEventReceiver
{
    IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken);
}
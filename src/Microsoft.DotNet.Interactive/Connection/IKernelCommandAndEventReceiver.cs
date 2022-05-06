// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Connection;

public interface IKernelCommandAndEventReceiver
{
    // FIX: (IKernelCommandAndEventReceiver) delete me
    IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken);
}

public interface IKernelCommandAndEventReceiver2 : IObservable<CommandOrEvent>
{
}
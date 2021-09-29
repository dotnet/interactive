// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    internal class NullKernelCommandAndEventReceiver : IKernelCommandAndEventReceiver
    {
        public IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
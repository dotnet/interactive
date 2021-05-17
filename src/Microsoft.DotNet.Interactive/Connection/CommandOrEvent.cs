// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class CommandOrEvent
    {
        public KernelCommand Command { get; }
        public KernelEvent Event { get; }

        public CommandOrEvent(KernelCommand kernelCommand)
        {
            Command = kernelCommand ?? throw new ArgumentNullException(nameof(kernelCommand));
            Event = null;
        }

        public CommandOrEvent(KernelEvent kernelEvent)
        {
            Command = null;
            Event = kernelEvent ?? throw new ArgumentNullException(nameof(kernelEvent));
        }
    };
}
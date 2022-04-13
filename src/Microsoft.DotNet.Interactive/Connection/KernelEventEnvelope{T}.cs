// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    public class KernelEventEnvelope<T> : KernelEventEnvelope
        where T : KernelEvent
    {
        public KernelEventEnvelope(T @event) : base(@event)
        {
            Event = @event;
        }

        public T Event { get; }

        public override string EventType => typeof(T).Name;
    }
}

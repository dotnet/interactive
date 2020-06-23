// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class KernelCommandResult
    {
        internal KernelCommandResult(IObservable<KernelEvent> events)
        {
            KernelEvents = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IObservable<KernelEvent> KernelEvents { get; }
    }
}
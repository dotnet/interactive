// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    internal class KernelCommandResult : IKernelCommandResult
    {
        public KernelCommandResult(IObservable<IKernelEvent> events)
        {
            KernelEvents = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IObservable<IKernelEvent> KernelEvents { get; }
    }
}
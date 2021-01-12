// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public abstract class KernelClientBase
    {
        public abstract IObservable<KernelEvent> KernelEvents { get; }

        public abstract Task SendAsync(KernelCommand command, string token = null);
    }
}
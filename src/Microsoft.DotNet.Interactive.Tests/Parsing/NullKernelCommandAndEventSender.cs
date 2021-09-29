// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    internal class NullKernelCommandAndEventSender : IKernelCommandAndEventSender
    {
        public Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
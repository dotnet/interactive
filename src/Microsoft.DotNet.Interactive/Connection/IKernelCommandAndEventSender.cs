// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection;

public interface IKernelCommandAndEventSender
{
    Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken);

    Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken);

    Uri RemoteHostUri { get; }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive;

public static class CompositeKernelExtensions
{
    public static KernelHost UseHost(
        this CompositeKernel kernel,
        IKernelCommandAndEventSender defaultSender,
        MultiplexingKernelCommandAndEventReceiver defaultReceiver,
        Uri hostUri = null)
    {
        return new KernelHost(kernel, defaultSender, defaultReceiver, hostUri);
    }
}
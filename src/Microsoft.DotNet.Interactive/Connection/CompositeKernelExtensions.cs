// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Connection;

public static class CompositeKernelExtensions
{
    public static KernelHost UseHost(
        this CompositeKernel kernel,
        IKernelCommandAndEventSender sender,
        IKernelCommandAndEventReceiver receiver,
        Uri hostUri)
    {
        return new KernelHost(kernel, sender, receiver, hostUri);
    }
}
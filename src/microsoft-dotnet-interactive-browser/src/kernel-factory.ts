// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelTransport } from "./dotnet-interactive/contracts";
import { Kernel } from "./dotnet-interactive/kernel";

export function attachKernelToTransport(kernel: Kernel, kernelTransport: KernelTransport) {
    kernelTransport.setCommandHandler(env => kernel.send(env));
    kernel.subscribeToKernelEvents(env => kernelTransport.publishKernelEvent(env))
}

let kernel: Kernel = null

export async function clientSideKernelFactory(kernelTransport: KernelTransport): Promise<Kernel> {
    if (!kernel) {
        // We need the client-side kernel to be a singleton. However, this factory method is
        // invoked each time a JS cell executes. This has the slightly unfortunate but ultimately
        // harmless effect that each cell sets up its own transport, so we end up with a multitude
        // of transports. But to have multiple kernels would become problematic - each would attempt
        // to handle incoming commands, leading to multiple handler invocations if a cell registering
        // a handler were run multiple times.
        kernel = new Kernel("client-side-kernel");
        attachKernelToTransport(kernel, kernelTransport);
    }
    return kernel;
}
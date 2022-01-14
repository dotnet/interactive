// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Connector } from "./dotnet-interactive/contracts";
import { Kernel } from "./dotnet-interactive/kernel";

export function attachKernelToConnector(kernel: Kernel, connector: Connector) {
    connector.setCommandHandler(env => kernel.send(env));
    kernel.subscribeToKernelEvents(env => connector.publishKernelEvent(env))
}

let kernel: Kernel | undefined;

export async function clientSideKernelFactory(connector: Connector): Promise<Kernel> {
    if (!kernel) {
        // We need the client-side kernel to be a singleton. However, this factory method is
        // invoked each time a JS cell executes. This has the slightly unfortunate but ultimately
        // harmless effect that each cell sets up its own transport, so we end up with a multitude
        // of transports. But to have multiple kernels would become problematic - each would attempt
        // to handle incoming commands, leading to multiple handler invocations if a cell registering
        // a handler were run multiple times.
        kernel = new Kernel("client-side-kernel");
        attachKernelToConnector(kernel, connector);
    }
    return kernel;
}
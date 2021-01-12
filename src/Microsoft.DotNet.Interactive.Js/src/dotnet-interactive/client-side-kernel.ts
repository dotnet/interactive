// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DisposableSubscription, KernelEventEnvelopeObserver, KernelCommandEnvelope, KernelTransport } from "./contracts";
import { Kernel } from "./dotnet-interactive-interfaces";

export class ClientSideKernel implements Kernel {
    private _commandHandlers: { [commandType: string]: (envelope: KernelCommandEnvelope) => Promise<void> } = {};

    send(command: KernelCommandEnvelope): Promise<void> {
        // TODO: add this to a queue, and deal with completion properly.
        let handler = this._commandHandlers[command.commandType];
        if (handler) {
            handler(command);
        }
        return Promise.resolve();
    }
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        // TODO: implement
        return { dispose: () => {} };
    }

    registerCommandHandler(commandType: string, handler: (envelope: KernelCommandEnvelope) => Promise<void>): void {
        // When a registration already existed, we want to overwrite it because we want users to
        // be able to develop handlers iteratively, and it would be unhelpful for handler registration
        // for any particular command to be cumulative.
        this._commandHandlers[commandType] = handler;
    }
}

export function attachKernelToTransport(kernel: Kernel, kernelTransport: KernelTransport) {
    kernelTransport.subscribeToCommands(env => kernel.send(env));
    kernel.subscribeToKernelEvents(env => kernelTransport.publishKernelEvent(env))
}

let kernel: ClientSideKernel = null

export async function clientSideKernelFactory(kernelTransport: KernelTransport): Promise<Kernel> {
    if (!kernel) {
        // We need the client-side kernel to be a singleton. However, this factory method is
        // invoked each time a JS cell executes. This has the slightly unfortunate but ultimately
        // harmless effect that each cell sets up its own transport, so we end up with a multitude
        // of transports. But to have multiple kernels would become problematic - each would attempt
        // to handle incoming commands, leading to multiple handler invocations if a cell registering
        // a handler were run multiple times.
        kernel = new ClientSideKernel();
        attachKernelToTransport(kernel, kernelTransport);
    }
    return kernel;
}
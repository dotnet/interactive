// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientSideKernelInvocationContext } from "./client-side-kernel-invocation-context";
import { DisposableSubscription, KernelEventEnvelopeObserver, KernelCommandEnvelope, KernelTransport } from "./contracts";
import { Kernel, KernelInvocationContext } from "./dotnet-interactive-interfaces";

export class ClientSideKernel implements Kernel {
    private _commandHandlers: { [commandType: string]: (envelope: KernelCommandEnvelope, context: KernelInvocationContext) => Promise<void> } = {};

    // Is it worth us going to efforts to ensure that the Promise returned here accurately reflects
    // the command's progress? The only thing that actually calls this is the kernel transport, through
    // the callback set up by attachKernelToTransport, and the callback is expected to return void, so
    // nothing is ever going to look at the promise we return here.
    send(command: KernelCommandEnvelope): Promise<void> {

        let handler = this._commandHandlers[command.commandType];
        if (handler) {
            let resolvePromise: () => void;
            let promise = new Promise<void>(r => resolvePromise = r);
                let _: Promise<void> = (async () => {
                let context = ClientSideKernelInvocationContext.establish(command);
                await handler(command, context);

                if (context.command === command) {
                    context.dispose();
                } else {
                    context.complete(command);
                }

                resolvePromise();
            })();
            return promise;

        } else {
            // TODO: what's the right behaviour if there is no handler?
            return Promise.reject(`No handler found for command type ${command.commandType}`)
        }
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        // TODO: implement
        return { dispose: () => {} };
    }

    registerCommandHandler(commandType: string, handler: (envelope: KernelCommandEnvelope, context: KernelInvocationContext) => Promise<void>): void {
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
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientSideKernelInvocationContext } from "./client-side-kernel-invocation-context";
import { DisposableSubscription, KernelEventEnvelopeObserver, KernelTransport, Disposable, KernelCommand, KernelEvent, KernelEventType, KernelCommandType } from "./contracts";
import { IKernelCommandHandler, Kernel } from "./dotnet-interactive-interfaces";
import { TokenGenerator } from "./tokenGenerator";

export class ClientSideKernel implements Kernel {
    private _commandHandlers: { [commandType: string]: IKernelCommandHandler } = {};
    private readonly _eventObservers: { [token: string]: KernelEventEnvelopeObserver} = {};
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();

    // Is it worth us going to efforts to ensure that the Promise returned here accurately reflects
    // the command's progress? The only thing that actually calls this is the kernel transport, through
    // the callback set up by attachKernelToTransport, and the callback is expected to return void, so
    // nothing is ever going to look at the promise we return here.
    send(kernelCommand: { command: KernelCommand, commandType: string }): Promise<void> {

        let { command, commandType } = kernelCommand;
        let handler = this._commandHandlers[commandType];
        if (handler) {
            let resolvePromise: () => void;
            let promise = new Promise<void>(r => resolvePromise = r);
                let _: Promise<void> = (async () => {
                let context = ClientSideKernelInvocationContext.establish(kernelCommand);

                let isRootCommand = context.command === command;
                let contextEventsSubscription: Disposable = null;

                if (isRootCommand) {
                    contextEventsSubscription = context.subscribeToKernelEvents(e => this.publishEvent(e));
                }

                await handler.handle({ command, context });

                if (isRootCommand) {
                    context.dispose();
                } else {
                    context.complete(command);
                }

                if (contextEventsSubscription) {
                    contextEventsSubscription.dispose();
                }

                resolvePromise();
            })();
            return promise;

        } else {
            return Promise.reject(`No handler found for command type ${commandType}`)
        }
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        let subToken = this._tokenGenerator.GetNewToken();
        this._eventObservers[subToken] = observer;
        return {
            dispose: () => { delete this._eventObservers[subToken]; }
        };
    }

    registerCommandHandler(handler: IKernelCommandHandler): void {
        // When a registration already existed, we want to overwrite it because we want users to
        // be able to develop handlers iteratively, and it would be unhelpful for handler registration
        // for any particular command to be cumulative.
        this._commandHandlers[handler.commandType] = handler;
    }

    private publishEvent(kernelEvent: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }) {
        let keys = Object.keys(this._eventObservers);
        for (let subToken of keys) {
            let observer = this._eventObservers[subToken];
            observer({
                event: kernelEvent.event,
                eventType: <KernelEventType>kernelEvent.eventType,
                command: {
                    command: kernelEvent.command,
                    commandType: <KernelCommandType>kernelEvent.commandType
                }});
        }
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
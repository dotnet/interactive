// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelInvocationContext } from "./kernelInvocationContext";
import { TokenGenerator } from "./tokenGenerator";
import { Disposable, DisposableSubscription, KernelCommand, KernelCommandEnvelope, KernelCommandType, KernelEventEnvelope, KernelEventEnvelopeObserver } from "../interfaces/contracts";

export interface IKernelCommandInvocation {
    commandEnvelope: KernelCommandEnvelope;
    context: IKernelInvocationContext;
}

export interface IKernelCommandHandler {
    commandType: string;
    handle: (commandInvocation: IKernelCommandInvocation) => Promise<void>;
}

export interface IKernelEventObserver {
    (kernelEvent: KernelEventEnvelope): void;
}

export interface IKernelInvocationContext extends Disposable {
    subscribeToKernelEvents(observer: IKernelEventObserver): DisposableSubscription;
    complete(command: KernelCommandEnvelope): void;
    fail(message?: string): void;
    publish(kernelEvent: KernelEventEnvelope): void;
    command: KernelCommand;
    commandEnvelope: KernelCommandEnvelope;
}

export class Kernel {
    private _commandHandlers = new Map<string, IKernelCommandHandler>();
    private readonly _eventObservers: { [token: string]: KernelEventEnvelopeObserver } = {};
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();


    constructor(readonly name: string) {
    }

    private ensureCommandToken(commandEnvelope: KernelCommandEnvelope) {
        if (!commandEnvelope.token) {
            commandEnvelope.token = this._tokenGenerator.GetNewToken();
        }
    }

    // Is it worth us going to efforts to ensure that the Promise returned here accurately reflects
    // the command's progress? The only thing that actually calls this is the kernel transport, through
    // the callback set up by attachKernelToTransport, and the callback is expected to return void, so
    // nothing is ever going to look at the promise we return here.
    async send(commandEnvelope: KernelCommandEnvelope): Promise<void> {
        this.ensureCommandToken(commandEnvelope);
        let context = KernelInvocationContext.establish(commandEnvelope);
        let isRootCommand = context.command === commandEnvelope.command;
        let contextEventsSubscription: Disposable | null = null;
        if (isRootCommand) {
            contextEventsSubscription = context.subscribeToKernelEvents(e => this.publishEvent(e));
        }
        await this.handleCommand(commandEnvelope);

        if (contextEventsSubscription) {
            contextEventsSubscription.dispose();
        }
    }

    getCommandHandler(commandType: KernelCommandType): IKernelCommandHandler | undefined {
        return this._commandHandlers.get(commandType);
    }

    handleCommand(commandEnvelope: KernelCommandEnvelope): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            let { command, commandType } = commandEnvelope;
            let context = KernelInvocationContext.establish(commandEnvelope);

            let isRootCommand = context.command === command;
                       
            let handler = this.getCommandHandler(commandType);
            if (handler) {
                try {
                    await handler.handle({ commandEnvelope: commandEnvelope, context });

                    if (isRootCommand) {
                        context.complete(commandEnvelope);
                        context.dispose();
                    } else {
                        context.complete(commandEnvelope);
                    }

                   

                    resolve();
                }
                catch (e) {
                    if (isRootCommand) {
                        context.fail(e.message);
                        context.dispose();
                    }
                   
                    reject(e);
                }
            } else {
                if (isRootCommand) {
                    context.dispose();
                }
                
                reject(new Error(`No handler found for command type ${commandType}`));
            }
        });
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
        this._commandHandlers.set(handler.commandType, handler);
    }

    getTargetKernel(command: KernelCommand): Kernel | undefined {
        let targetKernelName = command.targetKernelName ?? this.name;
        return targetKernelName === this.name ? this : undefined;
    }

    private publishEvent(kernelEvent: KernelEventEnvelope) {
        let keys = Object.keys(this._eventObservers);
        for (let subToken of keys) {
            let observer = this._eventObservers[subToken];
            observer(kernelEvent);
        }
    }
}
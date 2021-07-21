// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { areCommandsTheSame, KernelInvocationContext } from "./kernelInvocationContext";
import { TokenGenerator } from "./tokenGenerator";
import * as contracts from "../interfaces/contracts";

export interface IKernelCommandInvocation {
    commandEnvelope: contracts.KernelCommandEnvelope;
    context: KernelInvocationContext;
}

export interface IKernelCommandHandler {
    commandType: string;
    handle: (commandInvocation: IKernelCommandInvocation) => Promise<void>;
}

export interface IKernelEventObserver {
    (kernelEvent: contracts.KernelEventEnvelope): void;
}

export class Kernel {
    private _commandHandlers = new Map<string, IKernelCommandHandler>();
    private readonly _eventObservers: { [token: string]: contracts.KernelEventEnvelopeObserver } = {};
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();
    public rootKernel: Kernel = this;
    public parentKernel: Kernel | null = null;

    constructor(readonly name: string) {
    }

    private ensureCommandToken(commandEnvelope: contracts.KernelCommandEnvelope) {
        if (!commandEnvelope.token) {
            let nextToken = this._tokenGenerator.GetNewToken();
            if (KernelInvocationContext.current?.commandEnvelope) {
                // a parent command exists, create a token hierarchy
                nextToken = `${KernelInvocationContext.current.commandEnvelope.token}/${nextToken}`;
            }

            commandEnvelope.token = nextToken;
        }
    }

    static get current(): Kernel | null {
        if (KernelInvocationContext.current) {
            return KernelInvocationContext.current.handlingKernel;
        }
        return null;
    }

    static get root(): Kernel | null {
        if (Kernel.current) {
            return Kernel.current.rootKernel;
        }
        return null;
    }

    // Is it worth us going to efforts to ensure that the Promise returned here accurately reflects
    // the command's progress? The only thing that actually calls this is the kernel transport, through
    // the callback set up by attachKernelToTransport, and the callback is expected to return void, so
    // nothing is ever going to look at the promise we return here.
    async send(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        this.ensureCommandToken(commandEnvelope);
        let context = KernelInvocationContext.establish(commandEnvelope);
        let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);
        let contextEventsSubscription: contracts.Disposable | null = null;
        if (isRootCommand) {
            contextEventsSubscription = context.subscribeToKernelEvents(e => {
                const message = `kernel ${this.name} saw event ${e.eventType} with token ${e.command?.token}`;
                // @ts-ignore
                devconsole?.log(message);
                return this.publishEvent(e);
            });
        }

        try {
            await this.handleCommand(commandEnvelope);
        } finally {
            if (contextEventsSubscription) {
                contextEventsSubscription.dispose();
            }
        }
    }

    getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined {
        return this._commandHandlers.get(commandType);
    }

    handleCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            let context = KernelInvocationContext.establish(commandEnvelope);
            context.handlingKernel = this;
            let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);

            let handler = this.getCommandHandler(commandEnvelope.commandType);
            if (handler) {
                try {
                    // @ts-ignore
                    devconsole.log(`kernel ${this.name} about to handle command ${commandEnvelope.commandType}`);
                    await handler.handle({ commandEnvelope: commandEnvelope, context });

                    context.complete(commandEnvelope);
                    if (isRootCommand) {
                        context.dispose();
                    }

                    // @ts-ignore
                    devconsole.log(`kernel ${this.name} done handling command ${commandEnvelope.commandType}`);
                    resolve();
                }
                catch (e) {
                    context.fail(e.message);
                    if (isRootCommand) {
                        context.dispose();
                    }

                    reject(e);
                }
            } else {
                if (isRootCommand) {
                    context.dispose();
                }

                reject(new Error(`No handler found for command type ${commandEnvelope.commandType}`));
            }
        });
    }

    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): contracts.DisposableSubscription {
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

    getTargetKernel(command: contracts.KernelCommand): Kernel | undefined {
        let targetKernelName = command.targetKernelName ?? this.name;
        return targetKernelName === this.name ? this : undefined;
    }

    private publishEvent(kernelEvent: contracts.KernelEventEnvelope) {
        let keys = Object.keys(this._eventObservers);
        for (let subToken of keys) {
            let observer = this._eventObservers[subToken];
            observer(kernelEvent);
        }
    }
}
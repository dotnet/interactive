// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { areCommandsTheSame, KernelInvocationContext } from "./kernelInvocationContext";
import { Guid, TokenGenerator } from "./tokenGenerator";
import * as contracts from "./contracts";
import { Logger } from "./logger";
import { CompositeKernel } from "./compositeKernel";
import { KernelScheduler } from "./kernelScheduler";
import { PromiseCompletionSource } from "./genericChannel";

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
    private _kernelInfo: contracts.KernelInfo;

    private _commandHandlers = new Map<string, IKernelCommandHandler>();
    private readonly _eventObservers: { [token: string]: contracts.KernelEventEnvelopeObserver } = {};
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();
    public rootKernel: Kernel = this;
    public parentKernel: CompositeKernel | null = null;
    private _scheduler?: KernelScheduler<contracts.KernelCommandEnvelope> | null = null;

    public get kernelInfo(): contracts.KernelInfo {
        return this._kernelInfo;
    }

    constructor(readonly name: string, languageName?: string, languageVersion?: string) {
        this._kernelInfo = {
            localName: name,
            languageName: languageName,
            aliases: [],
            languageVersion: languageVersion,
            supportedDirectives: [],
            supportedKernelCommands: []
        };

        this.registerCommandHandler({
            commandType: contracts.RequestKernelInfoType, handle: async invocation => {
                await this.handleRequestKernelInfo(invocation);
            }
        });
    }

    protected async handleRequestKernelInfo(invocation: IKernelCommandInvocation): Promise<void> {
        const eventEnvelope: contracts.KernelEventEnvelope = {
            eventType: contracts.KernelInfoProducedType,
            command: invocation.commandEnvelope,
            event: <contracts.KernelInfoProduced>{ kernelInfo: this._kernelInfo }
        };//?

        invocation.context.publish(eventEnvelope);
        return Promise.resolve();
    }

    private getScheduler(): KernelScheduler<contracts.KernelCommandEnvelope> {
        if (!this._scheduler) {
            this._scheduler = this.parentKernel?.getScheduler() ?? new KernelScheduler<contracts.KernelCommandEnvelope>();
        }

        return this._scheduler;
    }

    private ensureCommandTokenAndId(commandEnvelope: contracts.KernelCommandEnvelope) {
        commandEnvelope;//?
        if (!commandEnvelope.token) {
            let nextToken = this._tokenGenerator.GetNewToken();
            if (KernelInvocationContext.current?.commandEnvelope) {
                // a parent command exists, create a token hierarchy
                nextToken = KernelInvocationContext.current.commandEnvelope.token!;
            }

            commandEnvelope.token = nextToken;
        }

        if (!commandEnvelope.id) {
            commandEnvelope.id = Guid.create().toString();
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
    // the command's progress? The only thing that actually calls this is the kernel channel, through
    // the callback set up by attachKernelToChannel, and the callback is expected to return void, so
    // nothing is ever going to look at the promise we return here.
    async send(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        this.ensureCommandTokenAndId(commandEnvelope);
        let context = KernelInvocationContext.establish(commandEnvelope);
        this.getScheduler().runAsync(commandEnvelope, (value) => this.executeCommand(value));
        return context.promise;
    }

    private async executeCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        let context = KernelInvocationContext.establish(commandEnvelope);
        let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);
        let contextEventsSubscription: contracts.Disposable | null = null;
        if (isRootCommand) {
            contextEventsSubscription = context.subscribeToKernelEvents(e => {
                const message = `kernel ${this.name} saw event ${e.eventType} with token ${e.command?.token}`;
                Logger.default.info(message);
                return this.publishEvent(e);
            });
        }

        try {
            await this.handleCommand(commandEnvelope);
        }
        catch (e) {
            context.fail((<any>e)?.message || JSON.stringify(e));
        }
        finally {
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
            let context = KernelInvocationContext.establish(commandEnvelope);//?
            context.handlingKernel = this;
            let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);

            let handler = this.getCommandHandler(commandEnvelope.commandType);
            if (handler) {
                try {
                    Logger.default.info(`kernel ${this.name} about to handle command: ${JSON.stringify(commandEnvelope)}`);
                    await handler.handle({ commandEnvelope: commandEnvelope, context });

                    context.complete(commandEnvelope);
                    if (isRootCommand) {
                        context.dispose();
                    }

                    Logger.default.info(`kernel ${this.name} done handling command: ${JSON.stringify(commandEnvelope)}`);
                    resolve();
                }
                catch (e) {
                    context.fail((<any>e)?.message || JSON.stringify(e));
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

    protected canHandle(commandEnvelope: contracts.KernelCommandEnvelope) {
        if (commandEnvelope.command.targetKernelName && commandEnvelope.command.targetKernelName !== this.name) {
            return false;

        }

        if (commandEnvelope.command.destinationUri) {
            if (this.kernelInfo.uri !== commandEnvelope.command.destinationUri) {
                return false;
            }
        }

        return this.supportsCommand(commandEnvelope.commandType);
    }

    supportsCommand(commandType: contracts.KernelCommandType): boolean {
        return this._commandHandlers.has(commandType);
    }

    registerCommandHandler(handler: IKernelCommandHandler): void {
        // When a registration already existed, we want to overwrite it because we want users to
        // be able to develop handlers iteratively, and it would be unhelpful for handler registration
        // for any particular command to be cumulative.
        this._commandHandlers.set(handler.commandType, handler);
        this._kernelInfo.supportedKernelCommands = Array.from(this._commandHandlers.keys()).map(commandName => ({ name: commandName }));
    }

    getHandlingKernel(commandEnvelope: contracts.KernelCommandEnvelope): Kernel | undefined {
        let targetKernelName = commandEnvelope.command.targetKernelName ?? this.name;
        return targetKernelName === this.name ? this : undefined;
    }

    protected publishEvent(kernelEvent: contracts.KernelEventEnvelope) {
        let keys = Object.keys(this._eventObservers);
        for (let subToken of keys) {
            let observer = this._eventObservers[subToken];
            observer(kernelEvent);
        }
    }
}

export async function submitCommandAndGetResult<TEvent extends contracts.KernelEvent>(kernel: Kernel, commandEnvelope: contracts.KernelCommandEnvelope, expectedEventType: contracts.KernelEventType): Promise<TEvent> {
    let completionSource = new PromiseCompletionSource<TEvent>();
    let handled = false;
    let disposable = kernel.subscribeToKernelEvents(eventEnvelope => {
        if (eventEnvelope.command?.token === commandEnvelope.token) {
            switch (eventEnvelope.eventType) {
                case contracts.CommandFailedType:
                    if (!handled) {
                        handled = true;
                        let err = <contracts.CommandFailed>eventEnvelope.event;//?
                        completionSource.reject(err);
                    }
                    break;
                case contracts.CommandSucceededType:
                    if (areCommandsTheSame(eventEnvelope.command!, commandEnvelope)
                        && (eventEnvelope.command?.id === commandEnvelope.id)) {
                        if (!handled) {//? ($ ? eventEnvelope : {})
                            handled = true;
                            completionSource.reject('Command was handled before reporting expected result.');
                        }
                        break;
                    }
                default:
                    if (eventEnvelope.eventType === expectedEventType) {
                        handled = true;
                        let event = <TEvent>eventEnvelope.event;//? ($ ? eventEnvelope : {})
                        completionSource.resolve(event);
                    }
                    break;
            }
        }
    });

    try {
        await kernel.send(commandEnvelope);
    }
    finally {
        disposable.dispose();
    }

    return completionSource.promise;
}

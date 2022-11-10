// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelInvocationContext, areCommandsTheSame } from "./kernelInvocationContext";
import { TokenGenerator, Guid } from "./tokenGenerator";
import * as contracts from "./contracts";
import { Logger } from "./logger";
import { CompositeKernel } from "./compositeKernel";
import { KernelScheduler } from "./kernelScheduler";
import { PromiseCompletionSource } from "./promiseCompletionSource";
import * as disposables from "./disposables";
import { commandRoutingSlipContains, eventRoutingSlipContains, stampCommandRoutingSlip, stampEventRoutingSlip } from "./connection";
import * as rxjs from "rxjs";

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

export enum KernelType {
    composite,
    proxy,
    default
};

export class Kernel {
    private _kernelInfo: contracts.KernelInfo;
    private _commandHandlers = new Map<string, IKernelCommandHandler>();
    private _eventSubject = new rxjs.Subject<contracts.KernelEventEnvelope>();
    private readonly _tokenGenerator: TokenGenerator = new TokenGenerator();
    public rootKernel: Kernel = this;
    public parentKernel: CompositeKernel | null = null;
    private _scheduler?: KernelScheduler<contracts.KernelCommandEnvelope> | null = null;
    private _kernelType: KernelType = KernelType.default;

    public get kernelInfo(): contracts.KernelInfo {

        return this._kernelInfo;
    }

    public get kernelType(): KernelType {
        return this._kernelType;
    }

    protected set kernelType(value: KernelType) {
        this._kernelType = value;
    }

    public get kernelEvents(): rxjs.Observable<contracts.KernelEventEnvelope> {
        return this._eventSubject.asObservable();
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
        this._internalRegisterCommandHandler({
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

    protected ensureCommandTokenAndId(commandEnvelope: contracts.KernelCommandEnvelope) {
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
        const kernelUri = getKernelUri(this);
        if (!commandRoutingSlipContains(commandEnvelope, kernelUri)) {
            stampCommandRoutingSlip(commandEnvelope, getKernelUri(this));
        } else {
            "should not be here";//?
        }
        commandEnvelope.routingSlip;//?
        KernelInvocationContext.establish(commandEnvelope);
        return this.getScheduler().runAsync(commandEnvelope, (value) => this.executeCommand(value));
    }

    private async executeCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        let context = KernelInvocationContext.establish(commandEnvelope);
        let previousHandlingKernel = context.handlingKernel;

        try {
            await this.handleCommand(commandEnvelope);
        }
        catch (e) {
            context.fail((<any>e)?.message || JSON.stringify(e));
        }
        finally {
            context.handlingKernel = previousHandlingKernel;
        }
    }

    getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined {
        return this._commandHandlers.get(commandType);
    }

    handleCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            let context = KernelInvocationContext.establish(commandEnvelope);

            const previoudHendlingKernel = context.handlingKernel;
            context.handlingKernel = this;
            let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);

            let eventSubscription: rxjs.Subscription | undefined = undefined;//?

            if (isRootCommand) {
                this.name;//?
                Logger.default.info(`kernel ${this.name} of type ${KernelType[this.kernelType]} subscribing to context events`);
                eventSubscription = context.kernelEvents.pipe(rxjs.map(e => {
                    const message = `kernel ${this.name} of type ${KernelType[this.kernelType]} saw event ${e.eventType} with token ${e.command?.token}`;
                    message;//?
                    Logger.default.info(message);
                    const kernelUri = getKernelUri(this);
                    if (!eventRoutingSlipContains(e, kernelUri)) {
                        stampEventRoutingSlip(e, kernelUri);
                    } else {
                        "should not get here";//?
                    }
                    return e;
                }))
                    .subscribe(this.publishEvent.bind(this));
            }

            let handler = this.getCommandHandler(commandEnvelope.commandType);
            if (handler) {
                try {
                    Logger.default.info(`kernel ${this.name} about to handle command: ${JSON.stringify(commandEnvelope)}`);
                    await handler.handle({ commandEnvelope: commandEnvelope, context });
                    context.complete(commandEnvelope);
                    context.handlingKernel = previoudHendlingKernel;
                    if (isRootCommand) {
                        eventSubscription?.unsubscribe();
                        context.dispose();
                    }
                    Logger.default.info(`kernel ${this.name} done handling command: ${JSON.stringify(commandEnvelope)}`);
                    resolve();
                }
                catch (e) {
                    context.fail((<any>e)?.message || JSON.stringify(e));
                    context.handlingKernel = previoudHendlingKernel;
                    if (isRootCommand) {
                        eventSubscription?.unsubscribe();
                        context.dispose();
                    }
                    reject(e);
                }
            } else {
                context.handlingKernel = previoudHendlingKernel;
                if (isRootCommand) {
                    eventSubscription?.unsubscribe();
                    context.dispose();
                }
                reject(new Error(`No handler found for command type ${commandEnvelope.commandType}`));
            }
        });
    }

    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): disposables.DisposableSubscription {
        const sub = this._eventSubject.subscribe(observer);

        return {
            dispose: () => { sub.unsubscribe(); }
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

        const shouldNotify = !this._commandHandlers.has(handler.commandType);
        this._internalRegisterCommandHandler(handler);
        if (shouldNotify) {
            const event: contracts.KernelInfoProduced = {
                kernelInfo: this._kernelInfo,
            };
            const envelope: contracts.KernelEventEnvelope = {
                eventType: contracts.KernelInfoProducedType,
                event: event
            };
            stampEventRoutingSlip(envelope, getKernelUri(this));
            const context = KernelInvocationContext.current;

            if (context) {
                envelope.command = context.commandEnvelope;

                context.publish(envelope);
            } else {
                this.publishEvent(envelope);
            }
        }
    }

    private _internalRegisterCommandHandler(handler: IKernelCommandHandler): void {
        this._commandHandlers.set(handler.commandType, handler);
        this._kernelInfo.supportedKernelCommands = Array.from(this._commandHandlers.keys()).map(commandName => ({ name: commandName }));
    }

    protected getHandlingKernel(commandEnvelope: contracts.KernelCommandEnvelope, context?: KernelInvocationContext | null): Kernel | null {
        if (this.canHandle(commandEnvelope)) {
            return this;
        } else {
            context?.fail(`Command ${commandEnvelope.commandType} is not supported by Kernel ${this.name}`);
            return null;
        }
    }

    protected publishEvent(kernelEvent: contracts.KernelEventEnvelope) {
        this._eventSubject.next(kernelEvent);
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

export function getKernelUri(kernel: Kernel): string {
    return kernel.kernelInfo.uri ?? `kernel://local/${kernel.kernelInfo.localName}`;
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelInvocationContext } from "./kernelInvocationContext";
import * as commandsAndEvents from "./commandsAndEvents";
import { Logger } from "./logger";
import { CompositeKernel } from "./compositeKernel";
import { KernelScheduler } from "./kernelScheduler";
import { PromiseCompletionSource } from "./promiseCompletionSource";
import * as disposables from "./disposables";
import * as routingslip from "./routingslip";
import * as rxjs from "rxjs";

export interface IKernelCommandInvocation {
    commandEnvelope: commandsAndEvents.KernelCommandEnvelope;
    context: KernelInvocationContext;
}

export interface IKernelCommandHandler {
    commandType: string;
    handle: (commandInvocation: IKernelCommandInvocation) => Promise<void>;
}

export interface IKernelEventObserver {
    (kernelEvent: commandsAndEvents.KernelEventEnvelope): void;
}

export class Kernel {
    private _kernelInfo: commandsAndEvents.KernelInfo;
    private _commandHandlers = new Map<string, IKernelCommandHandler>();
    private _eventSubject = new rxjs.Subject<commandsAndEvents.KernelEventEnvelope>();
    public rootKernel: Kernel = this;
    public parentKernel: CompositeKernel | null = null;
    private _scheduler?: KernelScheduler<commandsAndEvents.KernelCommandEnvelope> | null = null;

    public get kernelInfo(): commandsAndEvents.KernelInfo {

        return this._kernelInfo;
    }

    public get kernelEvents(): rxjs.Observable<commandsAndEvents.KernelEventEnvelope> {
        return this._eventSubject.asObservable();
    }

    constructor(readonly name: string, languageName?: string, languageVersion?: string, displayName?: string) {
        this._kernelInfo = {
            isProxy: false,
            isComposite: false,
            localName: name,
            languageName: languageName,
            aliases: [],
            uri: routingslip.createKernelUri(`kernel://local/${name}`),
            languageVersion: languageVersion,
            displayName: displayName ?? name,
            supportedKernelCommands: []
        };
        this._internalRegisterCommandHandler({
            commandType: commandsAndEvents.RequestKernelInfoType, handle: async invocation => {
                await this.handleRequestKernelInfo(invocation);
            }
        });
    }

    protected async handleRequestKernelInfo(invocation: IKernelCommandInvocation): Promise<void> {
        const eventEnvelope: commandsAndEvents.KernelEventEnvelope = new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.KernelInfoProducedType,
            <commandsAndEvents.KernelInfoProduced>{ kernelInfo: this._kernelInfo },
            invocation.commandEnvelope
        );//?

        invocation.context.publish(eventEnvelope);
        return Promise.resolve();
    }

    private getScheduler(): KernelScheduler<commandsAndEvents.KernelCommandEnvelope> {
        if (!this._scheduler) {
            this._scheduler = this.parentKernel?.getScheduler() ?? new KernelScheduler<commandsAndEvents.KernelCommandEnvelope>();
        }

        return this._scheduler;
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
    async send(commandEnvelopeOrModel: commandsAndEvents.KernelCommandEnvelope | commandsAndEvents.KernelCommandEnvelopeModel): Promise<void> {
        let commandEnvelope = <commandsAndEvents.KernelCommandEnvelope>commandEnvelopeOrModel;

        if (commandsAndEvents.KernelCommandEnvelope.isKernelCommandEnvelopeModel(commandEnvelopeOrModel)) {
            Logger.default.warn(`Converting command envelope model to command envelope for backawards compatibility.`);
            commandEnvelope = commandsAndEvents.KernelCommandEnvelope.fromJson(commandEnvelopeOrModel);
        }

        const context = KernelInvocationContext.getOrCreateAmbientContext(commandEnvelope);
        if (context.commandEnvelope) {
            if (!commandsAndEvents.KernelCommandEnvelope.areCommandsTheSame(context.commandEnvelope, commandEnvelope)) {
                commandEnvelope.setParent(context.commandEnvelope);
            }
        }
        const kernelUri = getKernelUri(this);
        if (!commandEnvelope.routingSlip.contains(kernelUri)) {
            commandEnvelope.routingSlip.stampAsArrived(kernelUri);
        } else {
            Logger.default.warn(`Trying to stamp ${commandEnvelope.commandType} as arrived but uri ${kernelUri} is already present.`);
        }
        commandEnvelope.routingSlip;

        return this.getScheduler().runAsync(commandEnvelope, (value) => this.executeCommand(value).finally(() => {
            if (!commandEnvelope.routingSlip.contains(kernelUri)) {
                commandEnvelope.routingSlip.stamp(kernelUri);
            }
            else {
                Logger.default.warn(`Trying to stamp ${commandEnvelope.commandType} as completed but uri ${kernelUri} is already present.`);
            }
        }));
    }

    private async executeCommand(commandEnvelope: commandsAndEvents.KernelCommandEnvelope): Promise<void> {
        let context = KernelInvocationContext.getOrCreateAmbientContext(commandEnvelope);
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

    getCommandHandler(commandType: commandsAndEvents.KernelCommandType): IKernelCommandHandler | undefined {
        return this._commandHandlers.get(commandType);
    }

    handleCommand(commandEnvelope: commandsAndEvents.KernelCommandEnvelope): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            let context = KernelInvocationContext.getOrCreateAmbientContext(commandEnvelope);

            const previoudHendlingKernel = context.handlingKernel;
            context.handlingKernel = this;
            let isRootCommand = commandsAndEvents.KernelCommandEnvelope.areCommandsTheSame(context.commandEnvelope, commandEnvelope);

            let eventSubscription: rxjs.Subscription | undefined = undefined;

            if (isRootCommand) {
                const kernelType = (this.kernelInfo.isProxy ? "proxy" : "") + (this.kernelInfo.isComposite ? "composite" : "");
                Logger.default.info(`kernel ${this.name} of type ${kernelType} subscribing to context events`);
                eventSubscription = context.kernelEvents.pipe(rxjs.map(e => {
                    const message = `kernel ${this.name} of type ${kernelType} saw event ${e.eventType} with token ${e.command?.getToken()}`;
                    message;//?
                    Logger.default.info(message);
                    const kernelUri = getKernelUri(this);
                    if (!e.routingSlip.contains(kernelUri)) {
                        e.routingSlip.stamp(kernelUri);
                    } else {
                        "should not get here";
                    }
                    return e;
                }))
                    .subscribe(this.publishEvent.bind(this));
            }

            let handler = this.getCommandHandler(commandEnvelope.commandType);
            if (handler) {
                try {
                    Logger.default.info(`kernel ${this.name} about to handle command: ${JSON.stringify(commandEnvelope)}`);
                    await handler.handle({ commandEnvelope: commandEnvelope, context }).catch(e => {
                        Logger.default.error(`Error when handing command ${commandEnvelope}: ${e}`);
                    });
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
                // hack like there is no tomorrow
                const shouldNoop = this.shouldNoopCommand(commandEnvelope, context);
                if (shouldNoop) {
                    context.complete(commandEnvelope);
                }
                context.handlingKernel = previoudHendlingKernel;
                if (isRootCommand) {
                    eventSubscription?.unsubscribe();
                    context.dispose();
                }
                if (!shouldNoop) {
                    reject(new Error(`No handler found for command type ${commandEnvelope.commandType}`));
                } else {
                    Logger.default.warn(`kernel ${this.name} done noop handling command: ${JSON.stringify(commandEnvelope)}`);
                    resolve();
                }
            }
        });
    }

    private shouldNoopCommand(commandEnvelope: commandsAndEvents.KernelCommandEnvelope, context: KernelInvocationContext): boolean {
        let shouldNoop = false;
        switch (commandEnvelope.commandType) {
            case commandsAndEvents.RequestCompletionsType:
            case commandsAndEvents.RequestSignatureHelpType:
            case commandsAndEvents.RequestDiagnosticsType:
            case commandsAndEvents.RequestHoverTextType:
                shouldNoop = true;
                break;
            default:
                shouldNoop = false;
                break;
        }
        return shouldNoop;
    }

    subscribeToKernelEvents(observer: commandsAndEvents.KernelEventEnvelopeObserver): disposables.DisposableSubscription {
        const sub = this._eventSubject.subscribe(observer);

        return {
            dispose: () => { sub.unsubscribe(); }
        };
    }

    protected canHandle(commandEnvelope: commandsAndEvents.KernelCommandEnvelope) {
        if (commandEnvelope.command.targetKernelName && commandEnvelope.command.targetKernelName !== this.name) {
            return false;

        }

        if (commandEnvelope.command.destinationUri) {
            const normalizedUri = routingslip.createKernelUri(commandEnvelope.command.destinationUri);
            if (this.kernelInfo.uri !== normalizedUri) {
                return false;
            }
        }

        return this.supportsCommand(commandEnvelope.commandType);
    }

    supportsCommand(commandType: commandsAndEvents.KernelCommandType): boolean {
        return this._commandHandlers.has(commandType);
    }

    registerCommandHandler(handler: IKernelCommandHandler): void {
        // When a registration already existed, we want to overwrite it because we want users to
        // be able to develop handlers iteratively, and it would be unhelpful for handler registration
        // for any particular command to be cumulative.

        const shouldNotify = !this._commandHandlers.has(handler.commandType);
        this._internalRegisterCommandHandler(handler);
        if (shouldNotify) {
            const event: commandsAndEvents.KernelInfoProduced = {
                kernelInfo: this._kernelInfo,
            };
            const envelope = new commandsAndEvents.KernelEventEnvelope(
                commandsAndEvents.KernelInfoProducedType,
                event,
                KernelInvocationContext.current?.commandEnvelope
            );

            envelope.routingSlip.stamp(getKernelUri(this));
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

    protected getHandlingKernel(commandEnvelope: commandsAndEvents.KernelCommandEnvelope, context?: KernelInvocationContext | null): Kernel | null {
        if (this.canHandle(commandEnvelope)) {
            return this;
        } else {
            context?.fail(`Command ${commandEnvelope.commandType} is not supported by Kernel ${this.name}`);
            return null;
        }
    }

    protected publishEvent(kernelEvent: commandsAndEvents.KernelEventEnvelope) {
        this._eventSubject.next(kernelEvent);
    }
}

export async function submitCommandAndGetResult<TEvent extends commandsAndEvents.KernelEvent>(kernel: Kernel, commandEnvelope: commandsAndEvents.KernelCommandEnvelope, expectedEventType: commandsAndEvents.KernelEventType): Promise<TEvent> {
    let completionSource = new PromiseCompletionSource<TEvent>();
    let handled = false;
    let disposable = kernel.subscribeToKernelEvents(eventEnvelope => {
        if (eventEnvelope.command?.getToken() === commandEnvelope.getToken()) {
            switch (eventEnvelope.eventType) {
                case commandsAndEvents.CommandFailedType:
                    if (!handled) {
                        handled = true;
                        let err = <commandsAndEvents.CommandFailed>eventEnvelope.event;//?
                        completionSource.reject(err);
                    }
                    break;
                case commandsAndEvents.CommandSucceededType:
                    if (commandsAndEvents.KernelCommandEnvelope.areCommandsTheSame(eventEnvelope.command!, commandEnvelope)) {
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
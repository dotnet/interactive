// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "./commandsAndEvents";
import { Logger } from "./logger";
import { Kernel, IKernelCommandHandler, IKernelCommandInvocation, getKernelUri } from "./kernel";
import * as connection from "./connection";
import * as routingSlip from "./routingslip";
import { PromiseCompletionSource } from "./promiseCompletionSource";
import { KernelInvocationContext } from "./kernelInvocationContext";

export class ProxyKernel extends Kernel {

    constructor(override readonly name: string, private readonly _sender: connection.IKernelCommandAndEventSender, private readonly _receiver: connection.IKernelCommandAndEventReceiver, languageName?: string, languageVersion?: string) {
        super(name, languageName, languageVersion);
        this.kernelInfo.isProxy = true;
    }

    override getCommandHandler(commandType: commandsAndEvents.KernelCommandType): IKernelCommandHandler | undefined {
        return {
            commandType,
            handle: (invocation) => {
                return this._commandHandler(invocation);
            }
        };
    }

    private delegatePublication(envelope: commandsAndEvents.KernelEventEnvelope, invocationContext: KernelInvocationContext): void {
        let alreadyBeenSeen = false;
        const kernelUri = getKernelUri(this);
        if (kernelUri && !envelope.routingSlip.contains(kernelUri)) {
            envelope.routingSlip.stamp(kernelUri);
        } else {
            alreadyBeenSeen = true;
        }

        if (this.hasSameOrigin(envelope)) {
            if (!alreadyBeenSeen) {
                invocationContext.publish(envelope);
            }
        }
    }

    private hasSameOrigin(envelope: commandsAndEvents.KernelEventEnvelope): boolean {
        let commandOriginUri = envelope.command?.command?.originUri ?? this.kernelInfo.uri;
        if (commandOriginUri === this.kernelInfo.uri) {
            return true;
        }

        return commandOriginUri === null;
    }

    private updateKernelInfoFromEvent(kernelInfoProduced: commandsAndEvents.KernelInfoProduced) {
        connection.updateKernelInfo(this.kernelInfo, kernelInfoProduced.kernelInfo);
    }

    private async _commandHandler(commandInvocation: IKernelCommandInvocation): Promise<void> {
        const commandToken = commandInvocation.commandEnvelope.getOrCreateToken();
        const commandId = commandInvocation.commandEnvelope.id;
        const completionSource = new PromiseCompletionSource<commandsAndEvents.KernelEventEnvelope>();
        const command = commandInvocation.commandEnvelope;
        // fix : is this the right way? We are trying to avoid forwarding events we just did forward
        let eventSubscription = this._receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    if (envelope.eventType === commandsAndEvents.KernelInfoProducedType &&
                        (envelope.command === null || envelope.command === undefined)) {

                        const kernelInfoProduced = <commandsAndEvents.KernelInfoProduced>envelope.event;
                        kernelInfoProduced.kernelInfo;//?
                        this.kernelInfo;//?
                        if (kernelInfoProduced.kernelInfo.uri === this.kernelInfo.remoteUri) {

                            this.updateKernelInfoFromEvent(kernelInfoProduced);
                            const event = new commandsAndEvents.KernelEventEnvelope(commandsAndEvents.KernelInfoProducedType, { kernelInfo: this.kernelInfo });
                            this.publishEvent(event);
                        }
                    }
                    else if (envelope.command!.getOrCreateToken() === commandToken) {

                        Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] processing event, envelopeid=${envelope.command!.id}, commandid=${commandId}`);
                        Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] processing event, ${JSON.stringify(envelope)}`);

                        try {
                            const original = [...commandInvocation.commandEnvelope?.routingSlip.toArray() ?? []];
                            commandInvocation.commandEnvelope.routingSlip.continueWith(envelope.command!.routingSlip);
                            //envelope.command!.routingSlip = [...commandInvocation.commandEnvelope.routingSlip ?? []];//?
                            Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, command routingSlip :${original}] has changed to: ${JSON.stringify(commandInvocation.commandEnvelope.routingSlip ?? [])}`);
                        } catch (e: any) {
                            Logger.default.error(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, error ${e?.message}`);
                        }

                        switch (envelope.eventType) {
                            case commandsAndEvents.KernelInfoProducedType:
                                {
                                    const kernelInfoProduced = <commandsAndEvents.KernelInfoProduced>envelope.event;
                                    if (kernelInfoProduced.kernelInfo.uri === this.kernelInfo.remoteUri) {
                                        this.updateKernelInfoFromEvent(kernelInfoProduced);
                                        const event = new commandsAndEvents.KernelEventEnvelope(
                                            commandsAndEvents.KernelInfoProducedType,
                                            { kernelInfo: this.kernelInfo },
                                            commandInvocation.commandEnvelope
                                        );

                                        event.routingSlip.continueWith(envelope.routingSlip);

                                        this.delegatePublication(event, commandInvocation.context);
                                        this.delegatePublication(envelope, commandInvocation.context);
                                    } else {
                                        this.delegatePublication(envelope, commandInvocation.context);
                                    }
                                }
                                break;
                            case commandsAndEvents.CommandFailedType:
                            case commandsAndEvents.CommandSucceededType:
                                Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] finished, envelopeid=${envelope.command!.id}, commandid=${commandId}`);
                                if (envelope.command!.id === commandId) {
                                    Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] resolving promise, envelopeid=${envelope.command!.id}, commandid=${commandId}`);
                                    completionSource.resolve(envelope);
                                } else {
                                    Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] not resolving promise, envelopeid=${envelope.command!.id}, commandid=${commandId}`);
                                    this.delegatePublication(envelope, commandInvocation.context);
                                }
                                break;
                            default:
                                this.delegatePublication(envelope, commandInvocation.context);
                                break;
                        }
                    }
                }
            }
        });

        try {
            if (!commandInvocation.commandEnvelope.command.destinationUri || !commandInvocation.commandEnvelope.command.originUri) {
                commandInvocation.commandEnvelope.command.originUri ??= this.kernelInfo.uri;
                commandInvocation.commandEnvelope.command.destinationUri ??= this.kernelInfo.remoteUri;
            }

            commandInvocation.commandEnvelope.routingSlip;//?

            if (commandInvocation.commandEnvelope.commandType === commandsAndEvents.RequestKernelInfoType) {
                const destinationUri = this.kernelInfo.remoteUri!;
                if (commandInvocation.commandEnvelope.routingSlip.contains(destinationUri, true)) {
                    return Promise.resolve();
                }
            }
            Logger.default.info(`proxy ${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] forwarding command ${commandInvocation.commandEnvelope.commandType} to ${commandInvocation.commandEnvelope.command.destinationUri}`);
            this._sender.send(commandInvocation.commandEnvelope);
            Logger.default.info(`proxy ${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] about to await with token ${commandToken} and  commandid ${commandId}`);
            const enventEnvelope = await completionSource.promise;
            if (enventEnvelope.eventType === commandsAndEvents.CommandFailedType) {
                commandInvocation.context.fail((<commandsAndEvents.CommandFailed>enventEnvelope.event).message);
            }
            Logger.default.info(`proxy ${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] done awaiting with token ${commandToken}} and  commandid ${commandId}`);
        }
        catch (e) {
            commandInvocation.context.fail((<any>e).message);
        }
        finally {
            eventSubscription.unsubscribe();
        }
    }
}

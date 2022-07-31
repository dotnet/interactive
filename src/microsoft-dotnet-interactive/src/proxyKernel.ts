// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { Logger } from "./logger";
import { Kernel, IKernelCommandHandler, IKernelCommandInvocation, getKernelUri, KernelType } from "./kernel";
import * as connection from "./connection";
import { PromiseCompletionSource } from "./promiseCompletionSource";
import { KernelInvocationContext } from "./kernelInvocationContext";

export class ProxyKernel extends Kernel {

    constructor(override readonly name: string, private readonly _sender: connection.IKernelCommandAndEventSender, private readonly _receiver: connection.IKernelCommandAndEventReceiver) {
        super(name);
        this.kernelType = KernelType.proxy;
    }
    override getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined {
        return {
            commandType,
            handle: (invocation) => {
                return this._commandHandler(invocation);
            }
        };
    }

    private delegatePublication(envelope: contracts.KernelEventEnvelope, invocationContext: KernelInvocationContext): void {
        let alreadyBeenSeen = false;
        if (envelope.routingSlip === undefined || !envelope.routingSlip.find(e => e === getKernelUri(this))) {
            connection.tryAddUriToRoutingSlip(envelope, getKernelUri(this));
        } else {
            alreadyBeenSeen = true;
        }

        if (this.hasSameOrigin(envelope)) {
            if (!alreadyBeenSeen) {
                invocationContext.publish(envelope);
            }
        }
    }

    private hasSameOrigin(envelope: contracts.KernelEventEnvelope): boolean {
        let commandOriginUri = envelope.command?.command?.originUri ?? this.kernelInfo.uri;
        if (commandOriginUri === this.kernelInfo.uri) {
            return true;
        }

        return commandOriginUri === null;
    }

    private updateKernelInfoFromEvent(kernelInfoProduced: contracts.KernelInfoProduced) {
        this.kernelInfo.languageName = kernelInfoProduced.kernelInfo.languageName;
        this.kernelInfo.languageVersion = kernelInfoProduced.kernelInfo.languageVersion;

        const supportedDirectives = new Set<string>();
        const supportedCommands = new Set<string>();

        if (!this.kernelInfo.supportedDirectives) {
            this.kernelInfo.supportedDirectives = [];
        }

        if (!this.kernelInfo.supportedKernelCommands) {
            this.kernelInfo.supportedKernelCommands = [];
        }

        for (const supportedDirective of this.kernelInfo.supportedDirectives) {
            supportedDirectives.add(supportedDirective.name);
        }

        for (const supportedCommand of this.kernelInfo.supportedKernelCommands) {
            supportedCommands.add(supportedCommand.name);
        }

        for (const supportedDirective of kernelInfoProduced.kernelInfo.supportedDirectives) {
            if (!supportedDirectives.has(supportedDirective.name)) {
                supportedDirectives.add(supportedDirective.name);
                this.kernelInfo.supportedDirectives.push(supportedDirective);
            }
        }

        for (const supportedCommand of kernelInfoProduced.kernelInfo.supportedKernelCommands) {
            if (!supportedCommands.has(supportedCommand.name)) {
                supportedCommands.add(supportedCommand.name);
                this.kernelInfo.supportedKernelCommands.push(supportedCommand);
            }
        }
    }

    private async _commandHandler(commandInvocation: IKernelCommandInvocation): Promise<void> {
        const commandToken = commandInvocation.commandEnvelope.token;
        const commandId = commandInvocation.commandEnvelope.id;
        const completionSource = new PromiseCompletionSource<contracts.KernelEventEnvelope>();
        // fix : is this the right way? We are trying to avoid forwarding events we just did forward
        let eventSubscription = this._receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    if (envelope.eventType === contracts.KernelInfoProducedType &&
                        (envelope.command === null || envelope.command === undefined)) {
                        const kernelInfoProduced = <contracts.KernelInfoProduced>envelope.event;
                        this.updateKernelInfoFromEvent(kernelInfoProduced);
                        this.publishEvent(
                            {
                                eventType: contracts.KernelInfoProducedType,
                                event: { kernelInfo: this.kernelInfo }
                            });
                    }
                    else if (envelope.command!.token === commandToken) {

                        for (const kernelUri of envelope.command!.routingSlip!) {
                            connection.tryAddUriToRoutingSlip(commandInvocation.commandEnvelope, kernelUri);
                            envelope.command!.routingSlip = commandInvocation.commandEnvelope.routingSlip;//?
                        }

                        switch (envelope.eventType) {
                            case contracts.KernelInfoProducedType:
                                {
                                    const kernelInfoProduced = <contracts.KernelInfoProduced>envelope.event;
                                    if (kernelInfoProduced.kernelInfo.uri === this.kernelInfo.remoteUri) {
                                        this.updateKernelInfoFromEvent(kernelInfoProduced);
                                        this.delegatePublication(
                                            {
                                                eventType: contracts.KernelInfoProducedType,
                                                event: { kernelInfo: this.kernelInfo },
                                                routingSlip: envelope.routingSlip,
                                                command: commandInvocation.commandEnvelope
                                            }, commandInvocation.context);
                                        this.delegatePublication(envelope, commandInvocation.context);
                                    } else {
                                        this.delegatePublication(envelope, commandInvocation.context);
                                    }
                                }
                                break;
                            case contracts.CommandFailedType:
                            case contracts.CommandSucceededType:
                                if (envelope.command!.id === commandId) {
                                    completionSource.resolve(envelope);
                                } else {
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
                const kernelInfo = this.parentKernel?.host?.tryGetKernelInfo(this);
                if (kernelInfo) {
                    commandInvocation.commandEnvelope.command.originUri ??= kernelInfo.uri;
                    commandInvocation.commandEnvelope.command.destinationUri ??= kernelInfo.remoteUri;
                }
            }

            commandInvocation.commandEnvelope.routingSlip;//?

            this._sender.send(commandInvocation.commandEnvelope);
            Logger.default.info(`proxy ${this.name} about to await with token ${commandToken}`);
            const enventEnvelope = await completionSource.promise;
            if (enventEnvelope.eventType === contracts.CommandFailedType) {
                commandInvocation.context.fail((<contracts.CommandFailed>enventEnvelope.event).message);
            }
            Logger.default.info(`proxy ${this.name} done awaiting with token ${commandToken}`);
        }
        catch (e) {
            commandInvocation.context.fail((<any>e).message);
        }
        finally {
            eventSubscription.unsubscribe();
        }
    }
}

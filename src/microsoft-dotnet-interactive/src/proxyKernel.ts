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

    private async _commandHandler(commandInvocation: IKernelCommandInvocation): Promise<void> {
        const commandToken = commandInvocation.commandEnvelope.token;
        const commandId = commandInvocation.commandEnvelope.id;
        const completionSource = new PromiseCompletionSource<contracts.KernelEventEnvelope>();
        // fix : is this the right way? We are trying to avoid forwarding events we just did forward
        let eventSubscription = this._receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    if (envelope.command!.token === commandToken) {

                        for (const kernelUri of envelope.command!.routingSlip!) {
                            connection.tryAddUriToRoutingSlip(commandInvocation.commandEnvelope, kernelUri);
                            envelope.command!.routingSlip = commandInvocation.commandEnvelope.routingSlip;//?
                        }

                        switch (envelope.eventType) {
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

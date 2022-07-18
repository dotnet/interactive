// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { Logger } from "./logger";
import { Kernel, IKernelCommandHandler, IKernelCommandInvocation } from "./kernel";
import * as connection from "./connection";
import { PromiseCompletionSource } from "./promiseCompletionSource";
export class ProxyKernel extends Kernel {

    constructor(override readonly name: string, private readonly _sender: connection.IKernelCommandAndEventSender, private readonly _receiver: connection.IKernelCommandAndEventReceiver) {
        super(name);
    }
    override getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined {
        return {
            commandType,
            handle: (invocation) => {
                return this._commandHandler(invocation);
            }
        };
    }

    private async _commandHandler(commandInvocation: IKernelCommandInvocation): Promise<void> {
        const commandToken = commandInvocation.commandEnvelope.token;
        const commandId = commandInvocation.commandEnvelope.id;
        const completionSource = new PromiseCompletionSource<contracts.KernelEventEnvelope>();

        const handledEvents = new Set<contracts.KernelEventEnvelope>();
        let eventSubscription = this._receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    if (envelope.command!.token === commandToken) {
                        switch (envelope.eventType) {
                            case contracts.CommandFailedType:
                            case contracts.CommandSucceededType:
                                if (envelope.command!.id === commandId) {
                                    completionSource.resolve(envelope);
                                } else {
                                    if (!handledEvents.has(envelope)) {
                                        handledEvents.add(envelope);
                                        commandInvocation.context.publish(envelope);
                                    }
                                }
                                break;
                            default:
                                if (!handledEvents.has(envelope)) {
                                    handledEvents.add(envelope);
                                    commandInvocation.context.publish(envelope);
                                }
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

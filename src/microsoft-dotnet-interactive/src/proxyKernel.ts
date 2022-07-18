// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import * as logger from "./logger";
import * as kernel from "./kernel";
import * as connection from "./connection";
import * as promiseCompletionSource from "./promiseCompletionSource";

export class ProxyKernel extends kernel.Kernel {

    constructor(override readonly name: string, private readonly _sender: connection.IKernelCommandAndEventSender, private readonly _receiver: connection.IKernelCommandAndEventReceiver) {
        super(name);
    }
    override getCommandHandler(commandType: contracts.KernelCommandType): kernel.IKernelCommandHandler | undefined {
        return {
            commandType,
            handle: (invocation) => {
                return this._commandHandler(invocation);
            }
        };
    }

    private async _commandHandler(commandInvocation: kernel.IKernelCommandInvocation): Promise<void> {
        const token = commandInvocation.commandEnvelope.token;
        const completionSource = new promiseCompletionSource.PromiseCompletionSource<contracts.KernelEventEnvelope>();
        let sub = this._receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    if (envelope.command!.token === token) {
                        switch (envelope.eventType) {
                            case contracts.CommandFailedType:
                            case contracts.CommandSucceededType:
                                if (envelope.command!.id === commandInvocation.commandEnvelope.id) {
                                    completionSource.resolve(envelope);
                                } else {
                                    commandInvocation.context.publish(envelope);
                                }
                                break;
                            default:
                                commandInvocation.context.publish(envelope);
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
            logger.Logger.default.info(`proxy ${this.name} about to await with token ${token}`);
            const enventEnvelope = await completionSource.promise;
            if (enventEnvelope.eventType === contracts.CommandFailedType) {
                commandInvocation.context.fail((<contracts.CommandFailed>enventEnvelope.event).message);
            }
            logger.Logger.default.info(`proxy ${this.name} done awaiting with token ${token}`);
        }
        catch (e) {
            commandInvocation.context.fail((<any>e).message);
        }
        finally {
            sub.unsubscribe();
        }
    }
}

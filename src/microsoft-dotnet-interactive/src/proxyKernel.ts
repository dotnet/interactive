// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { Logger } from "./logger";
import { PromiseCompletionSource } from "./genericChannel";
import { IKernelCommandHandler, IKernelCommandInvocation, Kernel } from "./kernel";

export class ProxyKernel extends Kernel {

    constructor(override readonly name: string, private readonly channel: contracts.KernelCommandAndEventChannel) {
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
        const token = commandInvocation.commandEnvelope.token;
        const completionSource = new PromiseCompletionSource<contracts.KernelEventEnvelope>();
        let sub = this.channel.subscribeToKernelEvents((envelope: contracts.KernelEventEnvelope) => {
            Logger.default.info(`proxy ${this.name} got event ${JSON.stringify(envelope)}`);
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
        });

        try {
            if (!commandInvocation.commandEnvelope.command.destinationUri || !commandInvocation.commandEnvelope.command.originUri) {
                const kernelInfo = this.parentKernel?.host?.tryGetKernelInfo(this);
                if (kernelInfo) {
                    commandInvocation.commandEnvelope.command.originUri ??= kernelInfo.uri;
                    commandInvocation.commandEnvelope.command.destinationUri ??= kernelInfo.remoteUri;
                }
            }

            this.channel.submitCommand(commandInvocation.commandEnvelope);
            Logger.default.info(`proxy ${this.name} about to await with token ${token}`);
            const enventEnvelope = await completionSource.promise;
            if (enventEnvelope.eventType === contracts.CommandFailedType) {
                commandInvocation.context.fail((<contracts.CommandFailed>enventEnvelope.event).message);
            }
            Logger.default.info(`proxy ${this.name} done awaiting with token ${token}`);
        }
        catch (e) {
            commandInvocation.context.fail((<any>e).message);
        }
        finally {
            sub.dispose();
        }
    }
}

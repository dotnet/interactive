// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { Logger } from "./logger";
import { PromiseCompletionSource } from "./genericTransport";
import { IKernelCommandHandler, IKernelCommandInvocation, Kernel } from "./kernel";

export class ProxyKernel extends Kernel {

    constructor(readonly name: string, private readonly transport: contracts.Connector) {
        super(name);
    }
    getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined {
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
        let sub = this.transport.subscribeToKernelEvents((envelope: contracts.KernelEventEnvelope) => {
            Logger.default.info(`proxy ${this.name} got event ${envelope.eventType} from ${envelope.command?.command?.targetKernelName} with token ${envelope.command?.token}`);
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
            if (!commandInvocation.commandEnvelope.destinationUri || !commandInvocation.commandEnvelope.originUri) {
                const kernelInfo = this.parentKernel?.host?.tryGetKernelInfo(this);
                if (kernelInfo) {
                    commandInvocation.commandEnvelope.originUri ??= kernelInfo.originUri;
                    commandInvocation.commandEnvelope.destinationUri ??= kernelInfo.destinationUri;
                }
            }

            this.transport.submitCommand(commandInvocation.commandEnvelope);
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

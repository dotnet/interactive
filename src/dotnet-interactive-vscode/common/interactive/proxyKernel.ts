// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "../interfaces/contracts";
import { Logger } from "../logger";
import { PromiseCompletionSource } from "./genericTransport";
import { IKernelCommandHandler, IKernelCommandInvocation, Kernel } from "./kernel";

export class ProxyKernel extends Kernel {

    constructor(readonly name: string, private readonly transport: contracts.Transport) {
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
        const completionSource = new PromiseCompletionSource<boolean>();
        let sub = this.transport.subscribeToKernelEvents((envelope: contracts.KernelEventEnvelope) => {
            Logger.default.info(`proxy ${this.name} got event ${envelope.eventType} from ${envelope.command?.command?.targetKernelName} with token ${envelope.command?.token}`);
            if (envelope.command!.token === token) {
                commandInvocation.context.publish(envelope);
                if (envelope.command!.command!.id === commandInvocation.commandEnvelope.command.id) {
                    switch (envelope.eventType) {
                        case contracts.CommandFailedType:
                        case contracts.CommandSucceededType:
                            completionSource.resolve(true);
                            break;
                    }
                }
            }
        });

        try {
            this.transport.submitCommand(commandInvocation.commandEnvelope);
            Logger.default.info(`proxy ${this.name} about to await with token ${token}`);
            await completionSource.promise;
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

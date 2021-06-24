// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../interfaces/contracts';
import * as vscode from 'vscode';

export interface Kernel {
    send(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
}

export class VSCodeKernel implements Kernel {
    private _commandHandlers: Map<string, contracts.KernelCommandEnvelopeHandler> = new Map();

    constructor(private readonly transport: contracts.KernelTransport) {
        this.registerGetInputCommandHandler();
    }

    private registerGetInputCommandHandler() {
        this.registerCommandHandler(contracts.GetInputType, async commandEnvelope => {
            const getInput = <contracts.GetInput>commandEnvelope.command;
            const prompt = getInput.prompt;
            const password = getInput.isPassword;
            const value = await vscode.window.showInputBox({ prompt, password });
            await this.publishEvent({
                eventType: contracts.InputProducedType,
                event: {
                    value
                },
                command: commandEnvelope,
            });
        });
    }

    registerCommandHandler(commandType: string, handler: contracts.KernelCommandEnvelopeHandler) {
        this._commandHandlers.set(commandType, handler);
    }

    private publishEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void> {
        return this.transport.publishKernelEvent(eventEnvelope);
    }

    async send(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        const handler = this._commandHandlers.get(commandEnvelope.commandType);
        if (handler) {
            try {
                await handler(commandEnvelope);
                this.publishEvent({
                    eventType: contracts.CommandSucceededType,
                    event: {},
                    command: commandEnvelope,
                });
            } catch (e) {
                await this.publishEvent({
                    eventType: contracts.CommandFailedType,
                    event: {
                        message: '' + e,
                    },
                    command: commandEnvelope,
                });
            }
        } else {
            await this.publishEvent({
                eventType: contracts.CommandFailedType,
                event: {
                    message: `Handler not defined for command of type '${commandEnvelope.commandType}'`,
                },
                command: commandEnvelope,
            });
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../interfaces/contracts';
import * as vscode from 'vscode';
import * as vscodeLike from '../interfaces/vscode-like';
import { getNotebookSpecificLanguage, languageToCellKind } from '../interactiveNotebook';
import { IKernelCommandHandler, Kernel, KernelInvocationContext } from '../interfaces/kernel';

export class VSCodeKernel implements Kernel {
    private _commandHandlers: Map<string, IKernelCommandHandler> = new Map();
    readonly name: string;
    constructor(private readonly transport: contracts.KernelTransport, private readonly notebookUri: vscodeLike.Uri) {
        this.registerGetInputCommandHandler();
        this.registerAddCellHandler();
        this.name = "vscode";
    }
    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): contracts.DisposableSubscription {
        throw new Error('Method not implemented.');
    }

    private registerGetInputCommandHandler() {
        this.registerCommandHandler({
            commandType: contracts.GetInputType, handle: async (commandInvocation) => {
                const getInput = <contracts.GetInput>commandInvocation.command;
                const prompt = getInput.prompt;
                const password = getInput.isPassword;
                const value = await vscode.window.showInputBox({ prompt, password });
                await this.publishEvent({
                    eventType: contracts.InputProducedType,
                    event: {
                        value
                    },
                    command: { commandType: contracts.GetInputType, command: commandInvocation.command },
                });
            }
        });
    }

    private registerAddCellHandler() {
        this.registerCommandHandler({
            commandType: contracts.AddCellType, handle: async commandInvocation => {
                const addCell = <contracts.AddCell>commandInvocation.command;
                const language = addCell.language;
                const contents = addCell.contents;
                const notebookDocument = vscode.workspace.notebookDocuments.find(notebook => notebook.uri.toString() === this.notebookUri.toString());
                if (notebookDocument) {
                    const edit = new vscode.WorkspaceEdit();
                    const range = new vscode.NotebookRange(notebookDocument.cellCount, notebookDocument.cellCount);
                    const cellKind = languageToCellKind(language);
                    const notebookCellLanguage = getNotebookSpecificLanguage(language);
                    const newCell = new vscode.NotebookCellData(cellKind, contents, notebookCellLanguage);
                    edit.replaceNotebookCells(notebookDocument.uri, range, [newCell]);
                    const succeeded = await vscode.workspace.applyEdit(edit);
                    if (!succeeded) {
                        throw new Error(`Unable to add cell to notebook '${this.notebookUri.toString()}'.`);
                    }
                } else {
                    throw new Error(`Unable to get notebook document for URI '${this.notebookUri.toString()}'.`);
                }
            }
        });
    }

    registerCommandHandler(handler: IKernelCommandHandler) {
        this._commandHandlers.set(handler.commandType, handler);
    }

    private publishEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void> {
        return this.transport.publishKernelEvent(eventEnvelope);
    }

    async send(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        const handler = this._commandHandlers.get(commandEnvelope.commandType);
        if (handler) {
            try {
                await handler.handle({ command: commandEnvelope.command, context: <KernelInvocationContext><any>null });
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

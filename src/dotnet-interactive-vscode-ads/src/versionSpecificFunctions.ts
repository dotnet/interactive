// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as azdata from 'azdata';
import { CompositeKernel } from './vscode-common/dotnet-interactive';
import * as contracts from './vscode-common/dotnet-interactive/contracts';

export function getNotebookDocumentFromEditor(notebookEditor: vscode.NotebookEditor): vscode.NotebookDocument {
    return notebookEditor.document;
}

export async function replaceNotebookCells(notebookUri: vscode.Uri, range: vscode.NotebookRange, cells: vscode.NotebookCellData[]): Promise<boolean> {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCells(notebookUri, range, cells);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function replaceNotebookCellMetadata(notebookUri: vscode.Uri, cellIndex: number, newCellMetadata: { [key: string]: any }): Promise<boolean> {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(notebookUri, cellIndex, newCellMetadata);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function replaceNotebookMetadata(notebookUri: vscode.Uri, documentMetadata: { [key: string]: any }): Promise<boolean> {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookMetadata(notebookUri, documentMetadata);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export function addCommandHandlers(compositeKernel: CompositeKernel): void {
    compositeKernel.registerCommandHandler({
        commandType: contracts.RequestInputType, handle: async (commandInvocation) => {
            const requestInput = <contracts.RequestInput>commandInvocation.commandEnvelope.command;
            if (requestInput.inputTypeHint === 'connectionstring-mssql') {
                let value: string | null | undefined;
                let connection = await azdata.connection.openConnectionDialog();
                if (connection) {
                    value = await azdata.connection.getConnectionString(connection.connectionId, true);
                }

                if (!value) {
                    commandInvocation.context.fail('Input request cancelled');
                } else {
                    commandInvocation.context.publish({
                        eventType: contracts.InputProducedType,
                        event: {
                            value
                        },
                        command: commandInvocation.commandEnvelope,
                    });
                }
            }

        }
    });
}
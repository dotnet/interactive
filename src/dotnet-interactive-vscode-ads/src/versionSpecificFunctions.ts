// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as azdata from 'azdata';

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

export async function handleRequestInput(prompt: string, password: boolean, inputTypeHint: string): Promise<string | null | undefined> {
    let result: string | null | undefined;
    if (inputTypeHint === 'connectionstring') {
        let connection = await azdata.connection.openConnectionDialog();
        if (connection) {
            result = await azdata.connection.getConnectionString(connection.connectionId, true);
        }
    } else {
        result = (inputTypeHint === 'file')
            ? await vscode.window.showOpenDialog({ canSelectFiles: true, canSelectFolders: false, title: prompt, canSelectMany: false })
                .then(v => typeof v?.[0].fsPath === 'undefined' ? null : v[0].fsPath)
            : await vscode.window.showInputBox({ prompt, password });
    }
    return result;
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

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

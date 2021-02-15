// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as interfaces from 'vscode-interfaces/out/notebook';

export async function updateCellOutputs(document: vscode.NotebookDocument, cellIndex: number, outputs: Array<interfaces.NotebookCellOutput>) {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellOutput(document.uri, cellIndex, outputs.map(o => {
        return new vscode.NotebookCellOutput(o.outputs.map(oi => {
            // stable vs code doesn't support the error mime type, so we force the display to `text/plain` to ensure something shows up
            return new vscode.NotebookCellOutputItem(oi.mime === interfaces.ErrorOutputMimeType ? 'text/plain' : oi.mime, oi.value);
        }));
    }));
    await vscode.workspace.applyEdit(edit);
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from 'vscode-interfaces/out/contracts';
import * as interfaces from 'vscode-interfaces/out/notebook';
import * as utilities from 'vscode-interfaces/out/utilities';

export async function updateCellOutputs(document: vscode.NotebookDocument, cellIndex: number, outputs: Array<interfaces.NotebookCellOutput>) {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellOutput(document.uri, cellIndex, outputs.map(o => {
        return new vscode.NotebookCellOutput(o.outputs.map(oi => {
            // Stable vs code doesn't support the error mime type, so we force the display to `text/plain` to ensure something shows up.
            // To ensure we can still detect error cells, we're also stuffing the original mime type into the output's metadata.
            const mimeType = oi.mime === interfaces.ErrorOutputMimeType ? 'text/plain' : oi.mime;
            const metadata = { mimeType: oi.mime };
            return new vscode.NotebookCellOutputItem(mimeType, oi.value, metadata);
        }));
    }));
    await vscode.workspace.applyEdit(edit);
}

export async function updateNotebookCellMetadata(document: vscode.NotebookDocument, cellIndex: number, metadata: vscode.NotebookCellMetadata) {
    const cell = document.cells[cellIndex];
    const newMetadata = { ...cell.metadata, ...metadata };
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(document.uri, cellIndex, newMetadata);
    await vscode.workspace.applyEdit(edit);
}

export function vsCodeCellOutputToContractCellOutput(output: vscode.CellOutput): contracts.NotebookCellOutput {
    switch (output.outputKind) {
        case vscode.CellOutputKind.Error:
            const error: contracts.NotebookCellErrorOutput = {
                errorName: output.ename,
                errorValue: output.evalue,
                stackTrace: output.traceback
            };
            return error;
        case vscode.CellOutputKind.Rich:
            const rich: contracts.NotebookCellDisplayOutput = {
                data: output.data
            };
            return rich;
        case vscode.CellOutputKind.Text:
            const text: contracts.NotebookCellTextOutput = {
                text: output.text
            };
            return text;
    }
}

export function contractCellOutputToVsCodeCellOutput(output: contracts.NotebookCellOutput): vscode.CellOutput {
    if (utilities.isDisplayOutput(output)) {
        return {
            outputKind: vscode.CellOutputKind.Rich,
            data: output.data
        };
    } else if (utilities.isErrorOutput(output)) {
        return {
            outputKind: vscode.CellOutputKind.Error,
            ename: output.errorName,
            evalue: output.errorValue,
            traceback: output.stackTrace
        };
    } else if (utilities.isTextOutput(output)) {
        return {
            outputKind: vscode.CellOutputKind.Text,
            text: output.text
        };
    }

    // unknown, better to return _something_ than to fail entirely
    return {
        outputKind: vscode.CellOutputKind.Rich,
        data: {}
    };
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from 'dotnet-interactive-vscode-interfaces/out/contracts';
import * as interfaces from 'dotnet-interactive-vscode-interfaces/out/notebook';
import * as utilities from 'dotnet-interactive-vscode-interfaces/out/utilities';

// The current insiders build doesn't honor the error mime type, so we're temporarily reverting to `text/plain`.
// To ensure we can still detect error cells, we're also stuffing the original mime type into the output's metadata.
// https://github.com/dotnet/interactive/issues/1063
function generateVsCodeNotebookCellOutputItem(mimeType: string, value: unknown): vscode.NotebookCellOutputItem {
    const metadata = { mimeType };
    const newMimeType = mimeType === interfaces.ErrorOutputMimeType ? 'text/plain' : mimeType;
    return new vscode.NotebookCellOutputItem(newMimeType, value, metadata);
}

export async function updateCellOutputs(document: vscode.NotebookDocument, cellIndex: number, outputs: Array<interfaces.NotebookCellOutput>) {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellOutput(document.uri, cellIndex, outputs.map(o => {
        return new vscode.NotebookCellOutput(o.outputs.map(oi => generateVsCodeNotebookCellOutputItem(oi.mime, oi.value)));
    }));
    await vscode.workspace.applyEdit(edit);
}

export async function updateNotebookCellMetadata(document: vscode.NotebookDocument, cellIndex: number, metadata: vscode.NotebookCellMetadata) {
    const cell = document.cells[cellIndex];
    const newMetadata = cell.metadata.with(metadata);
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(document.uri, cellIndex, newMetadata);
    await vscode.workspace.applyEdit(edit);
}

export function vsCodeCellOutputToContractCellOutput(output: vscode.NotebookCellOutput): contracts.NotebookCellOutput {
    const errorOutputItems = output.outputs.filter(oi => oi.mime === interfaces.ErrorOutputMimeType || oi.metadata?.mimeType === interfaces.ErrorOutputMimeType);
    if (errorOutputItems.length > 0) {
        // any error-like output takes precedence
        const errorOutputItem = errorOutputItems[0];
        const error: contracts.NotebookCellErrorOutput = {
            errorName: 'Error',
            errorValue: '' + errorOutputItem.value,
            stackTrace: [],
        };
        return error;
    } else {
        //otherwise build the mime=>value dictionary
        const data: { [key: string]: any } = {};
        for (const outputItem of output.outputs) {
            data[outputItem.mime] = outputItem.value;
        }

        const cellOutput: contracts.NotebookCellDisplayOutput = {
            data,
        };

        return cellOutput;
    }
}

export function contractCellOutputToVsCodeCellOutput(output: contracts.NotebookCellOutput): vscode.NotebookCellOutput {
    const outputItems: Array<vscode.NotebookCellOutputItem> = [];
    if (utilities.isDisplayOutput(output)) {
        for (const mimeKey in output.data) {
            outputItems.push(generateVsCodeNotebookCellOutputItem(mimeKey, output.data[mimeKey]));
        }
    } else if (utilities.isErrorOutput(output)) {
        outputItems.push(generateVsCodeNotebookCellOutputItem(interfaces.ErrorOutputMimeType, output.errorValue));
    } else if (utilities.isTextOutput(output)) {
        outputItems.push(generateVsCodeNotebookCellOutputItem('text/plain', output.text));
    }

    return new vscode.NotebookCellOutput(outputItems);
}

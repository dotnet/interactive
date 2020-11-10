// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { toVsCodeDiagnostic } from "./vscodeUtilities";
import { ClientMapper } from "../clientMapper";
import { CellOutput } from '../interfaces/vscode';
import { getDiagnosticCollection } from './diagnostics';
import { getSimpleLanguage } from "../interactiveNotebook";
import { Diagnostic, DiagnosticSeverity } from "../contracts";

export class DotNetInteractiveNotebookKernel implements vscode.NotebookKernel {
    id?: string | undefined;
    label: string;
    description?: string | undefined;
    detail?: string | undefined;
    isPreferred?: boolean | undefined;
    preloads?: vscode.Uri[] | undefined;

    constructor(readonly clientMapper: ClientMapper, apiBootstrapperUri: vscode.Uri) {
        this.label = ".NET Interactive";
        this.preloads = [apiBootstrapperUri];
    }

    async executeAllCells(document: vscode.NotebookDocument): Promise<void> {
        for (let cell of document.cells) {
            await this.executeCell(document, cell);
        }
    }

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        const startTime = Date.now();
        await updateCellMetadata(document, cell, {
            runStartTime: startTime,
            runState: vscode.NotebookCellRunState.Running,
        });
        await updateCellOutputs(document, cell, []);
        let client = await this.clientMapper.getOrAddClient(document.uri);
        let source = cell.document.getText();
        function outputObserver(outputs: Array<CellOutput>) {
            updateCellOutputs(document, cell, outputs).then(() => { });
        }

        let diagnosticCollection = getDiagnosticCollection(cell.uri);

        function diagnosticObserver(diags: Array<Diagnostic>) {
            diagnosticCollection.set(cell.uri, diags.filter(d => d.severity !== DiagnosticSeverity.Hidden).map(toVsCodeDiagnostic));
        }

        return client.execute(source, getSimpleLanguage(cell.language), outputObserver, diagnosticObserver, { id: document.uri.toString() }).then(async () => {
            await updateCellMetadata(document, cell, {
                runState: vscode.NotebookCellRunState.Success,
                lastRunDuration: Date.now() - startTime,
            });
        }).catch(async () => {
            await updateCellMetadata(document, cell, {
                runState: vscode.NotebookCellRunState.Error,
                lastRunDuration: Date.now() - startTime,
            });
        });
    }

    cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell): void {
        // not supported
    }

    cancelAllCellsExecution(document: vscode.NotebookDocument): void {
        // not supported
    }
}

export async function updateCellMetadata(document: vscode.NotebookDocument, cell: vscode.NotebookCell, metadata: vscode.NotebookCellMetadata): Promise<void> {
    const cellIndex = document.cells.findIndex(c => c === cell);
    if (cellIndex >= 0) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCellMetadata(document.uri, cellIndex, metadata);
        await vscode.workspace.applyEdit(edit);
    }
}

export async function updateCellOutputs(document: vscode.NotebookDocument, cell: vscode.NotebookCell, outputs: vscode.CellOutput[]): Promise<void> {
    const cellIndex = document.cells.findIndex(c => c === cell);
    if (cellIndex >= 0) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCellOutput(document.uri, cellIndex, outputs);
        await vscode.workspace.applyEdit(edit);
    }
}

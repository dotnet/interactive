// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { toVsCodeDiagnostic } from "./vscodeUtilities";
import { ClientMapper } from "../clientMapper";
import { CellOutput } from '../interfaces/vscode';
import { getDiagnosticCollection } from './diagnostics';
import { getSimpleLanguage } from "../interactiveNotebook";
import { Diagnostic, DiagnosticSeverity } from "../contracts";
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, withDotNetKernelMetadata } from '../ipynbUtilities';

export const KernelId: string = 'dotnet-interactive';

export class DotNetInteractiveNotebookKernel implements vscode.NotebookKernel {
    id: string = KernelId;
    label: string;
    description?: string | undefined;
    detail?: string | undefined;
    isPreferred: boolean = true;
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
        }).then(async () => {
            await updateCellLanguages(document);
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

export async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);
    edit.replaceNotebookMetadata(document.uri, documentKernelMetadata);
    await vscode.workspace.applyEdit(edit);
}

export async function updateCellLanguages(document: vscode.NotebookDocument): Promise<void> {
    const documentLanguageInfo = getLanguageInfoMetadata(document.metadata);

    // update cell language
    let applyUpdate = false;
    let cellDatas: Array<vscode.NotebookCellData> = [];
    for (const cell of document.cells) {
        const cellMetadata = getDotNetMetadata(cell.metadata);
        const cellText = cell.document.getText();
        const newLanguage = getCellLanguage(cellText, cellMetadata, documentLanguageInfo, cell.language);
        const cellData = {
            cellKind: cell.cellKind,
            source: cellText,
            language: newLanguage,
            outputs: cell.outputs,
            metadata: cell.metadata,
        };
        cellDatas.push(cellData);
        applyUpdate ||= cell.language !== newLanguage;
    }

    if (applyUpdate) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCells(document.uri, 0, document.cells.length, cellDatas);
        await vscode.workspace.applyEdit(edit);
    }
}

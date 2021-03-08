// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { isInsidersBuild, toVsCodeDiagnostic } from "./vscodeUtilities";
import { ClientMapper } from "../clientMapper";
import { getDiagnosticCollection } from './diagnostics';
import { getSimpleLanguage, notebookCellLanguages } from "../interactiveNotebook";
import { Diagnostic, DiagnosticSeverity } from 'dotnet-interactive-vscode-interfaces/out/contracts';
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, withDotNetKernelMetadata } from '../ipynbUtilities';

import * as interfaces from 'dotnet-interactive-vscode-interfaces/out/notebook';
import * as vscodeInsiders from 'dotnet-interactive-vscode-insiders/out/functions';
import * as vscodeStable from 'dotnet-interactive-vscode-stable/out/functions';
import { createErrorOutput } from '../utilities';

export const KernelId: string = 'dotnet-interactive';

export class DotNetInteractiveNotebookKernel implements vscode.NotebookKernel {
    id: string = KernelId;
    label: string;
    description?: string | undefined;
    detail?: string | undefined;
    isPreferred: boolean;
    preloads?: vscode.Uri[] | undefined;
    supportedLanguages: Array<string>;

    constructor(readonly clientMapper: ClientMapper, apiBootstrapperUri: vscode.Uri, isPreferred: boolean) {
        this.label = ".NET Interactive";
        this.preloads = [apiBootstrapperUri];
        this.isPreferred = isPreferred;
        this.supportedLanguages = notebookCellLanguages;
    }

    async executeAllCells(document: vscode.NotebookDocument): Promise<void> {
        for (let cell of document.cells) {
            await this.executeCell(document, cell);
        }
    }

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        const startTime = Date.now();
        try {
            await updateCellMetadata(document, cell, {
                runStartTime: startTime,
                runState: vscode.NotebookCellRunState.Running,
            });
            await updateCellOutputs(document, cell, []);
            let client = await this.clientMapper.getOrAddClient(document.uri);
            let source = cell.document.getText();
            function outputObserver(outputs: Array<interfaces.NotebookCellOutput>) {
                updateCellOutputs(document, cell, outputs).then(() => { });
            }

            let diagnosticCollection = getDiagnosticCollection(cell.uri);

            function diagnosticObserver(diags: Array<Diagnostic>) {
                diagnosticCollection.set(cell.uri, diags.filter(d => d.severity !== DiagnosticSeverity.Hidden).map(toVsCodeDiagnostic));
            }

            return client.execute(source, getSimpleLanguage(cell.language), outputObserver, diagnosticObserver, { id: document.uri.toString() }).then(() => {
                return updateCellMetadata(document, cell, {
                    runState: vscode.NotebookCellRunState.Success,
                    lastRunDuration: Date.now() - startTime,
                });
            }).catch(() => setCellErrorState(document, cell, startTime)).then(() => {
                return updateCellLanguages(document);
            });
        } catch (err) {
            const errorOutput = createErrorOutput(`Error executing cell: ${err}`);
            await updateCellOutputs(document, cell, [errorOutput]);
            await setCellErrorState(document, cell, startTime);
            throw err;
        }
    }

    cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell): void {
        // not supported
    }

    cancelAllCellsExecution(document: vscode.NotebookDocument): void {
        // not supported
    }
}

function setCellErrorState(document: vscode.NotebookDocument, cell: vscode.NotebookCell, startTime: number): Promise<void> {
    return updateCellMetadata(document, cell, {
        runState: vscode.NotebookCellRunState.Error,
        lastRunDuration: Date.now() - startTime,
    });
}

export async function updateCellMetadata(document: vscode.NotebookDocument, cell: vscode.NotebookCell, metadata: interfaces.NotebookCellMetadata): Promise<void> {
    const cellIndex = document.cells.findIndex(c => c === cell);
    if (cellIndex >= 0) {
        if (isInsidersBuild()) {
            await vscodeInsiders.updateNotebookCellMetadata(document, cellIndex, metadata);
        } else {
            await vscodeStable.updateNotebookCellMetadata(document, cellIndex, metadata);
        }
    }
}

export async function updateCellOutputs(document: vscode.NotebookDocument, cell: vscode.NotebookCell, outputs: Array<interfaces.NotebookCellOutput>): Promise<void> {
    const cellIndex = document.cells.findIndex(c => c === cell);
    if (cellIndex >= 0) {
        if (isInsidersBuild()) {
            await vscodeInsiders.updateCellOutputs(document, cellIndex, outputs);
        } else {
            await vscodeStable.updateCellOutputs(document, cellIndex, outputs);
        }
    }
}

export async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);

    // workaround for https://github.com/microsoft/vscode/issues/115912; capture all cell data so we can re-apply it at the end
    const cellData: Array<vscode.NotebookCellData> = document.cells.map(c => {
        return {
            cellKind: c.cellKind,
            source: c.document.getText(),
            language: c.language,
            outputs: c.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            metadata: c.metadata
        };
    });

    edit.replaceNotebookMetadata(document.uri, documentKernelMetadata);

    // this is the re-application for the workaround mentioned above
    edit.replaceNotebookCells(document.uri, 0, document.cells.length, cellData);

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
            outputs: cell.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
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

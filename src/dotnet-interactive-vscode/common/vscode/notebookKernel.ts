// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { toVsCodeDiagnostic } from "./vscodeUtilities";
import { ClientMapper } from "../clientMapper";
import { getDiagnosticCollection } from './diagnostics';
import { getSimpleLanguage, notebookCellLanguages } from "../interactiveNotebook";
import { Diagnostic, DiagnosticSeverity } from '../interfaces/contracts';
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, withDotNetKernelMetadata } from '../ipynbUtilities';
import { generateVsCodeNotebookCellOutputItem } from './notebookContentProvider';

import * as versionSpecificFunctions from '../../versionSpecificFunctions';
import * as vscodeLike from '../interfaces/vscode-like';
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
            function outputObserver(outputs: Array<vscodeLike.NotebookCellOutput>) {
                updateCellOutputs(document, cell, outputs).then(() => { });
            }

            let diagnosticCollection = getDiagnosticCollection(cell.document.uri);

            function diagnosticObserver(diags: Array<Diagnostic>) {
                diagnosticCollection.set(cell.document.uri, diags.filter(d => d.severity !== DiagnosticSeverity.Hidden).map(toVsCodeDiagnostic));
            }

            return client.execute(source, getSimpleLanguage(cell.document.languageId), outputObserver, diagnosticObserver, { id: document.uri.toString() }).then(() => {
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

    cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        const startTime = cell.metadata.runStartTime || Date.now();
        return this.clientMapper.getOrAddClient(document.uri).then(client => {
            const errorOutput = createErrorOutput("Cell execution cancelled by user");
            const resultPromise = () => updateCellOutputs(document, cell, [...cell.outputs, errorOutput])
                .then(() => setCellErrorState(document, cell, startTime));
            client.cancel()
                .then(resultPromise)
                .catch(resultPromise);
        }).catch((err) => {
            const errorOutput = createErrorOutput(`Error cancelling cell: ${err}`);
            return updateCellOutputs(document, cell, [errorOutput]).then(() =>
                setCellErrorState(document, cell, startTime));
        });
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

export async function updateCellMetadata(document: vscode.NotebookDocument, cell: vscode.NotebookCell, metadata: vscodeLike.NotebookCellMetadata): Promise<void> {
    const cellIndex = document.cells.findIndex(c => c === cell);
    if (cellIndex >= 0) {
        const cell = document.cells[cellIndex];
        const newMetadata = cell.metadata.with(metadata);
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCellMetadata(document.uri, cellIndex, newMetadata);
        await vscode.workspace.applyEdit(edit);
    }
}

export async function updateCellOutputs(document: vscode.NotebookDocument, cell: vscode.NotebookCell, outputs: Array<vscodeLike.NotebookCellOutput>): Promise<void> {
    const cellIndex = document.cells.findIndex(c => c === cell);
    if (cellIndex >= 0) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCellOutput(document.uri, cellIndex, outputs.map(o => {
            return new vscode.NotebookCellOutput(o.outputs.map(oi => generateVsCodeNotebookCellOutputItem(oi.mime, oi.value)));
        }));
        await vscode.workspace.applyEdit(edit);
    }
}

export async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);

    // workaround for https://github.com/microsoft/vscode/issues/115912; capture all cell data so we can re-apply it at the end
    const cellData: Array<vscode.NotebookCellData> = document.cells.map(c => {
        return versionSpecificFunctions.createVsCodeNotebookCellData({
            cellKind: versionSpecificFunctions.getCellKind(c),
            source: c.document.getText(),
            language: c.document.languageId,
            outputs: c.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            metadata: c.metadata
        });
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
        const newLanguage = getCellLanguage(cellText, cellMetadata, documentLanguageInfo, cell.document.languageId);
        const cellData = versionSpecificFunctions.createVsCodeNotebookCellData({
            cellKind: versionSpecificFunctions.getCellKind(cell),
            source: cellText,
            language: newLanguage,
            outputs: cell.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            metadata: cell.metadata,
        });
        cellDatas.push(cellData);
        applyUpdate ||= cell.document.languageId !== newLanguage;
    }

    if (applyUpdate) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCells(document.uri, 0, document.cells.length, cellDatas);
        await vscode.workspace.applyEdit(edit);
    }
}

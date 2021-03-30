// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { ClientMapper } from "../clientMapper";
import { getSimpleLanguage, notebookCellLanguages } from "../interactiveNotebook";
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, withDotNetKernelMetadata } from '../ipynbUtilities';
import * as contracts from '../interfaces/contracts';
import * as vscodeLike from '../interfaces/vscode-like';
import * as diagnostics from './diagnostics';
import * as notebookContentProvider from './notebookContentProvider';
import * as utilities from '../utilities';
import * as vscodeUtilities from './vscodeUtilities';

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

    async executeCellsRequest(document: vscode.NotebookDocument, ranges: vscode.NotebookCellRange[]): Promise<void> {
        for (const range of ranges) {
            for (let cellIndex = range.start; cellIndex < range.end; cellIndex++) {
                const cell = document.cells[cellIndex];
                await executeCell(document, cell, this.clientMapper);
            }
        }
    }
}

export async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);

    // workaround for https://github.com/microsoft/vscode/issues/115912; capture all cell data so we can re-apply it at the end
    const cellData: Array<vscode.NotebookCellData> = document.cells.map(c => {
        return new vscode.NotebookCellData(
            c.kind,
            c.document.getText(),
            c.document.languageId,
            c.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            c.metadata,
        );
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
        const cellData = new vscode.NotebookCellData(
            cell.kind,
            cellText,
            newLanguage,
            cell.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
            cell.metadata,
        );
        cellDatas.push(cellData);
        applyUpdate ||= cell.document.languageId !== newLanguage;
    }

    if (applyUpdate) {
        const edit = new vscode.WorkspaceEdit();
        edit.replaceNotebookCells(document.uri, 0, document.cells.length, cellDatas);
        await vscode.workspace.applyEdit(edit);
    }
}

const executionTasks: Map<vscode.Uri, vscode.NotebookCellExecutionTask> = new Map();

async function executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell, clientMapper: ClientMapper): Promise<void> {
    const startTime = Date.now();
    const executionTask = vscode.notebook.createNotebookCellExecutionTask(document.uri, cell.index, KernelId);
    if (executionTask) {
        executionTasks.set(cell.document.uri, executionTask);
        try {
            executionTask.start({
                startTime,
            });
            setCellLockState(cell, true);
            executionTask.clearOutput(cell.index);
            const client = await clientMapper.getOrAddClient(document.uri);
            executionTask.token.onCancellationRequested(() => {
                const errorOutput = utilities.createErrorOutput("Cell execution cancelled by user");
                const resultPromise = () => updateCellOutputs(executionTask, cell, [...cell.outputs, errorOutput])
                    .then(() => endExecution(cell, false, Date.now() - startTime));
                client.cancel()
                    .then(resultPromise)
                    .catch(resultPromise);
            });
            const source = cell.document.getText();
            function outputObserver(outputs: Array<vscodeLike.NotebookCellOutput>) {
                updateCellOutputs(executionTask!, cell, outputs).then(() => { });
            }

            const diagnosticCollection = diagnostics.getDiagnosticCollection(cell.document.uri);

            function diagnosticObserver(diags: Array<contracts.Diagnostic>) {
                diagnosticCollection.set(cell.document.uri, diags.filter(d => d.severity !== contracts.DiagnosticSeverity.Hidden).map(vscodeUtilities.toVsCodeDiagnostic));
            }

            return client.execute(source, getSimpleLanguage(cell.document.languageId), outputObserver, diagnosticObserver, { id: document.uri.toString() }).then(() =>
                endExecution(cell, true, Date.now() - startTime)
            ).catch(() => endExecution(cell, false, Date.now() - startTime)
            ).then(() => {
                return updateCellLanguages(document);
            });
        } catch (err) {
            const errorOutput = utilities.createErrorOutput(`Error executing cell: ${err}`);
            await updateCellOutputs(executionTask, cell, [errorOutput]);
            endExecution(cell, false, Date.now() - startTime);
            throw err;
        }
    }
}

function setCellLockState(cell: vscode.NotebookCell, locked: boolean) {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(cell.notebook.uri, cell.index, cell.metadata.with({ editable: !locked }));
    return vscode.workspace.applyEdit(edit);
}

export function endExecution(cell: vscode.NotebookCell, success: boolean, duration?: number) {
    setCellLockState(cell, false);
    const executionTask = executionTasks.get(cell.document.uri);
    if (executionTask) {
        executionTasks.delete(cell.document.uri);
        executionTask.end({
            success,
            duration,
        });
    }
}

async function updateCellOutputs(executionTask: vscode.NotebookCellExecutionTask, cell: vscode.NotebookCell, outputs: Array<vscodeLike.NotebookCellOutput>): Promise<void> {
    const reshapedOutputs = outputs.map(o => new vscode.NotebookCellOutput(o.outputs.map(oi => notebookContentProvider.generateVsCodeNotebookCellOutputItem(oi.mime, oi.value))));
    await executionTask.replaceOutput(reshapedOutputs, cell.index);
}

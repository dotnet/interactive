// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as utilities from './common/utilities';
import * as notebookContentProvider from './common/vscode/notebookContentProvider';
import * as notebookKernel from './common/vscode/notebookKernel';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';

import { ClientMapper } from './common/clientMapper';

export function getCellKind(cell: vscode.NotebookCell): vscode.NotebookCellKind {
    return cell.kind;
}

export function createVsCodeNotebookCellData(cellData: { cellKind: vscodeLike.NotebookCellKind, source: string, language: string, outputs: contracts.NotebookCellOutput[], metadata: any }): vscode.NotebookCellData {
    return new vscode.NotebookCellData(
        cellData.cellKind,
        cellData.source,
        cellData.language,
        cellData.outputs.map(notebookContentProvider.contractCellOutputToVsCodeCellOutput),
        cellData.metadata,
    );
}

const executionTasks: Map<vscode.Uri, vscode.NotebookCellExecutionTask> = new Map();

export async function executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell, clientMapper: ClientMapper): Promise<void> {
    const startTime = Date.now();
    const executionTask = vscode.notebook.createNotebookCellExecutionTask(document.uri, cell.index, notebookKernel.KernelId);
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

            return client.execute(source, interactiveNotebook.getSimpleLanguage(cell.document.languageId), outputObserver, diagnosticObserver, { id: document.uri.toString() }).then(() =>
                endExecution(cell, true, Date.now() - startTime)
            ).catch(() => endExecution(cell, false, Date.now() - startTime)
            ).then(() => {
                return notebookKernel.updateCellLanguages(document);
            });
        } catch (err) {
            const errorOutput = utilities.createErrorOutput(`Error executing cell: ${err}`);
            await updateCellOutputs(executionTask, cell, [errorOutput]);
            endExecution(cell, false, Date.now() - startTime);
            throw err;
        }
    }
}

function endExecution(cell: vscode.NotebookCell, success: boolean, duration?: number) {
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

function setCellLockState(cell: vscode.NotebookCell, locked: boolean) {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(cell.notebook.uri, cell.index, cell.metadata.with({ editable: !locked }));
    return vscode.workspace.applyEdit(edit);
}

export async function cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell, clientMapper: ClientMapper): Promise<void> {
    // only required for stable
}

export async function markCellIdle(document: vscode.NotebookDocument, cell: vscode.NotebookCell) {
    endExecution(cell, false);
}

async function updateCellOutputs(executionTask: vscode.NotebookCellExecutionTask, cell: vscode.NotebookCell, outputs: Array<vscodeLike.NotebookCellOutput>): Promise<void> {
    const reshapedOutputs = outputs.map(o => new vscode.NotebookCellOutput(o.outputs.map(oi => notebookContentProvider.generateVsCodeNotebookCellOutputItem(oi.mime, oi.value))));
    await executionTask.replaceOutput(reshapedOutputs, cell.index);
}

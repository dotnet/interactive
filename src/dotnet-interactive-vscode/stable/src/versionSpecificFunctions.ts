// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as utilities from './common/utilities';
import * as notebookContentProvider from './common/vscode/notebookContentProvider';
import * as notebookKernel from './common/vscode/notebookKernel';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';

import { ClientMapper } from './common/clientMapper';

export function registerAdditionalContentProvider(context: vscode.ExtensionContext, contentProvider: vscode.NotebookContentProvider) {
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive-jupyter', contentProvider));
}

export function getCellKind(cell: vscode.NotebookCell): vscode.NotebookCellKind {
    return cell.cellKind;
}

export function createVsCodeNotebookCellData(cellData: { cellKind: vscodeLike.NotebookCellKind, source: string, language: string, outputs: contracts.NotebookCellOutput[], metadata?: vscode.NotebookCellMetadata }): vscode.NotebookCellData {
    return {
        cellKind: cellData.cellKind,
        source: cellData.source,
        language: cellData.language,
        outputs: cellData.outputs.map(notebookContentProvider.contractCellOutputToVsCodeCellOutput),
        metadata: cellData.metadata,
    };
}

export async function executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell, clientMapper: ClientMapper): Promise<void> {
    const startTime = Date.now();
    try {
        await updateCellMetadata(document, cell, {
            runStartTime: startTime,
            runState: vscode.NotebookCellRunState.Running,
        });
        await updateCellOutputs(document, cell, []);
        let client = await clientMapper.getOrAddClient(document.uri);
        let source = cell.document.getText();
        function outputObserver(outputs: Array<vscodeLike.NotebookCellOutput>) {
            updateCellOutputs(document, cell, outputs).then(() => { });
        }

        let diagnosticCollection = diagnostics.getDiagnosticCollection(cell.document.uri);

        function diagnosticObserver(diags: Array<contracts.Diagnostic>) {
            diagnosticCollection.set(cell.document.uri, diags.filter(d => d.severity !== contracts.DiagnosticSeverity.Hidden).map(vscodeUtilities.toVsCodeDiagnostic));
        }

        return client.execute(source, interactiveNotebook.getSimpleLanguage(cell.document.languageId), outputObserver, diagnosticObserver, { id: document.uri.toString() }).then(() =>
            updateCellMetadata(document, cell, {
                runState: vscode.NotebookCellRunState.Success,
                lastRunDuration: Date.now() - startTime,
            })
        ).catch(() => setCellErrorState(document, cell, startTime)).then(() => {
            return notebookKernel.updateCellLanguages(document);
        });
    } catch (err) {
        const errorOutput = utilities.createErrorOutput(`Error executing cell: ${err}`);
        await updateCellOutputs(document, cell, [errorOutput]);
        await setCellErrorState(document, cell, startTime);
        throw err;
    }
}

export async function cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell, clientMapper: ClientMapper): Promise<void> {
    const startTime = cell.metadata.runStartTime || Date.now();
    const duration = Date.now() - startTime;
    return clientMapper.getOrAddClient(document.uri).then(client => {
        const errorOutput = utilities.createErrorOutput("Cell execution cancelled by user");
        const resultPromise = () => updateCellOutputs(document, cell, [...cell.outputs, errorOutput])
            .then(() => setCellErrorState(document, cell, startTime));
        client.cancel()
            .then(resultPromise)
            .catch(resultPromise);
    }).catch((err) => {
        const errorOutput = utilities.createErrorOutput(`Error cancelling cell: ${err}`);
        return updateCellOutputs(document, cell, [errorOutput]).then(() =>
            setCellErrorState(document, cell, startTime));
    });
}

export function markCellIdle(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
    return updateCellMetadata(document, cell, { runState: vscode.NotebookCellRunState.Idle });
}

function setCellErrorState(document: vscode.NotebookDocument, cell: vscode.NotebookCell, startTime: number): Promise<void> {
    return updateCellMetadata(document, cell, {
        runState: vscode.NotebookCellRunState.Error,
        lastRunDuration: Date.now() - startTime,
    });
}

async function updateCellMetadata(document: vscode.NotebookDocument, cell: vscode.NotebookCell, metadata: vscodeLike.NotebookCellMetadata): Promise<void> {
    const newMetadata = cell.metadata.with(metadata);
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(document.uri, cell.index, newMetadata);
    await vscode.workspace.applyEdit(edit);
}

async function updateCellOutputs(document: vscode.NotebookDocument, cell: vscode.NotebookCell, outputs: Array<vscodeLike.NotebookCellOutput>): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellOutput(document.uri, cell.index, outputs.map(o => {
        return new vscode.NotebookCellOutput(o.outputs.map(oi => notebookContentProvider.generateVsCodeNotebookCellOutputItem(oi.mime, oi.value)));
    }));
    await vscode.workspace.applyEdit(edit);
}

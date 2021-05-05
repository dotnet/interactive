// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './common/clientMapper';

import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as diagnostics from './common/vscode/diagnostics';
import * as utilities from './common/utilities';
import * as versionSpecificFunctions from './versionSpecificFunctions';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';
import { getSimpleLanguage, isDotnetInteractiveLanguage, notebookCellLanguages } from './common/interactiveNotebook';
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, isDotNetNotebookMetadata, withDotNetKernelMetadata } from './common/ipynbUtilities';
import { reshapeOutputValueForVsCode } from './common/interfaces/utilities';
import { selectDotNetInteractiveKernel } from './common/vscode/commands';

const executionTasks: Map<vscode.Uri, vscode.NotebookCellExecutionTask> = new Map();

const viewType = 'dotnet-interactive';
const jupyterViewType = 'jupyter-notebook';

export class DotNetNotebookKernel {

    private disposables: { dispose(): void }[] = [];

    constructor(private readonly clientMapper: ClientMapper, preloadUris: vscode.Uri[]) {
        const preloads = preloadUris.map(uri => ({ uri }));

        // .dib execution
        const dibController = vscode.notebook.createNotebookController(
            'dotnet-interactive',
            viewType,
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        this.commonControllerInit(dibController);

        // .ipynb execution via Jupyter extension (optional)
        const jupyterController = vscode.notebook.createNotebookController(
            'dotnet-interactive-for-jupyter',
            jupyterViewType,
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        jupyterController.onDidChangeNotebookAssociation(async e => {
            // update metadata
            if (e.selected) {
                await updateNotebookMetadata(e.notebook, clientMapper);
            }
        });
        this.commonControllerInit(jupyterController);
        this.disposables.push(vscode.notebook.onDidOpenNotebookDocument(async notebook => {
            if (notebook.viewType === jupyterViewType && isDotNetNotebook(notebook)) {
                jupyterController.updateNotebookAffinity(notebook, vscode.NotebookControllerAffinity.Preferred);
                await selectDotNetInteractiveKernel();
                await updateNotebookMetadata(notebook, clientMapper);
            }
        }));
    }

    dispose(): void {
        this.disposables.forEach(d => d.dispose());
    }

    private commonControllerInit(controller: vscode.NotebookController) {
        controller.supportedLanguages = notebookCellLanguages;
        this.disposables.push(controller.onDidReceiveMessage(e => {
            const documentUri = e.editor.document.uri;
            switch (e.message.command) {
                case "getHttpApiEndpoint":
                    this.clientMapper.tryGetClient(documentUri).then(client => {
                        if (client) {
                            const uri = client.tryGetProperty<vscode.Uri>("externalUri");
                            controller.postMessage({ command: "configureFactories", endpointUri: uri?.toString() });

                            this.clientMapper.onClientCreate(documentUri, async (client) => {
                                const uri = client.tryGetProperty<vscode.Uri>("externalUri");
                                await controller.postMessage({ command: "resetFactories", endpointUri: uri?.toString() });
                            });
                        }
                    });
                    break;
            }
        }));
        this.disposables.push(controller);
    }

    private async executeHandler(cells: vscode.NotebookCell[], document: vscode.NotebookDocument, controller: vscode.NotebookController): Promise<void> {
        for (const cell of cells) {
            await this.executeCell(cell, controller);
        }
    }

    private async executeCell(cell: vscode.NotebookCell, controller: vscode.NotebookController): Promise<void> {
        const executionTask = controller.createNotebookCellExecutionTask(cell);
        if (executionTask) {
            executionTasks.set(cell.document.uri, executionTask);
            try {
                executionTask.start({
                    startTime: Date.now(),
                });
                setCellLockState(cell, true);
                executionTask.clearOutput(cell.index);
                const client = await this.clientMapper.getOrAddClient(cell.notebook.uri);
                executionTask.token.onCancellationRequested(() => {
                    const errorOutput = utilities.createErrorOutput("Cell execution cancelled by user");
                    const resultPromise = () => updateCellOutputs(executionTask, cell, [...cell.outputs, errorOutput])
                        .then(() => endExecution(cell, false));
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

                return client.execute(source, getSimpleLanguage(cell.document.languageId), outputObserver, diagnosticObserver, { id: cell.document.uri.toString() }).then(() =>
                    endExecution(cell, true)
                ).catch(() => endExecution(cell, false));
            } catch (err) {
                const errorOutput = utilities.createErrorOutput(`Error executing cell: ${err}`);
                await updateCellOutputs(executionTask, cell, [errorOutput]);
                endExecution(cell, false);
                throw err;
            }
        }
    }
}

async function updateNotebookMetadata(notebook: vscode.NotebookDocument, clientMapper: ClientMapper): Promise<void> {
    try {
        // update various metadata
        await updateDocumentKernelspecMetadata(notebook);
        await updateCellLanguages(notebook);

        // force creation of the client so we don't have to wait for the user to execute a cell to get the tool
        await clientMapper.getOrAddClient(notebook.uri);
    } catch (err) {
        vscode.window.showErrorMessage(`Failed to set document metadata for '${notebook.uri}': ${err}`);
    }
}

export async function updateCellLanguages(document: vscode.NotebookDocument): Promise<void> {
    const documentLanguageInfo = getLanguageInfoMetadata(document.metadata);

    // update cell language
    const edit = new vscode.WorkspaceEdit();
    for (let i = 0; i < document.cellCount; i++) {
        const cell = document.cellAt(i);
        const cellMetadata = getDotNetMetadata(cell.metadata);
        const cellText = cell.document.getText();
        const newLanguage = getCellLanguage(cellText, cellMetadata, documentLanguageInfo, cell.document.languageId);
        if (cell.document.languageId !== newLanguage) {
            const newCellData = new vscode.NotebookCellData(
                cell.kind,
                cellText,
                newLanguage,
                cell.outputs.concat(), // can't pass through a readonly property, so we have to make it a regular array
                cell.metadata,
            );
            edit.replaceNotebookCells(document.uri, new vscode.NotebookRange(i, i + 1), [newCellData]);
        }
    }

    await vscode.workspace.applyEdit(edit);
}

function setCellLockState(cell: vscode.NotebookCell, locked: boolean) {
    const edit = new vscode.WorkspaceEdit();
    edit.replaceNotebookCellMetadata(cell.notebook.uri, cell.index, cell.metadata.with({ editable: !locked }));
    return vscode.workspace.applyEdit(edit);
}

async function updateCellOutputs(executionTask: vscode.NotebookCellExecutionTask, cell: vscode.NotebookCell, outputs: Array<vscodeLike.NotebookCellOutput>): Promise<void> {
    const reshapedOutputs = outputs.map(o => new vscode.NotebookCellOutput(o.outputs.map(oi => generateVsCodeNotebookCellOutputItem(oi.mime, oi.value))));
    await executionTask.replaceOutput(reshapedOutputs, cell.index);
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    setCellLockState(cell, false);
    const executionTask = executionTasks.get(cell.document.uri);
    if (executionTask) {
        executionTasks.delete(cell.document.uri);
        executionTask.end({
            success,
            endTime: Date.now(),
        });
    }
}

function generateVsCodeNotebookCellOutputItem(mimeType: string, value: unknown): vscode.NotebookCellOutputItem {
    const displayValue = reshapeOutputValueForVsCode(mimeType, value);
    return new vscode.NotebookCellOutputItem(mimeType, displayValue);
}

async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);

    // workaround for https://github.com/microsoft/vscode/issues/115912; capture all cell data so we can re-apply it at the end
    const cellData: Array<vscode.NotebookCellData> = versionSpecificFunctions.getCells(document).map(c => {
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
    const range = new vscode.NotebookRange(0, document.cellCount);
    edit.replaceNotebookCells(document.uri, range, cellData);

    await vscode.workspace.applyEdit(edit);
}

function isDotNetNotebook(notebook: vscode.NotebookDocument): boolean {
    if (isDotNetNotebookMetadata(notebook.metadata)) {
        // metadata looked correct
        return true;
    }

    if (notebook.uri.scheme === 'untitled' && notebook.cellCount === 1) {
        // untitled with a single cell, check cell
        const cell = notebook.cellAt(0);
        if (isDotnetInteractiveLanguage(cell.document.languageId) && cell.document.getText() === '') {
            // language was one of ours and cell was emtpy
            return true;
        }
    }

    // doesn't look like us
    return false;
}

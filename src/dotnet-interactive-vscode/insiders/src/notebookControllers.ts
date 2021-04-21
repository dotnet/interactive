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
import { getSimpleLanguage, notebookCellLanguages } from './common/interactiveNotebook';
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, withDotNetKernelMetadata } from './common/ipynbUtilities';
import { reshapeOutputValueForVsCode } from './common/interfaces/utilities';
import { isStableBuild } from './common/vscode/vscodeUtilities';

const executionTasks: Map<vscode.Uri, vscode.NotebookCellExecutionTask> = new Map();

export class DotNetNotebookKernel {

    private disposables: { dispose(): void }[] = [];

    constructor(private readonly clientMapper: ClientMapper, preloadUris: vscode.Uri[]) {
        const preloads = preloadUris.map(uri => ({ uri }));

        // .dib execution
        const dibController = vscode.notebook.createNotebookController(
            'dotnet-interactive',
            { viewType: 'dotnet-interactive' },
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        dibController.isPreferred = true;
        this.commonControllerInit(dibController);

        // .ipynb execution via this extension
        if (isStableBuild()) {
            const ipynbController = vscode.notebook.createNotebookController(
                'dotnet-interactive-jupyter',
                { viewType: 'dotnet-interactive-jupyter' },
                '.NET Interactive',
                this.executeHandler.bind(this), // handler
                preloads
            );
            ipynbController.isPreferred = false;
            this.commonControllerInit(ipynbController);
        }

        // .ipynb execution via Jupyter extension
        const jupyterController = vscode.notebook.createNotebookController(
            'dotnet-interactive-for-jupyter',
            { viewType: 'jupyter-notebook' },
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        jupyterController.isPreferred = false;
        jupyterController.onDidChangeNotebookAssociation(async e => {
            if (e.selected) {
                try {
                    // update various metadata
                    await updateDocumentKernelspecMetadata(e.notebook);
                    await updateCellLanguages(e.notebook);

                    // force creation of the client so we don't have to wait for the user to execute a cell to get the tool
                    await clientMapper.getOrAddClient(e.notebook.uri);
                } catch (err) {
                    vscode.window.showErrorMessage(`Failed to set document metadata for '${e.notebook.uri}': ${err}`);
                }
            } else if (isStableBuild()) {
                // `e.notebook.metadata.custom` is deprecated but still used by the Jupyter extension; soon the metadata will change to something like this:
                //    e.notebook.metadata['kernelspec']?.name;
                const kernelspecName = e.notebook.metadata?.custom?.metadata?.kernelspec?.name;
                await vscodeUtilities.offerToReOpen(e.notebook, kernelspecName);
            }
        });
        this.commonControllerInit(jupyterController);
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

    private async executeHandler(cells: vscode.NotebookCell[], controller: vscode.NotebookController): Promise<void> {
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
                ).catch(() => endExecution(cell, false)
                ).then(() => {
                    return updateCellLanguages(cell.notebook);
                });
            } catch (err) {
                const errorOutput = utilities.createErrorOutput(`Error executing cell: ${err}`);
                await updateCellOutputs(executionTask, cell, [errorOutput]);
                endExecution(cell, false);
                throw err;
            }
        }
    }
}

export async function updateCellLanguages(document: vscode.NotebookDocument): Promise<void> {
    const documentLanguageInfo = getLanguageInfoMetadata(document.metadata);

    // update cell language
    let applyUpdate = false;
    let cellDatas: Array<vscode.NotebookCellData> = [];
    for (const cell of versionSpecificFunctions.getCells(document)) {
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
        edit.replaceNotebookCells(document.uri, new vscode.NotebookRange(0, document.cellCount), cellDatas);
        await vscode.workspace.applyEdit(edit);
    }
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

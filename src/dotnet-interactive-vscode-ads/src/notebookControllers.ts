// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './vscode-common/clientMapper';

import * as contracts from './vscode-common/dotnet-interactive/contracts';
import * as vscodeLike from './vscode-common/interfaces/vscode-like';
import * as diagnostics from './vscode-common/diagnostics';
import * as vscodeUtilities from './vscode-common/vscodeUtilities';
import { getSimpleLanguage, isDotnetInteractiveLanguage, jupyterViewType, notebookCellLanguages } from './vscode-common/interactiveNotebook';
import { getCellLanguage, getDotNetMetadata, getLanguageInfoMetadata, isDotNetNotebookMetadata, withDotNetKernelMetadata } from './vscode-common/ipynbUtilities';
import { reshapeOutputValueForVsCode } from './vscode-common/interfaces/utilities';
import { selectDotNetInteractiveKernelForJupyter } from './vscode-common/commands';
import { ErrorOutputCreator } from './vscode-common/interactiveClient';
import { LogEntry, Logger } from './vscode-common/dotnet-interactive/logger';
import * as notebookMessageHandler from './vscode-common/notebookMessageHandler';

const executionTasks: Map<string, vscode.NotebookCellExecution> = new Map();

const viewType = 'dotnet-interactive';
const legacyViewType = 'dotnet-interactive-legacy';

export interface DotNetNotebookKernelConfiguration {
    clientMapper: ClientMapper,
    preloadUris: vscode.Uri[],
    createErrorOutput: ErrorOutputCreator,
}

export class DotNetNotebookKernel {

    private disposables: { dispose(): void }[] = [];

    constructor(readonly config: DotNetNotebookKernelConfiguration) {
        const preloads = config.preloadUris.map(uri => new vscode.NotebookRendererScript(uri));

        // .dib execution
        const dibController = vscode.notebooks.createNotebookController(
            'dotnet-interactive',
            viewType,
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        this.commonControllerInit(dibController);

        // .dotnet-interactive execution
        const legacyController = vscode.notebooks.createNotebookController(
            'dotnet-interactive-legacy',
            legacyViewType,
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        this.commonControllerInit(legacyController);

        // .ipynb execution via Jupyter extension (optional)
        const jupyterController = vscode.notebooks.createNotebookController(
            'dotnet-interactive-for-jupyter',
            jupyterViewType,
            '.NET Interactive',
            this.executeHandler.bind(this),
            preloads
        );
        jupyterController.onDidChangeSelectedNotebooks(async e => {
            // update metadata
            if (e.selected) {
                await updateNotebookMetadata(e.notebook, this.config.clientMapper);
            }
        });
        this.commonControllerInit(jupyterController);
        this.disposables.push(vscode.workspace.onDidOpenNotebookDocument(async notebook => {
            if (isDotNetNotebook(notebook)) {
                // eagerly spin up the backing process
                const _client = await config.clientMapper.getOrAddClient(notebook.uri);

                if (notebook.notebookType === jupyterViewType) {
                    jupyterController.updateNotebookAffinity(notebook, vscode.NotebookControllerAffinity.Preferred);
                    await selectDotNetInteractiveKernelForJupyter();
                    await updateNotebookMetadata(notebook, this.config.clientMapper);
                }
            }
        }));
    }

    dispose(): void {
        this.disposables.forEach(d => d.dispose());
    }

    private uriMessageHandlerMap: Map<string, notebookMessageHandler.MessageHandler> = new Map();

    private commonControllerInit(controller: vscode.NotebookController) {
        controller.supportedLanguages = notebookCellLanguages;
        this.disposables.push(controller.onDidReceiveMessage(e => {
            const documentUri = e.editor.document.uri;
            const documentUriString = documentUri.toString();

            if (e.message.envelope) {
                let messageHandler = this.uriMessageHandlerMap.get(documentUriString);
                if (messageHandler) {
                    const envelope = <contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope><any>(e.message.envelope);
                    if (messageHandler.waitingOnMessages) {
                        let capturedMessageWaiter = messageHandler.waitingOnMessages;
                        messageHandler.waitingOnMessages = null;
                        capturedMessageWaiter.resolve(envelope);
                    } else {
                        messageHandler.envelopeQueue.push(envelope);
                    }
                }
            }

            switch (e.message.preloadCommand) {
                case '#!connect':
                    this.config.clientMapper.getOrAddClient(documentUri).then(() => {
                        notebookMessageHandler.hashBangConnect(this.config.clientMapper, this.uriMessageHandlerMap, (arg) => controller.postMessage(arg), documentUri);
                    });
                    break;
            }


            if (e.message.logEntry) {
                Logger.default.write(<LogEntry>e.message.logEntry);
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
        const executionTask = controller.createNotebookCellExecution(cell);
        if (executionTask) {
            executionTasks.set(cell.document.uri.toString(), executionTask);
            let outputUpdatePromise = Promise.resolve();
            try {
                const startTime = Date.now();
                executionTask.start(startTime);
                await executionTask.clearOutput(cell);
                const controllerErrors: vscodeLike.NotebookCellOutput[] = [];

                function outputObserver(outputs: Array<vscodeLike.NotebookCellOutput>) {
                    outputUpdatePromise = outputUpdatePromise.catch(ex => {
                        console.error('Failed to update output', ex);
                    }).finally(() => updateCellOutputs(executionTask!, [...outputs]));
                }
                const client = await this.config.clientMapper.getOrAddClient(cell.notebook.uri);
                executionTask.token.onCancellationRequested(() => {
                    client.cancel().catch(async err => {
                        // command failed to cancel
                        const cancelFailureMessage = typeof err?.message === 'string' ? <string>err.message : '' + err;
                        const errorOutput = new vscode.NotebookCellOutput(this.config.createErrorOutput(cancelFailureMessage).items.map(oi => generateVsCodeNotebookCellOutputItem(oi.data, oi.mime, oi.stream)));
                        await executionTask.appendOutput(errorOutput);
                    });
                });
                const source = cell.document.getText();
                const diagnosticCollection = diagnostics.getDiagnosticCollection(cell.document.uri);

                function diagnosticObserver(diags: Array<contracts.Diagnostic>) {
                    diagnosticCollection.set(cell.document.uri, diags.filter(d => d.severity !== contracts.DiagnosticSeverity.Hidden).map(vscodeUtilities.toVsCodeDiagnostic));
                }

                return client.execute(source, getSimpleLanguage(cell.document.languageId), outputObserver, diagnosticObserver, { id: cell.document.uri.toString() }).then(async () => {
                    await outputUpdatePromise;
                    endExecution(cell, true);
                }).catch(async () => {
                    await outputUpdatePromise;
                    endExecution(cell, false);
                });
            } catch (err) {
                const errorOutput = new vscode.NotebookCellOutput(this.config.createErrorOutput(`Error executing cell: ${err}`).items.map(oi => generateVsCodeNotebookCellOutputItem(oi.data, oi.mime, oi.stream)));
                await executionTask.appendOutput(errorOutput);
                await outputUpdatePromise;
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
    await Promise.all(document.getCells().map(async (cell) => {
        const cellMetadata = getDotNetMetadata(cell.metadata);
        const cellText = cell.document.getText();
        const newLanguage = cell.kind === vscode.NotebookCellKind.Code
            ? getCellLanguage(cellText, cellMetadata, documentLanguageInfo, cell.document.languageId)
            : 'markdown';
        if (cell.document.languageId !== newLanguage) {
            await vscode.languages.setTextDocumentLanguage(cell.document, newLanguage);
        }
    }));
}

async function updateCellOutputs(executionTask: vscode.NotebookCellExecution, outputs: Array<vscodeLike.NotebookCellOutput>): Promise<void> {
    const streamMimetypes = ['application/vnd.code.notebook.stderr', 'application/vnd.code.notebook.stdout'];
    const reshapedOutputs: vscode.NotebookCellOutput[] = [];
    outputs.forEach(async (o) => {
        if (o.items.length > 1) {
            // multi mimeType outputs should not be processed
            reshapedOutputs.push(new vscode.NotebookCellOutput(o.items));
        } else {
            // If current nad previous items are of the same stream type then append currentItem to previousOutput.
            const currentItem = generateVsCodeNotebookCellOutputItem(o.items[0].data, o.items[0].mime, o.items[0].stream);
            const previousOutput = reshapedOutputs.length ? reshapedOutputs[reshapedOutputs.length - 1] : undefined;
            const previousOutputItem = previousOutput?.items.length === 1 ? previousOutput.items[0] : undefined;

            if (previousOutput && previousOutputItem?.mime && streamMimetypes.includes(previousOutputItem?.mime) && streamMimetypes.includes(currentItem.mime)) {
                const decoder = new TextDecoder();
                const newText = `${decoder.decode(previousOutputItem.data)}${decoder.decode(currentItem.data)}`;
                const newItem = previousOutputItem.mime === 'application/vnd.code.notebook.stderr' ? vscode.NotebookCellOutputItem.stderr(newText) : vscode.NotebookCellOutputItem.stdout(newText);
                previousOutput.items[previousOutput.items.length - 1] = newItem;
            } else {
                reshapedOutputs.push(new vscode.NotebookCellOutput([currentItem]));
            }
        }
    });
    await executionTask.replaceOutput(reshapedOutputs);
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    const key = cell.document.uri.toString();
    const executionTask = executionTasks.get(key);
    if (executionTask) {
        executionTasks.delete(key);
        const endTime = Date.now();
        executionTask.end(success, endTime);
    }
}

function generateVsCodeNotebookCellOutputItem(data: Uint8Array, mime: string, stream?: 'stdout' | 'stderr'): vscode.NotebookCellOutputItem {
    const displayData = reshapeOutputValueForVsCode(data, mime);
    switch (stream) {
        case 'stdout':
            return vscode.NotebookCellOutputItem.stdout(new TextDecoder().decode(displayData));
        case 'stderr':
            return vscode.NotebookCellOutputItem.stderr(new TextDecoder().decode(displayData));
        default:
            return new vscode.NotebookCellOutputItem(displayData, mime);
    }
}

async function updateDocumentKernelspecMetadata(document: vscode.NotebookDocument): Promise<void> {
    const edit = new vscode.WorkspaceEdit();
    const documentKernelMetadata = withDotNetKernelMetadata(document.metadata);
    edit.replaceNotebookMetadata(document.uri, documentKernelMetadata);
    await vscode.workspace.applyEdit(edit);
}

function isDotNetNotebook(notebook: vscode.NotebookDocument): boolean {
    if (notebook.uri.toString().endsWith('.dib')) {
        return true;
    }

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

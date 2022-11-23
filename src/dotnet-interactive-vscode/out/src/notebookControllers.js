"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.endExecution = exports.DotNetNotebookKernel = void 0;
const vscode = require("vscode");
const contracts = require("./vscode-common/dotnet-interactive/contracts");
const diagnostics = require("./vscode-common/diagnostics");
const vscodeUtilities = require("./vscode-common/vscodeUtilities");
const utilities_1 = require("./vscode-common/interfaces/utilities");
const commands_1 = require("./vscode-common/commands");
const logger_1 = require("./vscode-common/dotnet-interactive/logger");
const connection_1 = require("./vscode-common/dotnet-interactive/connection");
const metadataUtilities = require("./vscode-common/metadataUtilities");
const constants = require("./vscode-common/constants");
const versionSpecificFunctions = require("./versionSpecificFunctions");
const executionTasks = new Map();
class DotNetNotebookKernel {
    constructor(config, tokensProvider) {
        this.config = config;
        this.tokensProvider = tokensProvider;
        this.disposables = [];
        this.uriMessageHandlerMap = new Map();
        const preloads = config.preloadUris.map(uri => new vscode.NotebookRendererScript(uri));
        // .dib execution
        const dibController = vscode.notebooks.createNotebookController(constants.NotebookControllerId, constants.NotebookViewType, '.NET Interactive', this.executeHandler.bind(this), preloads);
        this.commonControllerInit(dibController);
        // .dotnet-interactive execution
        const legacyController = vscode.notebooks.createNotebookController(constants.LegacyNotebookControllerId, constants.LegacyNotebookViewType, '.NET Interactive', this.executeHandler.bind(this), preloads);
        this.commonControllerInit(legacyController);
        // .ipynb execution via Jupyter extension (optional)
        const jupyterController = vscode.notebooks.createNotebookController(constants.JupyterNotebookControllerId, constants.JupyterViewType, '.NET Interactive', this.executeHandler.bind(this), preloads);
        jupyterController.onDidChangeSelectedNotebooks((e) => __awaiter(this, void 0, void 0, function* () {
            // update metadata
            if (e.selected) {
                yield updateNotebookMetadata(e.notebook, this.config.clientMapper);
            }
        }));
        this.commonControllerInit(jupyterController);
        this.disposables.push(vscode.workspace.onDidOpenNotebookDocument((notebook) => __awaiter(this, void 0, void 0, function* () {
            yield this.onNotebookOpen(notebook, config.clientMapper, jupyterController);
        })));
        // ...but we may have to look at already opened ones if we were activated late
        for (const notebook of vscode.workspace.notebookDocuments) {
            this.onNotebookOpen(notebook, config.clientMapper, jupyterController);
        }
        this.disposables.push(vscode.workspace.onDidOpenTextDocument((textDocument) => __awaiter(this, void 0, void 0, function* () {
            var _a;
            if (vscode.window.activeNotebookEditor) {
                const notebook = vscode.window.activeNotebookEditor.notebook;
                if (isDotNetNotebook(notebook)) {
                    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(notebook);
                    const cells = notebook.getCells();
                    const foundCell = cells.find(cell => cell.document === textDocument);
                    if (foundCell && foundCell.index > 0) {
                        // if we found the cell and it's not the first, ensure it has kernel metadata
                        const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(foundCell);
                        if (!cellMetadata.kernelName) {
                            // no kernel metadata; copy from previous cell
                            const previousCell = cells[foundCell.index - 1];
                            const previousCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(previousCell);
                            yield vscodeUtilities.setCellKernelName(foundCell, (_a = previousCellMetadata.kernelName) !== null && _a !== void 0 ? _a : notebookMetadata.kernelInfo.defaultKernelName);
                        }
                    }
                }
            }
        })));
    }
    dispose() {
        this.disposables.forEach(d => d.dispose());
    }
    onNotebookOpen(notebook, clientMapper, jupyterController) {
        return __awaiter(this, void 0, void 0, function* () {
            if (isDotNetNotebook(notebook)) {
                // prepare initial grammar
                const kernelInfos = metadataUtilities.getKernelInfosFromNotebookDocument(notebook);
                this.tokensProvider.dynamicTokenProvider.rebuildNotebookGrammar(notebook.uri, kernelInfos);
                // eagerly spin up the backing process
                const client = yield clientMapper.getOrAddClient(notebook.uri);
                client.resetExecutionCount();
                if (notebook.notebookType === constants.JupyterViewType) {
                    jupyterController.updateNotebookAffinity(notebook, vscode.NotebookControllerAffinity.Preferred);
                    yield (0, commands_1.selectDotNetInteractiveKernelForJupyter)();
                }
                yield updateNotebookMetadata(notebook, this.config.clientMapper);
            }
        });
    }
    commonControllerInit(controller) {
        controller.supportedLanguages = [constants.CellLanguageIdentifier];
        controller.supportsExecutionOrder = true;
        this.disposables.push(controller.onDidReceiveMessage(e => {
            const notebookUri = e.editor.notebook.uri;
            const notebookUriString = notebookUri.toString();
            if (e.message.envelope) {
                let messageHandler = this.uriMessageHandlerMap.get(notebookUriString);
                messageHandler === null || messageHandler === void 0 ? void 0 : messageHandler.next(e.message.envelope);
            }
            switch (e.message.preloadCommand) {
                case '#!connect':
                    this.config.clientMapper.getOrAddClient(notebookUri).then(() => {
                        const kernelInfoProduced = (e.message.kernelInfoProduced).map(e => e.event);
                        const hostUri = e.message.hostUri;
                        versionSpecificFunctions.hashBangConnect(this.config.clientMapper, hostUri, kernelInfoProduced, this.uriMessageHandlerMap, (arg) => controller.postMessage(arg), notebookUri);
                    });
                    break;
            }
            if (e.message.logEntry) {
                logger_1.Logger.default.write(e.message.logEntry);
            }
        }));
        this.disposables.push(controller);
    }
    executeHandler(cells, document, controller) {
        return __awaiter(this, void 0, void 0, function* () {
            for (const cell of cells) {
                yield this.executeCell(cell, controller);
            }
        });
    }
    executeCell(cell, controller) {
        return __awaiter(this, void 0, void 0, function* () {
            const executionTask = controller.createNotebookCellExecution(cell);
            if (executionTask) {
                executionTasks.set(cell.document.uri.toString(), executionTask);
                let outputUpdatePromise = Promise.resolve();
                try {
                    const startTime = Date.now();
                    executionTask.start(startTime);
                    executionTask.executionOrder = undefined;
                    yield executionTask.clearOutput(cell);
                    const controllerErrors = [];
                    function outputObserver(outputs) {
                        outputUpdatePromise = outputUpdatePromise.catch(ex => {
                            console.error('Failed to update output', ex);
                        }).finally(() => updateCellOutputs(executionTask, [...outputs]));
                    }
                    const client = yield this.config.clientMapper.getOrAddClient(cell.notebook.uri);
                    executionTask.token.onCancellationRequested(() => {
                        client.cancel().catch((err) => __awaiter(this, void 0, void 0, function* () {
                            // command failed to cancel
                            const cancelFailureMessage = typeof (err === null || err === void 0 ? void 0 : err.message) === 'string' ? err.message : '' + err;
                            const errorOutput = new vscode.NotebookCellOutput(this.config.createErrorOutput(cancelFailureMessage).items.map(oi => generateVsCodeNotebookCellOutputItem(oi.data, oi.mime, oi.stream)));
                            yield executionTask.appendOutput(errorOutput);
                        }));
                    });
                    const source = cell.document.getText();
                    const diagnosticCollection = diagnostics.getDiagnosticCollection(cell.document.uri);
                    function diagnosticObserver(diags) {
                        diagnosticCollection.set(cell.document.uri, diags.filter(d => d.severity !== contracts.DiagnosticSeverity.Hidden).map(vscodeUtilities.toVsCodeDiagnostic));
                    }
                    return client.execute(source, vscodeUtilities.getCellKernelName(cell), outputObserver, diagnosticObserver, { id: cell.document.uri.toString() }).then((success) => __awaiter(this, void 0, void 0, function* () {
                        yield outputUpdatePromise;
                        endExecution(client, cell, success);
                    })).catch(() => __awaiter(this, void 0, void 0, function* () {
                        yield outputUpdatePromise;
                        endExecution(client, cell, false);
                    }));
                }
                catch (err) {
                    const errorOutput = new vscode.NotebookCellOutput(this.config.createErrorOutput(`Error executing cell: ${err}`).items.map(oi => generateVsCodeNotebookCellOutputItem(oi.data, oi.mime, oi.stream)));
                    yield executionTask.appendOutput(errorOutput);
                    yield outputUpdatePromise;
                    endExecution(undefined, cell, false);
                    throw err;
                }
            }
        });
    }
}
exports.DotNetNotebookKernel = DotNetNotebookKernel;
function updateNotebookMetadata(notebook, clientMapper) {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            // update various metadata
            yield updateDocumentKernelspecMetadata(notebook);
            yield updateCellLanguagesAndKernels(notebook);
            // force creation of the client so we don't have to wait for the user to execute a cell to get the tool
            const client = yield clientMapper.getOrAddClient(notebook.uri);
            yield updateKernelInfoMetadata(client, notebook);
        }
        catch (err) {
            vscode.window.showErrorMessage(`Failed to set document metadata for '${notebook.uri}': ${err}`);
        }
    });
}
function updateKernelInfoMetadata(client, document) {
    return __awaiter(this, void 0, void 0, function* () {
        const isIpynb = metadataUtilities.isIpynbNotebook(document);
        client.channel.receiver.subscribe({
            next: (commandOrEventEnvelope) => __awaiter(this, void 0, void 0, function* () {
                if ((0, connection_1.isKernelEventEnvelope)(commandOrEventEnvelope) && commandOrEventEnvelope.eventType === contracts.KernelInfoProducedType) {
                    // got info about a kernel; either update an existing entry, or add a new one
                    let metadataChanged = false;
                    const kernelInfoProduced = commandOrEventEnvelope.event;
                    const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
                    for (const item of notebookMetadata.kernelInfo.items) {
                        if (item.name === kernelInfoProduced.kernelInfo.localName) {
                            metadataChanged = true;
                            item.languageName = kernelInfoProduced.kernelInfo.languageName;
                            item.aliases = kernelInfoProduced.kernelInfo.aliases;
                        }
                    }
                    if (!metadataChanged) {
                        // nothing changed, must be a new kernel
                        notebookMetadata.kernelInfo.items.push({
                            name: kernelInfoProduced.kernelInfo.localName,
                            languageName: kernelInfoProduced.kernelInfo.languageName,
                            aliases: kernelInfoProduced.kernelInfo.aliases
                        });
                    }
                    const existingRawNotebookDocumentMetadata = document.metadata;
                    const updatedRawNotebookDocumentMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(notebookMetadata, isIpynb);
                    const newRawNotebookDocumentMetadata = metadataUtilities.mergeRawMetadata(existingRawNotebookDocumentMetadata, updatedRawNotebookDocumentMetadata);
                    yield versionSpecificFunctions.replaceNotebookMetadata(document.uri, newRawNotebookDocumentMetadata);
                }
            })
        });
        const notebookDocumentMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
        const kernelNotebokMetadata = metadataUtilities.getNotebookDocumentMetadataFromCompositeKernel(client.kernel);
        const mergedMetadata = metadataUtilities.mergeNotebookDocumentMetadata(notebookDocumentMetadata, kernelNotebokMetadata);
        const rawNotebookDocumentMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(mergedMetadata, isIpynb);
        yield versionSpecificFunctions.replaceNotebookMetadata(document.uri, rawNotebookDocumentMetadata);
    });
}
function updateCellLanguagesAndKernels(document) {
    return __awaiter(this, void 0, void 0, function* () {
        const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
        // update cell language and kernel
        yield Promise.all(document.getCells().map((cell) => __awaiter(this, void 0, void 0, function* () {
            const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
            yield vscodeUtilities.ensureCellLanguage(cell);
            if (!cellMetadata.kernelName) {
                // no kernel specified; apply global
                vscodeUtilities.setCellKernelName(cell, notebookMetadata.kernelInfo.defaultKernelName);
            }
        })));
    });
}
function updateCellOutputs(executionTask, outputs) {
    return __awaiter(this, void 0, void 0, function* () {
        const streamMimetypes = ['application/vnd.code.notebook.stderr', 'application/vnd.code.notebook.stdout'];
        const reshapedOutputs = [];
        outputs.forEach((o) => __awaiter(this, void 0, void 0, function* () {
            if (o.items.length > 1) {
                // multi mimeType outputs should not be processed
                reshapedOutputs.push(new vscode.NotebookCellOutput(o.items));
            }
            else {
                // If current nad previous items are of the same stream type then append currentItem to previousOutput.
                const currentItem = generateVsCodeNotebookCellOutputItem(o.items[0].data, o.items[0].mime, o.items[0].stream);
                const previousOutput = reshapedOutputs.length ? reshapedOutputs[reshapedOutputs.length - 1] : undefined;
                const previousOutputItem = (previousOutput === null || previousOutput === void 0 ? void 0 : previousOutput.items.length) === 1 ? previousOutput.items[0] : undefined;
                if (previousOutput && (previousOutputItem === null || previousOutputItem === void 0 ? void 0 : previousOutputItem.mime) && streamMimetypes.includes(previousOutputItem === null || previousOutputItem === void 0 ? void 0 : previousOutputItem.mime) && streamMimetypes.includes(currentItem.mime)) {
                    const decoder = new TextDecoder();
                    const newText = `${decoder.decode(previousOutputItem.data)}${decoder.decode(currentItem.data)}`;
                    const newItem = previousOutputItem.mime === 'application/vnd.code.notebook.stderr' ? vscode.NotebookCellOutputItem.stderr(newText) : vscode.NotebookCellOutputItem.stdout(newText);
                    previousOutput.items[previousOutput.items.length - 1] = newItem;
                }
                else {
                    reshapedOutputs.push(new vscode.NotebookCellOutput([currentItem]));
                }
            }
        }));
        yield executionTask.replaceOutput(reshapedOutputs);
    });
}
function endExecution(client, cell, success) {
    const key = cell.document.uri.toString();
    const executionTask = executionTasks.get(key);
    if (executionTask) {
        executionTasks.delete(key);
        executionTask.executionOrder = client === null || client === void 0 ? void 0 : client.getNextExecutionCount();
        const endTime = Date.now();
        executionTask.end(success, endTime);
    }
}
exports.endExecution = endExecution;
function generateVsCodeNotebookCellOutputItem(data, mime, stream) {
    const displayData = (0, utilities_1.reshapeOutputValueForVsCode)(data, mime);
    switch (stream) {
        case 'stdout':
            return vscode.NotebookCellOutputItem.stdout(new TextDecoder().decode(displayData));
        case 'stderr':
            return vscode.NotebookCellOutputItem.stderr(new TextDecoder().decode(displayData));
        default:
            return new vscode.NotebookCellOutputItem(displayData, mime);
    }
}
function updateDocumentKernelspecMetadata(document) {
    return __awaiter(this, void 0, void 0, function* () {
        const documentMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(document);
        const newMetadata = metadataUtilities.createNewIpynbMetadataWithNotebookDocumentMetadata(document.metadata, documentMetadata);
        yield versionSpecificFunctions.replaceNotebookMetadata(document.uri, newMetadata);
    });
}
function isDotNetNotebook(notebook) {
    const notebookUriString = notebook.uri.toString();
    if (notebookUriString.endsWith('.dib') || notebook.uri.fsPath.endsWith('.dib')) {
        return true;
    }
    const kernelspecMetadata = metadataUtilities.getKernelspecMetadataFromIpynbNotebookDocument(notebook);
    if (kernelspecMetadata.name.startsWith('.net-')) {
        return true;
    }
    // doesn't look like us
    return false;
}
//# sourceMappingURL=notebookControllers.js.map
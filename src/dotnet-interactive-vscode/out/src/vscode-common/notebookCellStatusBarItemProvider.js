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
exports.registerNotbookCellStatusBarItemProvider = void 0;
const vscode = require("vscode");
const contracts = require("./dotnet-interactive/contracts");
const metadataUtilities = require("./metadataUtilities");
const versionSpecificFunctions = require("../versionSpecificFunctions");
const dotnet_interactive_1 = require("./dotnet-interactive");
const kernelSelectorUtilities = require("./kernelSelectorUtilities");
const constants = require("./constants");
const vscodeUtilities = require("./vscodeUtilities");
const selectKernelCommandName = 'polyglot-notebook.selectCellKernel';
function registerNotbookCellStatusBarItemProvider(context, clientMapper) {
    const cellItemProvider = new DotNetNotebookCellStatusBarItemProvider(clientMapper);
    clientMapper.onClientCreate((_uri, client) => {
        client.channel.receiver.subscribe({
            next: envelope => {
                if ((0, dotnet_interactive_1.isKernelEventEnvelope)(envelope) && envelope.eventType === contracts.KernelInfoProducedType) {
                    cellItemProvider.updateKernelDisplayNames();
                }
            }
        });
    });
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.NotebookViewType, cellItemProvider));
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.JupyterViewType, cellItemProvider)); // TODO: fix this
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.LegacyNotebookViewType, cellItemProvider));
    context.subscriptions.push(vscode.commands.registerCommand(selectKernelCommandName, (cell) => __awaiter(this, void 0, void 0, function* () {
        if (cell) {
            const client = yield clientMapper.tryGetClient(cell.notebook.uri);
            if (client) {
                const availableOptions = kernelSelectorUtilities.getKernelSelectorOptions(client.kernel, cell.notebook, contracts.SubmitCodeType);
                const availableDisplayOptions = availableOptions.map(o => o.displayValue);
                const selectedDisplayOption = yield vscode.window.showQuickPick(availableDisplayOptions, { title: 'Select kernel' });
                if (selectedDisplayOption) {
                    const selectedValueIndex = availableDisplayOptions.indexOf(selectedDisplayOption);
                    if (selectedValueIndex >= 0) {
                        const selectedKernelData = availableOptions[selectedValueIndex];
                        const codeCell = yield vscodeUtilities.ensureCellKernelKind(cell, vscode.NotebookCellKind.Code);
                        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
                        if (notebookCellMetadata.kernelName !== selectedKernelData.kernelName) {
                            notebookCellMetadata.kernelName = selectedKernelData.kernelName;
                            const newRawMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
                            const mergedMetadata = metadataUtilities.mergeRawMetadata(cell.metadata, newRawMetadata);
                            const _succeeded = yield versionSpecificFunctions.replaceNotebookCellMetadata(codeCell.notebook.uri, codeCell.index, mergedMetadata);
                            yield vscode.commands.executeCommand('polyglot-notebook.refreshSemanticTokens');
                        }
                    }
                }
            }
        }
    })));
}
exports.registerNotbookCellStatusBarItemProvider = registerNotbookCellStatusBarItemProvider;
function getNotebookDcoumentFromCellDocument(cellDocument) {
    const notebookDocument = vscode.workspace.notebookDocuments.find(notebook => notebook.getCells().some(cell => cell.document === cellDocument));
    return notebookDocument;
}
class DotNetNotebookCellStatusBarItemProvider {
    constructor(clientMapper) {
        this.clientMapper = clientMapper;
        this._onDidChangeCellStatusBarItemsEmitter = new vscode.EventEmitter();
        this.onDidChangeCellStatusBarItems = this._onDidChangeCellStatusBarItemsEmitter.event;
    }
    provideCellStatusBarItems(cell, token) {
        var _a;
        return __awaiter(this, void 0, void 0, function* () {
            if (!metadataUtilities.isDotNetNotebook(cell.notebook)) {
                return [];
            }
            let displayText;
            if (cell.document.languageId === 'markdown') {
                displayText = 'Markdown';
            }
            else {
                const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
                const cellKernelName = (_a = cellMetadata.kernelName) !== null && _a !== void 0 ? _a : 'csharp';
                const notebookDocument = getNotebookDcoumentFromCellDocument(cell.document);
                const client = yield this.clientMapper.tryGetClient(notebookDocument.uri); // don't force client creation
                if (client) {
                    const matchingKernel = client.kernel.childKernels.find(k => k.kernelInfo.localName === cellKernelName);
                    displayText = matchingKernel ? kernelSelectorUtilities.getKernelInfoDisplayValue(matchingKernel.kernelInfo) : cellKernelName;
                }
                else {
                    displayText = cellKernelName;
                }
            }
            const item = new vscode.NotebookCellStatusBarItem(displayText, vscode.NotebookCellStatusBarAlignment.Right);
            const command = {
                title: '<unused>',
                command: selectKernelCommandName,
                arguments: [],
            };
            item.command = command;
            return [item];
        });
    }
    updateKernelDisplayNames() {
        this._onDidChangeCellStatusBarItemsEmitter.fire();
    }
}
//# sourceMappingURL=notebookCellStatusBarItemProvider.js.map
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
exports.hashBangConnect = exports.handleCustomInputRequest = exports.replaceNotebookMetadata = exports.replaceNotebookCellMetadata = exports.replaceNotebookCells = exports.getNotebookDocumentFromEditor = void 0;
const vscode = require("vscode");
const logger_1 = require("./vscode-common/dotnet-interactive/logger");
const dotnet_interactive_1 = require("./vscode-common/dotnet-interactive");
const rxjs = require("rxjs");
const connection = require("./vscode-common/dotnet-interactive/connection");
const contracts = require("./vscode-common/dotnet-interactive/contracts");
function getNotebookDocumentFromEditor(notebookEditor) {
    return notebookEditor.notebook;
}
exports.getNotebookDocumentFromEditor = getNotebookDocumentFromEditor;
function replaceNotebookCells(notebookUri, range, cells) {
    return __awaiter(this, void 0, void 0, function* () {
        const notebookEdit = vscode.NotebookEdit.replaceCells(range, cells);
        const edit = new vscode.WorkspaceEdit();
        edit.set(notebookUri, [notebookEdit]);
        const succeeded = yield vscode.workspace.applyEdit(edit);
        return succeeded;
    });
}
exports.replaceNotebookCells = replaceNotebookCells;
function replaceNotebookCellMetadata(notebookUri, cellIndex, newCellMetadata) {
    return __awaiter(this, void 0, void 0, function* () {
        const notebookEdit = vscode.NotebookEdit.updateCellMetadata(cellIndex, newCellMetadata);
        const edit = new vscode.WorkspaceEdit();
        edit.set(notebookUri, [notebookEdit]);
        const succeeded = yield vscode.workspace.applyEdit(edit);
        return succeeded;
    });
}
exports.replaceNotebookCellMetadata = replaceNotebookCellMetadata;
function replaceNotebookMetadata(notebookUri, documentMetadata) {
    return __awaiter(this, void 0, void 0, function* () {
        const notebookEdit = vscode.NotebookEdit.updateNotebookMetadata(documentMetadata);
        const edit = new vscode.WorkspaceEdit();
        edit.set(notebookUri, [notebookEdit]);
        const succeeded = yield vscode.workspace.applyEdit(edit);
        return succeeded;
    });
}
exports.replaceNotebookMetadata = replaceNotebookMetadata;
function handleCustomInputRequest(prompt, inputTypeHint, password) {
    return __awaiter(this, void 0, void 0, function* () {
        return { handled: false, result: undefined };
    });
}
exports.handleCustomInputRequest = handleCustomInputRequest;
function hashBangConnect(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri) {
    logger_1.Logger.default.info(`handling #!connect for ${documentUri.toString()}`);
    hashBangConnectPrivate(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri);
}
exports.hashBangConnect = hashBangConnect;
function hashBangConnectPrivate(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri) {
    logger_1.Logger.default.info(`handling #!connect from '${hostUri}' for not ebook: ${documentUri.toString()}`);
    const documentUriString = documentUri.toString();
    clientMapper.getOrAddClient(documentUri).then(client => {
        let messageHandler = messageHandlerMap.get(documentUriString);
        if (!messageHandler) {
            messageHandler = new rxjs.Subject();
            messageHandlerMap.set(documentUriString, messageHandler);
        }
        let extensionHostToWebviewSender = dotnet_interactive_1.KernelCommandAndEventSender.FromFunction(envelope => {
            controllerPostMessage({ envelope });
        });
        let WebviewToExtensionHostReceiver = dotnet_interactive_1.KernelCommandAndEventReceiver.FromObservable(messageHandler);
        logger_1.Logger.default.info(`configuring routing for host '${hostUri}'`);
        let sub01 = client.channel.receiver.subscribe({
            next: envelope => {
                if ((0, dotnet_interactive_1.isKernelEventEnvelope)(envelope)) {
                    logger_1.Logger.default.info(`forwarding event to '${hostUri}' ${JSON.stringify(envelope)}`);
                    extensionHostToWebviewSender.send(envelope);
                }
            }
        });
        let sub02 = WebviewToExtensionHostReceiver.subscribe({
            next: envelope => {
                var _a, _b, _c;
                if ((0, dotnet_interactive_1.isKernelCommandEnvelope)(envelope)) {
                    // handle command routing
                    if (envelope.command.destinationUri) {
                        if (envelope.command.destinationUri.startsWith("kernel://vscode")) {
                            // wants to go to vscode
                            logger_1.Logger.default.info(`routing command from '${hostUri}' ${JSON.stringify(envelope)} to extension host`);
                            const kernel = client.kernelHost.getKernel(envelope);
                            kernel.send(envelope);
                        }
                        else {
                            const host = (0, dotnet_interactive_1.extractHostAndNomalize)(envelope.command.destinationUri);
                            const connector = client.kernelHost.tryGetConnector(host);
                            if (connector) {
                                // route to interactive
                                logger_1.Logger.default.info(`routing command from '${hostUri}' ${JSON.stringify(envelope)} to '${host}'`);
                                connector.sender.send(envelope);
                            }
                            else {
                                logger_1.Logger.default.error(`cannot find connector to reach${envelope.command.destinationUri}`);
                            }
                        }
                    }
                    else {
                        const kernel = client.kernelHost.getKernel(envelope);
                        kernel.send(envelope);
                    }
                }
                if ((0, dotnet_interactive_1.isKernelEventEnvelope)(envelope)) {
                    if (envelope.eventType === contracts.KernelInfoProducedType) {
                        const kernelInfoProduced = envelope.event;
                        if (!connection.isKernelInfoForProxy(kernelInfoProduced.kernelInfo)) {
                            connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, client.kernel);
                        }
                    }
                    if ((_a = envelope.command) === null || _a === void 0 ? void 0 : _a.command.originUri) {
                        const host = (0, dotnet_interactive_1.extractHostAndNomalize)((_b = envelope.command) === null || _b === void 0 ? void 0 : _b.command.originUri);
                        const connector = client.kernelHost.tryGetConnector(host);
                        if (connector) {
                            // route to interactive
                            logger_1.Logger.default.info(`routing command from webview ${JSON.stringify(envelope)} to host ${host}`);
                            connector.sender.send(envelope);
                        }
                        else {
                            logger_1.Logger.default.error(`cannot find connector to reach ${(_c = envelope.command) === null || _c === void 0 ? void 0 : _c.command.originUri}`);
                        }
                    }
                }
            }
        });
        const knownKernels = client.kernelHost.getKernelInfoProduced();
        for (const knwonKernel of knownKernels) {
            const kernelInfoProduced = knwonKernel.event;
            logger_1.Logger.default.info(`forwarding kernelInfo [${JSON.stringify(kernelInfoProduced.kernelInfo)}] to webview`);
            extensionHostToWebviewSender.send(knwonKernel);
        }
        client.kernelHost.tryAddConnector({
            sender: extensionHostToWebviewSender,
            receiver: WebviewToExtensionHostReceiver,
            remoteUris: ["kernel://webview"]
        });
        client.registerForDisposal(() => {
            messageHandlerMap.delete(documentUriString);
            client.kernelHost.tryRemoveConnector({ remoteUris: ["kernel://webview"] });
            sub01.unsubscribe();
            sub02.unsubscribe();
        });
        for (const kernelInfo of kernelInfoProduced) {
            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, client.kernel);
        }
    });
}
//# sourceMappingURL=versionSpecificFunctions.js.map
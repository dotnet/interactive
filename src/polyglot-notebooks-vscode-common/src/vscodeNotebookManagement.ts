// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { Logger } from './polyglot-notebooks/logger';
import { extractHostAndNomalize, isKernelCommandEnvelope, isKernelEventEnvelope, KernelCommandAndEventReceiver, KernelCommandAndEventSender, KernelCommandOrEventEnvelope } from './polyglot-notebooks';
import * as rxjs from 'rxjs';
import * as connection from './polyglot-notebooks/connection';
import * as commandsAndEvents from './polyglot-notebooks/commandsAndEvents';
import { areEquivalentObjects } from './metadataUtilities';

export async function replaceNotebookCells(notebookUri: vscode.Uri, range: vscode.NotebookRange, cells: vscode.NotebookCellData[]): Promise<boolean> {
    const notebookEdit = vscode.NotebookEdit.replaceCells(range, cells);
    const edit = new vscode.WorkspaceEdit();
    edit.set(notebookUri, [notebookEdit]);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function updateNotebookCellMetadata(notebookUri: vscode.Uri, cellIndex: number, newCellMetadata: { [key: string]: any }): Promise<boolean> {
    // This is a workaround for a bug in VSCode's .ipynb handling which responds the same notification and races with this code, clobbering our metadata change. Yielding to the event loop seems to help.
    await new Promise(resolve => setTimeout(resolve, 0));

    const notebookEdit = vscode.NotebookEdit.updateCellMetadata(cellIndex, newCellMetadata);
    const edit = new vscode.WorkspaceEdit();
    edit.set(notebookUri, [notebookEdit]);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function updateNotebookMetadata(notebookUri: vscode.Uri, documentMetadata: { [key: string]: any }): Promise<void> {
    const notebook = vscode.workspace.notebookDocuments.find(d => d.uri === notebookUri);
    if (notebook) {
        const metadata = notebook.metadata;
        const shouldUpdate = !areEquivalentObjects(metadata, documentMetadata);

        if (shouldUpdate) {
            const notebookEdit = vscode.NotebookEdit.updateNotebookMetadata(documentMetadata);
            const edit = new vscode.WorkspaceEdit();
            edit.set(notebookUri, [notebookEdit]);
            const succeeded = await vscode.workspace.applyEdit(edit);
        }
    }
}

export async function handleCustomInputRequest(prompt: string, inputTypeHint: string, password: boolean): Promise<{ handled: boolean, result: string | null | undefined }> {
    return { handled: false, result: undefined };
}

export function isNotebookDirty(notebookUri: vscode.Uri): boolean {
    const notebook = vscode.workspace.notebookDocuments.find(d => d.uri === notebookUri);

    if (!notebook) {
        throw new Error(`Notebook with URI ${notebookUri.toString()} not found`);
    }

    return notebook.isDirty;
}

export function hashBangConnect(clientMapper: ClientMapper, hostUri: string, kernelInfos: commandsAndEvents.KernelInfo[], messageHandlerMap: Map<string, rxjs.Subject<KernelCommandOrEventEnvelope>>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {
    Logger.default.info(`handling #!connect for ${documentUri.toString()}`);
    hashBangConnectPrivate(clientMapper, hostUri, kernelInfos, messageHandlerMap, controllerPostMessage, documentUri);
}

function hashBangConnectPrivate(clientMapper: ClientMapper, hostUri: string, kernelInfos: commandsAndEvents.KernelInfo[], messageHandlerMap: Map<string, rxjs.Subject<KernelCommandOrEventEnvelope>>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {

    Logger.default.info(`handling #!connect from '${hostUri}' for notebook: ${documentUri.toString()}`);

    const documentUriString = documentUri.toString();

    clientMapper.getOrAddClient(documentUri).then(client => {

        let messageHandler = messageHandlerMap.get(documentUriString);
        if (!messageHandler) {
            messageHandler = new rxjs.Subject<KernelCommandOrEventEnvelope>();
            messageHandlerMap.set(documentUriString, messageHandler);
        }
        let extensionHostToWebviewSender = KernelCommandAndEventSender.FromFunction(envelope => {
            const commandOrEventForWebview = { envelope: envelope.toJson() };
            controllerPostMessage(commandOrEventForWebview);
        });

        let WebviewToExtensionHostReceiver = KernelCommandAndEventReceiver.FromObservable(messageHandler);

        Logger.default.info(`configuring routing for host '${hostUri}'`);

        let subscriptionToExtensionHost = client.channel.receiver.subscribe({
            next: envelope => {
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`forwarding event to '${hostUri}' ${JSON.stringify(envelope.toJson())}`);
                    extensionHostToWebviewSender.send(envelope);
                }
            }
        });

        let subscriptionToWebView = WebviewToExtensionHostReceiver.subscribe({
            next: envelope => {
                if (isKernelCommandEnvelope(envelope)) {
                    // handle command routing
                    if (envelope.command.destinationUri) {
                        if (envelope.command.destinationUri.startsWith("kernel://vscode")) {
                            // wants to go to vscode
                            Logger.default.info(`routing command from '${hostUri}' ${JSON.stringify(envelope)} to extension host`);
                            const kernel = client.kernelHost.getKernel(envelope);
                            kernel.send(envelope);

                        } else {
                            const host = extractHostAndNomalize(envelope.command.destinationUri);
                            const connector = client.kernelHost.tryGetConnector(host!);
                            if (connector) {
                                // route to interactive
                                Logger.default.info(`routing command from '${hostUri}' ${JSON.stringify(envelope)} to '${host}'`);
                                connector.sender.send(envelope);
                            } else {
                                Logger.default.error(`cannot find connector to reach${envelope.command.destinationUri}`);
                            }
                        }
                    }
                    else {
                        const kernel = client.kernelHost.getKernel(envelope);
                        kernel.send(envelope);
                    }
                }

                if (isKernelEventEnvelope(envelope)) {
                    if (envelope.eventType === commandsAndEvents.KernelInfoProducedType) {
                        const kernelInfoProduced = <commandsAndEvents.KernelInfoProduced>envelope.event;
                        if (!connection.isKernelInfoForProxy(kernelInfoProduced.kernelInfo)) {
                            connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced.kernelInfo, client.kernel);
                        }
                    }

                    if (envelope.command?.command.originUri) {

                        const host = extractHostAndNomalize(envelope.command?.command.originUri);
                        const connector = client.kernelHost.tryGetConnector(host!);
                        if (connector) {
                            // route to interactive
                            Logger.default.info(`routing event from webview ${JSON.stringify(envelope)} to host ${host}`);
                            connector.sender.send(envelope);
                        } else {
                            Logger.default.error(`cannot find connector to reach ${envelope.command?.command.originUri}`);
                        }
                    }
                }
            }
        });

        client.kernelHost.tryAddConnector({
            sender: extensionHostToWebviewSender,
            receiver: WebviewToExtensionHostReceiver,
            remoteUris: ["kernel://webview"]
        });

        client.registerForDisposal(() => {
            messageHandlerMap.delete(documentUriString);
            client.kernelHost.tryRemoveConnector({ remoteUris: ["kernel://webview"] });
            subscriptionToExtensionHost.unsubscribe();
            subscriptionToWebView.unsubscribe();
        });

        for (const kernelInfo of kernelInfos) {
            const remoteUri = kernelInfo.isProxy ? kernelInfo.remoteUri! : kernelInfo.uri;
            if (!client.kernelHost.tryGetConnector(remoteUri)) {
                client.kernelHost.defaultConnector.addRemoteHostUri(remoteUri);
            }
            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, client.kernel);
        }
    });
}

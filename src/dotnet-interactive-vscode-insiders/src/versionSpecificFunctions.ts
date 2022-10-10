// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as vscodeLike from './vscode-common/interfaces/vscode-like';
import { ClientMapper } from './vscode-common/clientMapper';
import { Logger } from './vscode-common/dotnet-interactive/logger';
import { extractHostAndNomalize, isKernelCommandEnvelope, isKernelEventEnvelope, KernelCommandAndEventReceiver, KernelCommandAndEventSender, KernelCommandOrEventEnvelope } from './vscode-common/dotnet-interactive';
import * as rxjs from 'rxjs';
import * as connection from './vscode-common/dotnet-interactive/connection';
import * as contracts from './vscode-common/dotnet-interactive/contracts';

export function getNotebookDocumentFromEditor(notebookEditor: vscode.NotebookEditor): vscode.NotebookDocument {
    return notebookEditor.notebook;
}

export async function replaceNotebookCells(notebookUri: vscode.Uri, range: vscode.NotebookRange, cells: vscode.NotebookCellData[]): Promise<boolean> {
    const notebookEdit = vscode.NotebookEdit.replaceCells(range, cells);
    const edit = new vscode.WorkspaceEdit();
    edit.set(notebookUri, [notebookEdit]);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function replaceNotebookCellMetadata(notebookUri: vscode.Uri, cellIndex: number, newCellMetadata: { [key: string]: any }): Promise<boolean> {
    const notebookEdit = vscode.NotebookEdit.updateCellMetadata(cellIndex, newCellMetadata);
    const edit = new vscode.WorkspaceEdit();
    edit.set(notebookUri, [notebookEdit]);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function replaceNotebookMetadata(notebookUri: vscode.Uri, documentMetadata: { [key: string]: any }): Promise<boolean> {
    const notebookEdit = vscode.NotebookEdit.updateNotebookMetadata(documentMetadata);
    const edit = new vscode.WorkspaceEdit();
    edit.set(notebookUri, [notebookEdit]);
    const succeeded = await vscode.workspace.applyEdit(edit);
    return succeeded;
}

export async function handleCustomInputRequest(prompt: string, inputTypeHint: string, password: boolean): Promise<{ handled: boolean, result: string | null | undefined }> {
    return { handled: false, result: undefined };
}

export function hashBangConnect(clientMapper: ClientMapper, hostUri: string, kernelInfoProduced: contracts.KernelInfoProduced[], messageHandlerMap: Map<string, rxjs.Subject<KernelCommandOrEventEnvelope>>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {
    Logger.default.info(`handling #!connect for ${documentUri.toString()}`);
    hashBangConnectPrivate(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri);
}

function hashBangConnectPrivate(clientMapper: ClientMapper, hostUri: string, kernelInfoProduced: contracts.KernelInfoProduced[], messageHandlerMap: Map<string, rxjs.Subject<KernelCommandOrEventEnvelope>>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {

    Logger.default.info(`handling #!connect from '${hostUri}' for not ebook: ${documentUri.toString()}`);

    const documentUriString = documentUri.toString();

    clientMapper.getOrAddClient(documentUri).then(client => {

        let messageHandler = messageHandlerMap.get(documentUriString);
        if (!messageHandler) {
            messageHandler = new rxjs.Subject<KernelCommandOrEventEnvelope>();
            messageHandlerMap.set(documentUriString, messageHandler);
        }
        let extensionHostToWebviewSender = KernelCommandAndEventSender.FromFunction(envelope => {
            controllerPostMessage({ envelope });
        });

        let WebviewToExtensionHostReceiver = KernelCommandAndEventReceiver.FromObservable(messageHandler);

        Logger.default.info(`configuring routing for host '${hostUri}'`);
        let sub01 = client.channel.receiver.subscribe({
            next: envelope => {
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`forwarding event to '${hostUri}' ${JSON.stringify(envelope)}`);
                    extensionHostToWebviewSender.send(envelope);
                }
            }
        });

        let sub02 = WebviewToExtensionHostReceiver.subscribe({
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
                    if (envelope.eventType === contracts.KernelInfoProducedType) {
                        const kernelInfoProduced = <contracts.KernelInfoProduced>envelope.event;
                        if (!connection.isKernelInfoForProxy(kernelInfoProduced.kernelInfo)) {
                            connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, client.kernel);
                        }
                    }

                    if (envelope.command?.command.originUri) {

                        const host = extractHostAndNomalize(envelope.command?.command.originUri);
                        const connector = client.kernelHost.tryGetConnector(host!);
                        if (connector) {
                            // route to interactive
                            Logger.default.info(`routing command from webview ${JSON.stringify(envelope)} to host ${host}`);
                            connector.sender.send(envelope);
                        } else {
                            Logger.default.error(`cannot find connector to reach ${envelope.command?.command.originUri}`);
                        }
                    }
                }
            }
        });

        const knownKernels = client.kernelHost.getKernelInfoProduced();

        for (const knwonKernel of knownKernels) {
            const kernelInfoProduced = <contracts.KernelInfoProduced>knwonKernel.event;
            Logger.default.info(`forwarding kernelInfo [${JSON.stringify(kernelInfoProduced.kernelInfo)}] to webview`);
            extensionHostToWebviewSender.send(knwonKernel);
        }

        client.kernelHost.tryAddConnector({
            sender: extensionHostToWebviewSender,
            receiver: WebviewToExtensionHostReceiver,
            remoteUris: ["kernel://webview"]
        });

        client.registerForDisposal(() => {
            messageHandlerMap.delete(documentUriString);
            client.kernelHost.tryRemoveConnector({ remoteUris: ["kernel://webview"] })
            sub01.unsubscribe();
            sub02.unsubscribe();
        });

        for (const kernelInfo of kernelInfoProduced) {
            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, client.kernel);
        }
    });
}

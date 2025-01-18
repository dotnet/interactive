// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as commandsAndEvents from './polyglot-notebooks/commandsAndEvents';
import * as metadataUtilities from './metadataUtilities';
import * as vscodeNotebookManagement from './vscodeNotebookManagement';
import { ClientMapper } from './clientMapper';
import { isKernelEventEnvelope } from './polyglot-notebooks';
import * as kernelSelectorUtilities from './kernelSelectorUtilities';
import * as constants from './constants';
import * as vscodeUtilities from './vscodeUtilities';
import { ServiceCollection } from './serviceCollection';

const selectKernelCommandName = 'polyglot-notebook.selectCellKernel';

class KernelSelectorItem implements vscode.QuickPickItem {
    constructor(label: string) {
        this.label = label;
    }

    label: string;
    description?: string | undefined;
    detail?: string | undefined;
    iconPath?: vscode.ThemeIcon;
}

export function registerNotbookCellStatusBarItemProvider(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
    const cellItemProvider = new DotNetNotebookCellStatusBarItemProvider(clientMapper);
    clientMapper.onClientCreate((_uri, client) => {
        client.channel.receiver.subscribe({
            next: envelope => {
                if (isKernelEventEnvelope(envelope) && envelope.eventType === commandsAndEvents.KernelInfoProducedType) {
                    cellItemProvider.updateKernelDisplayNames();
                }
            }
        });
    });
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.NotebookViewType, cellItemProvider));
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.JupyterViewType, cellItemProvider));
    context.subscriptions.push(vscode.commands.registerCommand(selectKernelCommandName, async (cell?: vscode.NotebookCell) => {
        if (cell) {
            const client = await clientMapper.tryGetClient(cell.notebook.uri);
            if (client) {
                const kernelSelectorOptions = kernelSelectorUtilities
                    .getKernelSelectorOptions(client.kernel, cell.notebook, commandsAndEvents.SubmitCodeType);

                const kernelSelectorItems = kernelSelectorOptions
                    .map(o => {
                        const item = new KernelSelectorItem(o.displayValue);
                        item.description = o.description;
                        item.iconPath = new vscode.ThemeIcon('notebook-kernel-select');
                        return item;
                    });

                const recentConnectionsOption = {
                    label: 'Connect subkernel...',
                    iconPath: new vscode.ThemeIcon('plug')
                };

                const mruConnectionItems = [recentConnectionsOption];

                const allItems = [
                    ...kernelSelectorItems,
                    { kind: vscode.QuickPickItemKind.Separator, description: '', label: '' },
                    ...mruConnectionItems];

                const selectedDisplayOption = await vscode.window.showQuickPick(allItems, { title: 'Select cell kernel' });

                if (selectedDisplayOption) {
                    const selectedValueIndex = kernelSelectorItems.indexOf(selectedDisplayOption);
                    if (selectedValueIndex >= 0) {
                        const selectedKernelItem = kernelSelectorOptions[selectedValueIndex];
                        const codeCell = await vscodeUtilities.ensureCellKernelKind(cell, vscode.NotebookCellKind.Code);
                        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
                        if (notebookCellMetadata.kernelName !== selectedKernelItem.kernelName) {
                            // update metadata
                            notebookCellMetadata.kernelName = selectedKernelItem.kernelName;
                            const newRawMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
                            const mergedMetadata = metadataUtilities.mergeRawMetadata(cell.metadata, newRawMetadata);
                            const _succeeded = await vscodeNotebookManagement.replaceNotebookCellMetadata(codeCell.notebook.uri, codeCell.index, mergedMetadata);

                            // update language configuration
                            ServiceCollection.Instance.LanguageConfigurationManager.ensureLanguageConfigurationForDocument(cell.document);

                            // update tokens
                            await vscode.commands.executeCommand('polyglot-notebook.refreshSemanticTokens');
                        }
                    } else if (selectedDisplayOption === recentConnectionsOption) {
                        await vscode.commands.executeCommand('polyglot-notebook.notebookEditor.connectSubkernel');
                    }
                }
            }
        }
    }));
}

function getNotebookDcoumentFromCellDocument(cellDocument: vscode.TextDocument): vscode.NotebookDocument | undefined {
    const notebookDocument = vscode.workspace.notebookDocuments.find(notebook => notebook.getCells().some(cell => cell.document === cellDocument));
    return notebookDocument;
}

class DotNetNotebookCellStatusBarItemProvider {
    private _onDidChangeCellStatusBarItemsEmitter: vscode.EventEmitter<void> = new vscode.EventEmitter<void>();

    onDidChangeCellStatusBarItems: vscode.Event<void> = this._onDidChangeCellStatusBarItemsEmitter.event;

    constructor(private readonly clientMapper: ClientMapper) {
    }

    async provideCellStatusBarItems(cell: vscode.NotebookCell, token: vscode.CancellationToken): Promise<vscode.NotebookCellStatusBarItem[]> {
        if (!metadataUtilities.isDotNetNotebook(cell.notebook) || cell.document.languageId === 'markdown') {
            return [];
        }

        const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(cell.notebook);
        const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
        const cellKernelName = cellMetadata.kernelName ?? notebookMetadata.kernelInfo.defaultKernelName;
        const notebookDocument = getNotebookDcoumentFromCellDocument(cell.document);
        const client = await this.clientMapper.tryGetClient(notebookDocument!.uri); // don't force client creation
        let displayText: string;
        if (client) {
            const matchingKernel = client.kernel.childKernels.find(k => k.kernelInfo.localName === cellKernelName);
            displayText = matchingKernel ? kernelSelectorUtilities.getKernelInfoDisplayValue(matchingKernel.kernelInfo) : cellKernelName;
        }
        else {
            displayText = cellKernelName;
        }

        const item = new vscode.NotebookCellStatusBarItem(displayText, vscode.NotebookCellStatusBarAlignment.Right);
        item.command = selectKernelCommandName;
        item.tooltip = "Select cell kernel";
        return [item];
    }

    updateKernelDisplayNames() {
        this._onDidChangeCellStatusBarItemsEmitter.fire();
    }
}

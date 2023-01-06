// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './dotnet-interactive/contracts';
import * as metadataUtilities from './metadataUtilities';
import * as versionSpecificFunctions from '../versionSpecificFunctions';
import { ClientMapper } from './clientMapper';
import { isKernelEventEnvelope } from './dotnet-interactive';
import * as kernelSelectorUtilities from './kernelSelectorUtilities';
import * as constants from './constants';
import * as vscodeUtilities from './vscodeUtilities';
import { ServiceCollection } from './serviceCollection';

const selectKernelCommandName = 'polyglot-notebook.selectCellKernel';

export function registerNotbookCellStatusBarItemProvider(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
    const cellItemProvider = new DotNetNotebookCellStatusBarItemProvider(clientMapper);
    clientMapper.onClientCreate((_uri, client) => {
        client.channel.receiver.subscribe({
            next: envelope => {
                if (isKernelEventEnvelope(envelope) && envelope.eventType === contracts.KernelInfoProducedType) {
                    cellItemProvider.updateKernelDisplayNames();
                }
            }
        });
    });
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.NotebookViewType, cellItemProvider));
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.JupyterViewType, cellItemProvider)); // TODO: fix this
    context.subscriptions.push(vscode.notebooks.registerNotebookCellStatusBarItemProvider(constants.LegacyNotebookViewType, cellItemProvider));
    context.subscriptions.push(vscode.commands.registerCommand(selectKernelCommandName, async (cell?: vscode.NotebookCell) => {
        if (cell) {
            const client = await clientMapper.tryGetClient(cell.notebook.uri);
            if (client) {
                const availableOptions = kernelSelectorUtilities.getKernelSelectorOptions(client.kernel, cell.notebook, contracts.SubmitCodeType);
                const availableDisplayOptions = availableOptions.map(o => o.displayValue);
                const selectedDisplayOption = await vscode.window.showQuickPick(availableDisplayOptions, { title: 'Select kernel' });
                if (selectedDisplayOption) {
                    const selectedValueIndex = availableDisplayOptions.indexOf(selectedDisplayOption);
                    if (selectedValueIndex >= 0) {
                        const selectedKernelData = availableOptions[selectedValueIndex];
                        const codeCell = await vscodeUtilities.ensureCellKernelKind(cell, vscode.NotebookCellKind.Code);
                        const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
                        if (notebookCellMetadata.kernelName !== selectedKernelData.kernelName) {
                            // update metadata
                            notebookCellMetadata.kernelName = selectedKernelData.kernelName;
                            const newRawMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(notebookCellMetadata);
                            const mergedMetadata = metadataUtilities.mergeRawMetadata(cell.metadata, newRawMetadata);
                            const _succeeded = await versionSpecificFunctions.replaceNotebookCellMetadata(codeCell.notebook.uri, codeCell.index, mergedMetadata);

                            // update language configuration
                            ServiceCollection.Instance.LanguageConfigurationManager.ensureLanguageConfigurationForDocument(cell.document);

                            // update tokens
                            await vscode.commands.executeCommand('polyglot-notebook.refreshSemanticTokens');
                        }
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
        if (!metadataUtilities.isDotNetNotebook(cell.notebook)) {
            return [];
        }

        let displayText: string;
        if (cell.document.languageId === 'markdown') {
            displayText = 'Markdown';
        } else {
            const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
            const cellKernelName = cellMetadata.kernelName ?? 'csharp';
            const notebookDocument = getNotebookDcoumentFromCellDocument(cell.document);
            const client = await this.clientMapper.tryGetClient(notebookDocument!.uri); // don't force client creation
            if (client) {
                const matchingKernel = client.kernel.childKernels.find(k => k.kernelInfo.localName === cellKernelName);
                displayText = matchingKernel ? kernelSelectorUtilities.getKernelInfoDisplayValue(matchingKernel.kernelInfo) : cellKernelName;
            }
            else {
                displayText = cellKernelName;
            }
        }

        const item = new vscode.NotebookCellStatusBarItem(displayText, vscode.NotebookCellStatusBarAlignment.Right);
        const command: vscode.Command = {
            title: '<unused>',
            command: selectKernelCommandName,
            arguments: [],
        };
        item.command = command;
        return [item];
    }

    updateKernelDisplayNames() {
        this._onDidChangeCellStatusBarItemsEmitter.fire();
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import * as contracts from './dotnet-interactive/contracts';
import * as utilities from './utilities';
import * as versionSpecificFunctions from '../versionSpecificFunctions';
import { Disposable } from './dotnet-interactive/disposables';
import { isKernelEventEnvelope } from './dotnet-interactive';
import * as kernelSelectorUtilities from './kernelSelectorUtilities';
import * as vscodeLike from './interfaces/vscode-like';
import { VariableGridRow, VariableInfo } from '../ui/types';

function debounce(callback: () => void) {
    utilities.debounce('variable-explorer', 500, callback);
}

export function registerVariableExplorer(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
    context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.shareValueWith', async (variableInfo: VariableInfo | undefined) => {
        const activeNotebookEditor = vscode.window.activeNotebookEditor;
        if (variableInfo && activeNotebookEditor) {
            const notebookDocument = versionSpecificFunctions.getNotebookDocumentFromEditor(activeNotebookEditor);
            const client = await clientMapper.tryGetClient(notebookDocument.uri);
            if (client) {
                const kernelSelectorOptions = kernelSelectorUtilities.getKernelSelectorOptions(client.kernel, notebookDocument, contracts.SendValueType);
                const kernelDisplayValues = kernelSelectorOptions.map(k => k.displayValue);
                const selectedKernelDisplayName = await vscode.window.showQuickPick(kernelDisplayValues, { title: `Share value [${variableInfo.valueName}] from [${variableInfo.sourceKernelName}] to ...` });
                if (selectedKernelDisplayName) {
                    const targetKernelIndex = kernelDisplayValues.indexOf(selectedKernelDisplayName);
                    if (targetKernelIndex >= 0) {
                        const targetKernelSelectorOption = kernelSelectorOptions[targetKernelIndex];
                        // ends with newline to make adding code easier
                        const code = `#!share --from ${variableInfo.sourceKernelName} ${variableInfo.valueName}\n`;
                        const command: contracts.SendEditableCode = {
                            kernelName: targetKernelSelectorOption.kernelName,
                            code,
                        };
                        const commandEnvelope: contracts.KernelCommandEnvelope = {
                            commandType: contracts.SendEditableCodeType,
                            command,
                        };

                        await client.kernel.rootKernel.send(commandEnvelope);
                    }
                }
            }
        }
    }));

    const webViewProvider = new WatchWindowTableViewProvider(clientMapper, context.extensionPath);
    context.subscriptions.push(vscode.window.registerWebviewViewProvider('polyglot-notebook-panel-values', webViewProvider, { webviewOptions: { retainContextWhenHidden: true } }));

    vscode.window.onDidChangeActiveNotebookEditor(async editor => {
        const notebookUri = editor?.notebook.uri;
        debounce(() => webViewProvider.showNotebookVariables(notebookUri));
    });
}

class WatchWindowTableViewProvider implements vscode.WebviewViewProvider {
    private webview: vscode.Webview | undefined = undefined;
    private clientMessageSubscriptions: Map<vscodeLike.Uri, Disposable> = new Map();
    private notebookVariables: Map<vscodeLike.Uri, VariableGridRow[]> = new Map();
    private completedNotebookKernels: Map<vscodeLike.Uri, Set<string>> = new Map();

    constructor(private readonly clientMapper: ClientMapper, private readonly extensionPath: string) {
        // on every new notebook, track it's completion events
        clientMapper.onClientCreate((uri, client) => {
            const subscription = client.channel.receiver.subscribe({
                next: (envelope) => {
                    if (isKernelEventEnvelope(envelope)) {
                        switch (envelope.eventType) {
                            case contracts.CommandSucceededType:
                            case contracts.CommandFailedType:
                            case contracts.CommandCancelledType:
                                if (envelope.command?.commandType === contracts.SubmitCodeType) {
                                    const completedKernels = this.completedNotebookKernels.get(uri) ?? new Set<string>();
                                    const kernelName = envelope.command?.command?.targetKernelName;
                                    if (kernelName) {
                                        completedKernels.add(kernelName);
                                    }

                                    this.completedNotebookKernels.set(uri, completedKernels);
                                    debounce(() => {
                                        this.refreshVariables(uri).then(() => {
                                            this.showNotebookVariables(vscode.window.activeNotebookEditor?.notebook.uri);
                                        });
                                    });
                                }
                                break;
                        }
                    }
                }
            });

            this.clientMessageSubscriptions.set(uri, {
                dispose: () => {
                    subscription.unsubscribe();
                    this.completedNotebookKernels.delete(uri);
                    this.notebookVariables.delete(uri);
                }
            });
        });

        // when the notebook is closed, stop tracking it's completion events
        clientMapper.onClientDispose((uri, _client) => {
            const disposable = this.clientMessageSubscriptions.get(uri);
            if (disposable) {
                disposable.dispose();
            }
        });
    }

    async resolveWebviewView(webviewView: vscode.WebviewView, context: vscode.WebviewViewResolveContext<unknown>, token: vscode.CancellationToken): Promise<void> {
        this.webview = webviewView.webview;
        webviewView.webview.options = {
            enableScripts: true,
            enableCommandUris: true,
        };
        this.webview.onDidReceiveMessage(message => {
            const x = message;
            if (message.command === 'shareValueWith') {
                vscode.commands.executeCommand('polyglot-notebook.shareValueWith', message.variableInfo);
            }
        });

        const jsFileUri = this.webview.asWebviewUri(vscode.Uri.file(path.join(this.extensionPath, 'resources', 'variable-grid.bundle.js')));
        const htmlFileUri = vscode.Uri.file(path.join(this.extensionPath, 'resources', 'index.variable-grid.html'));

        const decoder = new TextDecoder();
        let rawhtmlContent = await vscode.workspace.fs.readFile(htmlFileUri);
        let htmlContent = decoder.decode(rawhtmlContent);
        htmlContent = htmlContent.replace("variable-grid.bundle.js", jsFileUri.toString());

        this.webview.html = htmlContent;

        const currentNotebookUri = vscode.window.activeNotebookEditor?.notebook.uri;
        this.showNotebookVariables(currentNotebookUri);
    }

    private setRows(rows: VariableGridRow[]) {
        if (this.webview) {
            this.webview.postMessage({ command: 'set-rows', rows });
        }
    }

    showNotebookVariables(notebookUri: vscodeLike.Uri | undefined) {
        let rows: VariableGridRow[] = [];
        if (notebookUri) {
            const cachedRows = this.notebookVariables.get(notebookUri);
            rows = cachedRows ?? [];
        }

        this.setRows(rows);
    }

    async refreshVariables(uri: vscodeLike.Uri): Promise<void> {
        const rows: VariableGridRow[] = [];
        const client = await this.clientMapper.tryGetClient(uri);
        if (client) {
            const allKernels = Array.from(client.kernel.childKernels.filter(k => k.kernelInfo.supportedKernelCommands.find(ci => ci.name === contracts.RequestValueInfosType)));
            const kernels = allKernels.filter(kernel => {
                return this.completedNotebookKernels.get(uri)?.has(kernel.name) ?? false;
            });
            for (const kernel of kernels) {
                try {
                    const valueInfos = await client.requestValueInfos(kernel.name);
                    for (const valueInfo of valueInfos.valueInfos) {
                        try {
                            const valueName = valueInfo.name;
                            const valueValue = valueInfo.formattedValue.value;
                            const typeName = valueInfo.typeName;
                            const displayName = kernelSelectorUtilities.getKernelInfoDisplayValue(kernel.kernelInfo);

                            rows.push({
                                name: valueName,
                                value: valueValue,
                                typeName: typeName,
                                kernelDisplayName: displayName,
                                kernelName: kernel.name
                            });
                        } catch (e) {
                            // likely didn't support `RequestValue`
                            const x = e;
                        }
                    }
                } catch (e) {
                    // likely didn't support `RequestValueInfos`
                    const x = e;
                }
            }
        }

        this.notebookVariables.set(uri, rows);
    }
}

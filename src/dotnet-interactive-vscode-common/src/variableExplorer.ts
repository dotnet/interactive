// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import * as contracts from './dotnet-interactive/contracts';
import { VariableGridRow, VariableInfo } from './dotnet-interactive/webview/variableGridInterfaces';
import * as utilities from './utilities';
import * as versionSpecificFunctions from '../versionSpecificFunctions';
import { Disposable } from './dotnet-interactive/disposables';
import { isKernelEventEnvelope } from './dotnet-interactive';
import * as kernelSelectorUtilities from './kernelSelectorUtilities';
import * as vscodeLike from './interfaces/vscode-like';

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
        // only load this once
        const apiFileUri = this.webview.asWebviewUri(vscode.Uri.file(path.join(this.extensionPath, 'resources', 'variableGrid.js')));
        const html = `
        <html>
            <head>
                <meta charset="utf-8">
                <title>Polyglot Notebook: Values</title>
            </head>
            <body>
                <style>
                    table, th, td {
                        border-collapse: collapse;
                        border: 1px solid var(--vscode-quickInputList-focusBackground);
                    }
                    table {
                        width: 100%;
                    }
                    th {
                        color: var(--vscode-quickInputList-focusForeground);
                        background-color: var(--vscode-quickInputList-focusBackground);
                    }
                    td {
                        text-align: left;
                    }

                    input {
                        background-color: var(--vscode-settings-textInputBackground);
                        border: 1px solid var(--vscode-inputValidation-infoBorder);
                        color: var(--vscode-settings-textInputForeground);
                    }
                    button {
                        background-color: var(--vscode-button-background);
                        border: var(--vscode-button-border);
                        color: var(--vscode-button-foreground);
                    }
                    button[hover] {
                        background-color: var(--vscode-button-hoverBackground);
                    }
                    button.share {
                        background-color: transparent;
                        border: 0px;
                    }
                    .name-column {
                        width: 20%;
                    }
                    .type-column {
                        width: 15%;
                    }
                    .value-column {
                    }
                    .kernel-column {
                        width: 15%;
                    }
                    .share-column {
                        width: 10%;
                    }

                    .share-data {
                        text-align: center;
                    }

                    .share-symbol {
                        padding: 2px;
                        height: 16px;
                        width: 16px;
                    }
                    .arrow {
                        fill: var(--vscode-settings-textInputForeground);
                    }
                    .arrow-box {
                        fill: var(--vscode-button-background);
                    }
                </style>
                <svg style="display: none">
                  <symbol id="share-icon" viewBox="0 0 16 16">
                    <title>Share value to...</title>
                    <g id="canvas">
                      <path d="M16,16H0V0H16Z" fill="none" opacity="0" />
                    </g>
                    <g id="level-1">
                      <path class="arrow" d="M10.5,9.5v-2a9.556,9.556,0,0,0-7,3c0-7,7-7,7-7v-2l4,4Z" opacity="0.1" />
                      <path class="arrow" d="M15.207,5.5,10,.293V3.032C8.322,3.2,3,4.223,3,10.5v1.371l.883-1.05A9.133,9.133,0,0,1,10,8.014v2.693ZM4.085,9.26C4.834,4.081,10.254,4,10.5,4L11,4V2.707L13.793,5.5,11,8.293V7h-.5A10.141,10.141,0,0,0,4.085,9.26Z" />
                      <path class="arrow-box" d="M12,10.121V15H0V4H1V14H11V11.121Z" />
                    </g>
                  </symbol>
                </svg>
                <script defer type="text/javascript" src="${apiFileUri.toString()}"></script>
                <div style="margin: 2px 0px 2px 0px;">
                  <label for="filter">Filter</label>
                  <input id="filter" type="text" />
                </div>
                <div id="content"></div>
            </body>
        </html>
        `;
        this.webview.html = html;

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
                            const commandUrl = `command:polyglot-notebook.shareValueWith?${encodeURIComponent(JSON.stringify({ valueName, kernelName: kernel.name }))}`;
                            rows.push({
                                name: valueName,
                                value: valueValue,
                                typeName: typeName,
                                kernelDisplayName: displayName,
                                kernelName: kernel.name,
                                link: commandUrl,
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

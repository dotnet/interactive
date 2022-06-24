// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import * as contracts from './dotnet-interactive/contracts';
import { getNotebookSpecificLanguage } from './interactiveNotebook';
import { VariableGridRow } from './dotnet-interactive/webview/variableGridInterfaces';
import * as versionSpecificFunctions from '../versionSpecificFunctions';

// creates a map of, e.g.:
//   "dotnet-interactive.csharp" => "C# (.NET Interactive)""
const languageIdToAliasMap = new Map(
    vscode.extensions.all.map(e => <any[]>e.packageJSON?.contributes?.languages || [])
        .filter(l => l)
        .reduce((a, b) => a.concat(b), [])
        .filter(l => typeof l.id === 'string' && (l.aliases?.length ?? 0) > 0 && typeof l.aliases[0] === 'string')
        .map(l => <[string, string]>[<string>l.id, <string>l.aliases[0]])
);

export function registerVariableExplorer(context: vscode.ExtensionContext, clientMapper: ClientMapper) {

    context.subscriptions.push(vscode.commands.registerCommand('dotnet-interactive.shareValueTo', async (variableInfo: { kernelName: string, valueName: string } | undefined) => {
        if (variableInfo && vscode.window.activeNotebookEditor) {
            const client = await clientMapper.tryGetClient(versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor).uri);
            if (client) {
                // creates a map of _only_ the available languages in this notebook, e.g.:
                //   "C# (.NET Interactive)" => "dotnet-interactive.csharp"
                const availableKernelDisplayNamesToLanguageNames = new Map(client.kernel.childKernels.map(k => {
                    const notebookLanguage = getNotebookSpecificLanguage(k.name);
                    let displayLanguage = notebookLanguage;
                    const displayLanguageCandidate = languageIdToAliasMap.get(notebookLanguage);
                    if (displayLanguageCandidate) {
                        displayLanguage = displayLanguageCandidate;
                    }

                    return <[string, string]>[displayLanguage, k.name];
                }));

                const kernelDisplayValues = [...availableKernelDisplayNamesToLanguageNames.keys()];
                const selectedKernelName = await vscode.window.showQuickPick(kernelDisplayValues, { title: `Share value [${variableInfo.valueName}] from [${variableInfo.kernelName}] to ...` });
                if (selectedKernelName) {
                    // translate back from display name (e.g., "C# (.NET Interactive)") to language name (e.g., "dotnet-interactive.csharp")
                    const targetKernelName = availableKernelDisplayNamesToLanguageNames.get(selectedKernelName)!;
                    // TODO: if not well-known kernel/language, add kernel selector, e.g., `#!sql-AdventureWorks`
                    // ends with newline to make adding code easier
                    const code = `#!share --from ${variableInfo.kernelName} ${variableInfo.valueName}\n`;
                    const command: contracts.SendEditableCode = {
                        language: targetKernelName,
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
    }));

    const webViewProvider = new WatchWindowTableViewProvider(clientMapper, context.extensionPath);
    context.subscriptions.push(vscode.window.registerWebviewViewProvider('dotnet-interactive-panel-values', webViewProvider, { webviewOptions: { retainContextWhenHidden: true } }));

    vscode.window.onDidChangeActiveNotebookEditor(async _editor => {
        // TODO: update on client process restart
        webViewProvider.refresh();
    });
}

class WatchWindowTableViewProvider implements vscode.WebviewViewProvider {
    private currentNotebookSubscription: contracts.DisposableSubscription | undefined = undefined;
    private webview: vscode.Webview | undefined = undefined;

    constructor(private readonly clientMapper: ClientMapper, private readonly extensionPath: string) {
    }

    async resolveWebviewView(webviewView: vscode.WebviewView, context: vscode.WebviewViewResolveContext<unknown>, token: vscode.CancellationToken): Promise<void> {
        this.webview = webviewView.webview;
        webviewView.webview.options = {
            enableScripts: true,
            enableCommandUris: true,
        };
        this.webview.onDidReceiveMessage(message => {
            const x = message;
        });
        // only load this once
        const apiFileUri = this.webview.asWebviewUri(vscode.Uri.file(path.join(this.extensionPath, 'resources', 'variableGrid.js')));
        const html = `
        <html>
            <head>
                <meta charset="utf-8">
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
                        border: var(--vscode-settings-textInputBorder);
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

                    .name-column {
                        width: 20%;
                    }
                    .value-column {
                    }
                    .kernel-column {
                        width: 20%;
                    }
                    .share-column {
                        width: 10%;
                    }
                </style>
                <script defer type="text/javascript" src="${apiFileUri.toString()}"></script>
                <label for="filter">Filter</label>
                <input id="filter" type="text" />
                <button id="clear">Clear</button>
                <div id="content"></div>
            </body>
        </html>
        `;
        this.webview.html = html;
        this.refresh();
    }

    private setRows(rows: VariableGridRow[]) {
        if (this.webview) {
            this.webview.postMessage({ command: 'set-rows', rows });
        }
    }

    async refresh(): Promise<void> {
        const rows: VariableGridRow[] = [];
        this.currentNotebookSubscription?.dispose();
        this.currentNotebookSubscription = undefined;
        if (vscode.window.activeNotebookEditor) {
            const notebook = versionSpecificFunctions.getNotebookDocumentFromEditor(vscode.window.activeNotebookEditor);
            const client = await this.clientMapper.getOrAddClient(notebook.uri);
            this.currentNotebookSubscription = client.channel.subscribeToKernelEvents(eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case contracts.CommandSucceededType:
                    case contracts.CommandFailedType:
                    case contracts.CommandCancelledType:
                        if (eventEnvelope.command?.commandType === contracts.SubmitCodeType) {
                            this.refresh();
                        }
                        break;
                }
            });

            const kernelNames = [...client.kernel.childKernels.map(k => k.name)];
            kernelNames.push('value');

            for (const name of kernelNames) {
                try {
                    const valueInfos = await client.requestValueInfos(name);
                    for (const valueInfo of valueInfos.valueInfos) {
                        try {
                            const value = await client.requestValue(valueInfo.name, name);
                            const valueName = value.name;
                            const valueValue = value.formattedValue.value;
                            const commandUrl = `command:dotnet-interactive.shareValueTo?${encodeURIComponent(JSON.stringify({ valueName, kernelName: name }))}`;
                            rows.push({
                                name: valueName,
                                value: valueValue,
                                kernel: `#!${name}`,
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

        this.setRows(rows);
    }
}

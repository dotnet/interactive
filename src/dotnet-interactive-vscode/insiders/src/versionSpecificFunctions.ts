// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as ipynbUtilities from './common/ipynbUtilities';
import * as utilities from './common/utilities';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';
import * as notebookControllers from './notebookControllers';
import * as notebookSerializers from './notebookSerializers';
import { ClientMapper } from './common/clientMapper';
import { OutputChannelAdapter } from './common/vscode/OutputChannelAdapter';
import { ErrorOutputCreator } from './common/interactiveClient';

export function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, outputChannel: OutputChannelAdapter, createErrorOutput: ErrorOutputCreator, ...preloadUris: vscode.Uri[]) {
    const config = {
        clientMapper,
        preloadUris,
        createErrorOutput,
    };
    context.subscriptions.push(new notebookControllers.DotNetNotebookKernel(config));
    context.subscriptions.push(new notebookSerializers.DotNetDibNotebookSerializer(clientMapper, outputChannel));
    context.subscriptions.push(new notebookSerializers.DotNetLegacyNotebookSerializer(clientMapper, outputChannel));
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    notebookControllers.endExecution(cell, success);
}

export function createErrorOutput(message: string, outputId?: string): vscodeLike.NotebookCellOutput {
    const error = { name: 'Error', message };
    const errorItem = vscode.NotebookCellOutputItem.error(error);
    const cellOutput = utilities.createOutput([errorItem], outputId);
    return cellOutput;
}

export async function createNewBlankNotebook(extension: string, _openNotebook: (uri: vscode.Uri) => Promise<void>): Promise<void> {
    const viewType = extension === '.dib' || extension === '.dotnet-interactive'
        ? 'dotnet-interactive'
        : interactiveNotebook.jupyterViewType;

    // get language
    const newNotebookCSharp = `C#`;
    const newNotebookFSharp = `F#`;
    const newNotebookPowerShell = `PowerShell`;
    const notebookLanguage = await vscode.window.showQuickPick([newNotebookCSharp, newNotebookFSharp, newNotebookPowerShell], { title: 'Default Language' });
    if (!notebookLanguage) {
        return;
    }

    const ipynbLanguageName = ipynbUtilities.mapIpynbLanguageName(notebookLanguage);
    const cellMetadata = new vscode.NotebookCellMetadata().with({
        custom: {
            metadata: {
                dotnet_interactive: {
                    language: ipynbLanguageName
                }
            }
        }
    });
    const cell = new vscode.NotebookCellData(vscode.NotebookCellKind.Code, '', `dotnet-interactive.${ipynbLanguageName}`, undefined, cellMetadata);
    const documentMetadata = new vscode.NotebookDocumentMetadata().with({
        custom: {
            metadata: {
                kernelspec: {
                    display_name: `.NET (${notebookLanguage})`,
                    language: notebookLanguage,
                    name: `.net-${ipynbLanguageName}`
                },
                language_info: {
                    name: notebookLanguage
                }
            }
        }
    });
    const content = new vscode.NotebookData([cell], documentMetadata);
    const notebook = await vscode.notebook.openNotebookDocument(viewType, content);
    const _editor = await vscode.window.showNotebookDocument(notebook);
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
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
import fetch from 'node-fetch';

export function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, outputChannel: OutputChannelAdapter, createErrorOutput: ErrorOutputCreator, ...preloadUris: vscode.Uri[]) {
    const config = {
        clientMapper,
        preloadUris,
        createErrorOutput,
    };
    context.subscriptions.push(new notebookControllers.DotNetNotebookKernel(config));
    notebookSerializers.DotNetDibNotebookSerializer.registerNotebookSerializer(context, 'dotnet-interactive', clientMapper, outputChannel);
    notebookSerializers.DotNetLegacyNotebookSerializer.registerNotebookSerializer(context, 'dotnet-interactive-legacy', clientMapper, outputChannel);
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    notebookControllers.endExecution(cell, success);
}

export function getCellOutputItems(cellOutput: vscode.NotebookCellOutput): vscode.NotebookCellOutputItem[] {
    return cellOutput.items;
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
    const notebook = await vscode.notebooks.openNotebookDocument(viewType, content);
    const _editor = await vscode.window.showNotebookDocument(notebook);
}

export async function openNotebookFromUrl(notebookUrl: string, clientMapper: ClientMapper, diagnosticsChannel: OutputChannelAdapter): Promise<void> {
    await vscode.commands.executeCommand('dotnet-interactive.acquire');
    const extension = path.extname(notebookUrl);
    let serializer: notebookSerializers.DotNetDibNotebookSerializer | undefined = undefined;
    let viewType: string | undefined = undefined;
    switch (extension) {
        case '.dib':
        case '.dotnet-interactive':
            serializer = new notebookSerializers.DotNetDibNotebookSerializer(clientMapper, diagnosticsChannel);
            viewType = 'dotnet-interactive';
            break;
        case '.ipynb':
            serializer = new notebookSerializers.DotNetJupyterNotebookSerializer(clientMapper, diagnosticsChannel);
            viewType = 'jupyter-notebook';
            break;
    }

    if (serializer && viewType) {
        try {
            const response = await fetch(notebookUrl);
            const arrayBuffer = await response.arrayBuffer();
            const content = new Uint8Array(arrayBuffer);
            const cancellationTokenSource = new vscode.CancellationTokenSource();
            const notebookData = await serializer.deserializeNotebook(content, cancellationTokenSource.token);
            const notebook = await vscode.notebooks.openNotebookDocument(viewType, notebookData);
            const _editor = await vscode.window.showNotebookDocument(notebook);
        } catch (e) {
            vscode.window.showWarningMessage(`Unable to read notebook from '${notebookUrl}': ${e}`);
        }
    }
}

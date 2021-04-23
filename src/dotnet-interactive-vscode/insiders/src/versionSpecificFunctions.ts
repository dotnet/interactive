// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as utilities from './common/utilities';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';
import * as notebookControllers from './notebookControllers';
import * as notebookSerializers from './notebookSerializers';
import { ClientMapper } from './common/clientMapper';
import { OutputChannelAdapter } from './common/vscode/OutputChannelAdapter';
import { getSimpleLanguage, isDotnetInteractiveLanguage } from './common/interactiveNotebook';

export function cellAt(document: vscode.NotebookDocument, index: number): vscode.NotebookCell {
    return document.cellAt(index);
}

export function cellCount(document: vscode.NotebookDocument): number {
    return document.cellCount;
}

export function getCells(document: vscode.NotebookDocument | undefined): Array<vscode.NotebookCell> {
    if (document) {
        return [...document.getCells()];
    }

    return [];
}

export function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, outputChannel: OutputChannelAdapter, ...preloadUris: vscode.Uri[]) {
    context.subscriptions.push(new notebookControllers.DotNetNotebookKernel(clientMapper, preloadUris));
    context.subscriptions.push(new notebookSerializers.DotNetDibNotebookSerializer(clientMapper, outputChannel));
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    notebookControllers.endExecution(cell, success);
}

export function onDocumentOpen(context: vscode.ExtensionContext) {
    context.subscriptions.push(vscode.notebook.onDidOpenNotebookDocument(async e => await updateNotebookCellLanguageInMetadata(e)));
}


async function updateNotebookCellLanguageInMetadata(candidateNotebookDocument: vscode.NotebookDocument) {
    const notebook = candidateNotebookDocument;
    const edit = new vscode.WorkspaceEdit();
    for (let cell of notebook.getCells()) {

        if (cell.kind === vscode.NotebookCellKind.Code && isDotnetInteractiveLanguage(cell.document.languageId)) {
            const newMetadata = cell.metadata.with({
                custom: {
                    metadata: {
                        dotnet_interactive: {
                            language: getSimpleLanguage(cell.document.languageId)
                        }
                    }
                }
            });

            edit.replaceNotebookCellMetadata(notebook.uri, cell.index, newMetadata);

        }
    }
    await vscode.workspace.applyEdit(edit);
}


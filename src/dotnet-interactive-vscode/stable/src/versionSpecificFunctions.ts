// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
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
import { ErrorOutputCreator } from './common/interactiveClient';

export function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, outputChannel: OutputChannelAdapter, createErrorOutput: ErrorOutputCreator, ...preloadUris: vscode.Uri[]) {
    context.subscriptions.push(new notebookControllers.DotNetNotebookKernel(clientMapper, createErrorOutput, preloadUris));
    context.subscriptions.push(new notebookSerializers.DotNetDibNotebookSerializer(clientMapper, outputChannel));
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    notebookControllers.endExecution(cell, success);
}

export function createErrorOutput(message: string, outputId?: string): vscodeLike.NotebookCellOutput {
    const errorItem: vscodeLike.NotebookCellOutputItem = {
        mime: 'application/x.notebook.error-traceback',
        value: {
            ename: 'Error',
            evalue: message,
            traceback: [],
        },
    };
    const cellOutput = utilities.createOutput([errorItem], outputId);
    return cellOutput;
}

export async function createNewBlankNotebook(extension: string, openNotebook: (uri: vscode.Uri) => Promise<void>): Promise<void> {
    const fileName = getNewNotebookName(extension);
    const newUri = vscode.Uri.file(fileName).with({ scheme: 'untitled', path: fileName });
    await openNotebook(newUri);
}

function workspaceHasUnsavedNotebookWithName(fileName: string): boolean {
    return vscode.workspace.textDocuments.findIndex(textDocument => {
        if (textDocument.notebook) {
            const notebookUri = textDocument.notebook.uri;
            return notebookUri.scheme === 'untitled' && path.basename(notebookUri.fsPath) === fileName;
        }

        return false;
    }) >= 0;
}

function getNewNotebookName(extension: string): string {
    let suffix = 1;
    let filename = '';
    do {
        filename = `Untitled-${suffix++}${extension}`;
    } while (workspaceHasUnsavedNotebookWithName(filename));
    return filename;
}

export async function openNotebookFromUrl(notebookUrl: string, clientMapper: ClientMapper, diagnosticsChannel: OutputChannelAdapter): Promise<void> {
    // NOOP
}

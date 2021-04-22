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

export function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, outputChannel: OutputChannelAdapter, useJupyterExtension: boolean, ...preloadUris: vscode.Uri[]) {
    context.subscriptions.push(new notebookControllers.DotNetNotebookKernel(clientMapper, preloadUris));
    context.subscriptions.push(new notebookSerializers.DotNetDibNotebookSerializer(clientMapper, outputChannel));
    if (vscodeUtilities.isStableBuild()) {
        context.subscriptions.push(new notebookSerializers.DotNetIpynbNotebookSerializer(clientMapper, outputChannel));
    }
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    notebookControllers.endExecution(cell, success);
}

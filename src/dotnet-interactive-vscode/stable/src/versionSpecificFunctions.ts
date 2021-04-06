// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as utilities from './common/utilities';
import * as notebookContentProvider from './common/vscode/notebookContentProvider';
import * as notebookKernel from './common/vscode/notebookKernel';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';

export function registerAdditionalContentProvider(context: vscode.ExtensionContext, contentProvider: vscode.NotebookContentProvider) {
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive-jupyter', contentProvider));
}

export function cellAt(document: vscode.NotebookDocument, index: number): vscode.NotebookCell {
    return document.cells[index];
}

export function cellCount(document: vscode.NotebookDocument): number {
    return document.cells.length;
}

export function getCells(document: vscode.NotebookDocument | undefined): Array<vscode.NotebookCell> {
    if (document) {
        return [...document.cells];
    }

    return [];
}

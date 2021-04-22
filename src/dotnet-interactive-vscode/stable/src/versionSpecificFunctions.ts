// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as contracts from './common/interfaces/contracts';
import * as vscodeLike from './common/interfaces/vscode-like';
import * as utilities from './common/utilities';
import * as notebookKernel from './notebookKernel';
import * as interactiveNotebook from './common/interactiveNotebook';
import * as diagnostics from './common/vscode/diagnostics';
import * as vscodeUtilities from './common/vscode/vscodeUtilities';
import { KernelId } from './common/vscode/extension';
import { DotNetInteractiveNotebookKernelProvider } from './notebookKernelProvider';
import { DotNetInteractiveNotebookContentProvider } from './notebookContentProvider';
import { ClientMapper } from './common/clientMapper';
import { OutputChannelAdapter } from './common/vscode/OutputChannelAdapter';
import { isStableBuild, offerToReOpen } from './common/vscode/vscodeUtilities';

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

export function registerWithVsCode(context: vscode.ExtensionContext, clientMapper: ClientMapper, diagnosticsChannel: OutputChannelAdapter, useJupyterExtension: boolean, ...preloadUris: vscode.Uri[]) {
    const selectorDib = {
        viewType: ['dotnet-interactive'],
        filenamePattern: '*.{dib,dotnet-interactive}'
    };
    const selectorIpynbWithJupyter = {
        viewType: ['jupyter-notebook'],
        filenamePattern: '*.ipynb'
    };
    const selectorIpynbWithDotNetInteractive = {
        viewType: ['dotnet-interactive-jupyter'],
        filenamePatter: '*.ipynb'
    };
    const notebookContentProvider = new DotNetInteractiveNotebookContentProvider(diagnosticsChannel, clientMapper);

    // notebook content
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive', notebookContentProvider));
    context.subscriptions.push(vscode.notebook.registerNotebookContentProvider('dotnet-interactive-jupyter', notebookContentProvider));
    const notebookKernelProvider = new DotNetInteractiveNotebookKernelProvider(preloadUris, clientMapper);
    context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selectorDib, notebookKernelProvider));
    if (useJupyterExtension) {
        context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selectorIpynbWithJupyter, notebookKernelProvider));
    }

    // always register as a possible .ipynb handler
    context.subscriptions.push(vscode.notebook.registerNotebookKernelProvider(selectorIpynbWithDotNetInteractive, notebookKernelProvider));

    context.subscriptions.push(vscode.notebook.onDidChangeActiveNotebookKernel(async e => await handleNotebookKernelChange(e, clientMapper)));
    context.subscriptions.push(vscode.notebook.onDidCloseNotebookDocument(notebookDocument => vscodeUtilities.resolveNotebookDocumentClose(notebookDocument)));
}

export function endExecution(cell: vscode.NotebookCell, success: boolean) {
    notebookKernel.endExecution(cell, success);
}

async function handleNotebookKernelChange(e: { document: vscode.NotebookDocument, kernel: vscode.NotebookKernel | undefined }, clientMapper: ClientMapper) {
    if (e.kernel?.id === KernelId) {
        try {
            // update various metadata
            await notebookKernel.updateDocumentKernelspecMetadata(e.document);
            await notebookKernel.updateCellLanguages(e.document);

            // force creation of the client so we don't have to wait for the user to execute a cell to get the tool
            await clientMapper.getOrAddClient(e.document.uri);
        } catch (err) {
            vscode.window.showErrorMessage(`Failed to set document metadata for '${e.document.uri}': ${err}`);
        }
    } else if (isStableBuild()) {
        const kernelspecName = e.document.metadata?.custom?.metadata?.kernelspec?.name;
        await offerToReOpen(e.document, kernelspecName);
    }
}

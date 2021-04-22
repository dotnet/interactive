// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { DotNetInteractiveNotebookKernel } from "./notebookKernel";
import { ClientMapper } from './common/clientMapper';
import { isDotNetKernelPreferred } from './common/utilities';
import { configureWebViewMessaging } from './notebookContentProvider';

export class DotNetInteractiveNotebookKernelProvider implements vscode.NotebookKernelProvider<DotNetInteractiveNotebookKernel> {
    private preferredKernel: DotNetInteractiveNotebookKernel;
    private nonPreferredKernel: DotNetInteractiveNotebookKernel;

    constructor(preloadUris: vscode.Uri[], readonly clientMapper: ClientMapper) {
        this.preferredKernel = new DotNetInteractiveNotebookKernel(clientMapper, preloadUris, true);
        this.nonPreferredKernel = new DotNetInteractiveNotebookKernel(clientMapper, preloadUris, false);
    }

    onDidChangeKernels?: vscode.Event<vscode.NotebookDocument | undefined> | undefined;

    provideKernels(document: vscode.NotebookDocument, token: vscode.CancellationToken): vscode.ProviderResult<DotNetInteractiveNotebookKernel[]> {
        if (isDotNetKernelPreferred(document.uri.fsPath, document.metadata)) {
            return [this.preferredKernel];
        } else {
            return [this.nonPreferredKernel];
        }
    }

    resolveKernel(kernel: DotNetInteractiveNotebookKernel, document: vscode.NotebookDocument, webview: vscode.NotebookCommunication, token: vscode.CancellationToken): vscode.ProviderResult<void> {
        configureWebViewMessaging(webview, document.uri, this.clientMapper);
    }
}

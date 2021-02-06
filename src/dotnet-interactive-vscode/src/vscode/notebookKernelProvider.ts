// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { DotNetInteractiveNotebookKernel } from "./notebookKernel";
import { configureWebViewMessaging } from "./vscodeUtilities";
import { ClientMapper } from '../clientMapper';
import { isDotNetKernelPreferred } from '../utilities';

export class DotNetInteractiveNotebookKernelProvider implements vscode.NotebookKernelProvider<DotNetInteractiveNotebookKernel> {
    private preferredKernel: DotNetInteractiveNotebookKernel;
    private nonPreferredKernel: DotNetInteractiveNotebookKernel;

    constructor(readonly apiBootstrapperUri: vscode.Uri, readonly clientMapper: ClientMapper) {
        this.preferredKernel = new DotNetInteractiveNotebookKernel(clientMapper, this.apiBootstrapperUri, true);
        this.nonPreferredKernel = new DotNetInteractiveNotebookKernel(clientMapper, this.apiBootstrapperUri, false);
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

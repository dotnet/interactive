// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

import { DotNetInteractiveNotebookKernel } from "./notebookKernel";
import { configureWebViewMessaging } from "./vscodeUtilities";
import { ClientMapper } from '../clientMapper';

export class DotNetInteractiveNotebookKernelProvider implements vscode.NotebookKernelProvider<DotNetInteractiveNotebookKernel> {
    constructor(readonly kernel: DotNetInteractiveNotebookKernel, readonly clientMapper: ClientMapper) {
    }

    onDidChangeKernels?: vscode.Event<vscode.NotebookDocument | undefined> | undefined;

    provideKernels(document: vscode.NotebookDocument, token: vscode.CancellationToken): vscode.ProviderResult<DotNetInteractiveNotebookKernel[]> {
        return [this.kernel];
    }

    resolveKernel(kernel: DotNetInteractiveNotebookKernel, document: vscode.NotebookDocument, webview: vscode.NotebookCommunication, token: vscode.CancellationToken): vscode.ProviderResult<void> {
        configureWebViewMessaging(webview, document.uri, this.clientMapper);
    }
}

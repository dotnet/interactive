// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as vscodeLike from './interfaces/vscode-like';
import * as connection from './polyglot-notebooks/connection';
import { InteractiveClient } from './interactiveClient';
import { CompositeKernel } from './polyglot-notebooks/compositeKernel';
import { ClientMapper } from './clientMapper';

export type KernelInfoUpdatedDelegate = { (notebook: vscode.NotebookDocument, client: InteractiveClient): void };

export class KernelInfoUpdaterService {
    private _callbacks: KernelInfoUpdatedDelegate[] = [];
    private _compositeKernelToNotebookUri: Map<CompositeKernel, vscodeLike.Uri> = new Map();

    constructor(private readonly clientMapper: ClientMapper) {
        clientMapper.onClientCreate((uri, client) => {
            this._compositeKernelToNotebookUri.set(client.kernel, uri);
        });

        connection.registerForKernelInfoUpdates(async (compositeKernel) => {
            const notebookUri = this._compositeKernelToNotebookUri.get(compositeKernel);
            if (notebookUri) {
                const notebookDocument = vscode.workspace.notebookDocuments.find(document => document.uri.fsPath === notebookUri.fsPath);
                if (notebookDocument) {
                    const client = await this.clientMapper.getOrAddClient(notebookDocument.uri);
                    this.notifyOfKernelInfoUpdates(notebookDocument, client);
                }
            }
        });
    }

    private notifyOfKernelInfoUpdates(notebook: vscode.NotebookDocument, client: InteractiveClient) {
        for (const callback of this._callbacks) {
            try {
                callback(notebook, client);
            } catch {
                // don't care
            }
        }
    }

    onKernelInfoUpdated(callback: KernelInfoUpdatedDelegate) {
        this._callbacks.push(callback);
        return {
            dispose: () => {
                this._callbacks = this._callbacks.filter(d => d !== callback);
            }
        };
    }
}

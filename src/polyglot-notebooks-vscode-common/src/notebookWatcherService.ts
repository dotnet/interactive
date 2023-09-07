// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import { InteractiveClient } from './interactiveClient';
import { Disposable, Logger } from './polyglot-notebooks';

export type NotebookDelegate = { (notebook: vscode.NotebookDocument, client: InteractiveClient): void };

export class NotebookWatcherService {
    private notebookOpenDelegates: NotebookDelegate[] = [];
    private notebookCloseDelegates: NotebookDelegate[] = [];

    constructor(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
        context.subscriptions.push(vscode.workspace.onDidCloseNotebookDocument(async notebook => {
            const client = await clientMapper.tryGetClient(notebook.uri);
            if (client) {
                // if one of ours...
                this.notebookDocumentClosed(notebook, client);
            }
        }));
        clientMapper.onClientCreate((uri, client) => {
            const notebooks = vscode.workspace.notebookDocuments.filter(n => n.uri.fsPath === uri.fsPath);
            if (notebooks.length === 1) {
                const notebook = notebooks[0];
                this.notebookDocumentOpened(notebook, client);
            } else {
                Logger.default.error(`Unable to find single notebook for URI ${uri.toString()}`);
            }
        });
    }

    onNotebookDocumentOpened(callback: NotebookDelegate): Disposable {
        this.notebookOpenDelegates.push(callback);
        return {
            dispose: () => {
                this.notebookOpenDelegates = this.notebookOpenDelegates.filter(d => d !== callback);
            }
        };
    }

    onNotebookDocumentClosed(callback: NotebookDelegate): Disposable {
        this.notebookCloseDelegates.push(callback);
        return {
            dispose: () => {
                this.notebookCloseDelegates = this.notebookCloseDelegates.filter(d => d !== callback);
            }
        };
    }

    private notebookDocumentOpened(notebook: vscode.NotebookDocument, client: InteractiveClient) {
        for (const callback of this.notebookOpenDelegates) {
            try {
                callback(notebook, client);
            } catch (e) {
                Logger.default.error(`Error calling notebook open delegate for ${notebook.uri.toString()}: ${e}`);
            }
        }
    }

    private notebookDocumentClosed(notebook: vscode.NotebookDocument, client: InteractiveClient) {
        for (const callback of this.notebookCloseDelegates) {
            try {
                callback(notebook, client);
            } catch (e) {
                Logger.default.error(`Error calling notebook close delegate for ${notebook.uri.toString()}: ${e}`);
            }
        }
    }

    dispose() {
    }
}

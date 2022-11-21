// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { InteractiveClient } from './interactiveClient';

export class ActiveNotebookTracker {
    private activeClients: Map<vscodeLike.Uri, InteractiveClient> = new Map();

    constructor(context: vscode.ExtensionContext, private readonly clientMapper: ClientMapper) {
        context.subscriptions.push(vscode.workspace.onDidCloseNotebookDocument(notebook => this.notebookDocumentClosed(notebook)));
        clientMapper.onClientCreate((uri, client) => this.notebookDocumentCreated(uri, client));
    }

    private notebookDocumentClosed(notebook: vscode.NotebookDocument) {
        this.activeClients.delete(notebook.uri);
        this.clientMapper.closeClient(notebook.uri, true);
    }

    private notebookDocumentCreated(uri: vscodeLike.Uri, client: InteractiveClient) {
        this.activeClients.set(uri, client);
    }

    dispose() {
        this.activeClients.forEach(client => client.dispose());
        this.activeClients.clear();
    }
}

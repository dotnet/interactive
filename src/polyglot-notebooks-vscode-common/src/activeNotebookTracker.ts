// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { InteractiveClient } from './interactiveClient';
import { ServiceCollection } from './serviceCollection';
import * as diagnostics from './diagnostics';

export class ActiveNotebookTracker {
    private activeClients: Map<vscodeLike.Uri, InteractiveClient> = new Map();

    constructor(private readonly clientMapper: ClientMapper) {
        ServiceCollection.Instance.NotebookWatcher.onNotebookDocumentOpened((notebook, client) => this.notebookDocumentOpened(notebook, client));
        ServiceCollection.Instance.NotebookWatcher.onNotebookDocumentClosed((notebook, client) => this.notebookDocumentClosed(notebook, client));
    }

    private notebookDocumentOpened(notebook: vscode.NotebookDocument, client: InteractiveClient) {
        this.activeClients.set(notebook.uri, client);
    }

    private notebookDocumentClosed(notebook: vscode.NotebookDocument, client: InteractiveClient) {
        this.activeClients.delete(notebook.uri);
        this.clientMapper.closeClient(notebook.uri, true);
        for (const cell of notebook.getCells()) {
            diagnostics.getDiagnosticCollection(cell.document.uri).set(cell.document.uri, undefined);
        }
    }

    dispose() {
        this.activeClients.forEach(client => client.dispose());
        this.activeClients.clear();
    }
}

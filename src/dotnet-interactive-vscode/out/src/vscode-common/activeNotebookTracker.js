"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.ActiveNotebookTracker = void 0;
const vscode = require("vscode");
class ActiveNotebookTracker {
    constructor(context, clientMapper) {
        this.clientMapper = clientMapper;
        this.activeClients = new Map();
        context.subscriptions.push(vscode.workspace.onDidCloseNotebookDocument(notebook => this.notebookDocumentClosed(notebook)));
        clientMapper.onClientCreate((uri, client) => this.notebookDocumentCreated(uri, client));
    }
    notebookDocumentClosed(notebook) {
        this.activeClients.delete(notebook.uri);
        this.clientMapper.closeClient(notebook.uri, true);
    }
    notebookDocumentCreated(uri, client) {
        this.activeClients.set(uri, client);
    }
    dispose() {
        this.activeClients.forEach(client => client.dispose());
        this.activeClients.clear();
    }
}
exports.ActiveNotebookTracker = ActiveNotebookTracker;
//# sourceMappingURL=activeNotebookTracker.js.map
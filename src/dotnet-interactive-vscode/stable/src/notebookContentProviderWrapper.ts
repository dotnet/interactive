// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { backupNotebook, defaultNotebookCellLanguage } from './common/interactiveNotebook';
import { OutputChannelAdapter } from './common/vscode/OutputChannelAdapter';

// a thin wrapper around the new `vscode.NotebookSerializer` api
export class DotNetNotebookContentProviderWrapper implements vscode.NotebookContentProvider {
    constructor(private readonly serializer: vscode.NotebookSerializer, private readonly outputChannel: OutputChannelAdapter) {
    }

    async openNotebook(uri: vscode.Uri, openContext: vscode.NotebookDocumentOpenContext, token: vscode.CancellationToken): Promise<vscode.NotebookData> {
        let fileUri: vscode.Uri | undefined = uri.scheme === 'untitled'
            ? undefined
            : uri;
        if (openContext.backupId) {
            // restoring a backed up notebook

            // N.B., when F5 debugging, the `backupId` property is _always_ `undefined`, so to properly test this you'll
            // have to build and install a VSIX.
            fileUri = vscode.Uri.file(openContext.backupId);
        }

        let notebookData: vscode.NotebookData | undefined = undefined;
        if (fileUri && fs.existsSync(fileUri.fsPath)) {
            // file on disk
            try {
                const buffer = Buffer.from(await vscode.workspace.fs.readFile(fileUri));
                notebookData = await this.serializer.deserializeNotebook(buffer, token);
            } catch (e) {
                vscode.window.showErrorMessage(`Error opening file '${fileUri.fsPath}'; check the '${this.outputChannel.getName()}' output channel for details`);
                this.outputChannel.appendLine(`Error opening file '${fileUri.fsPath}':\n${e?.message}`);
            }
        } else {
            // new empty/blank notebook; nothing to do
        }

        if (!notebookData) {
            notebookData = new vscode.NotebookData([]);
        }

        if (notebookData.cells.length === 0) {
            // ensure at least one cell
            notebookData = new vscode.NotebookData([{
                kind: vscode.NotebookCellKind.Code,
                source: '',
                language: defaultNotebookCellLanguage,
            }]);
        }

        return notebookData;
    }

    saveNotebook(document: vscode.NotebookDocument, token: vscode.CancellationToken): Thenable<void> {
        return this.saveNotebookToUri(document, document.uri, token);
    }

    saveNotebookAs(targetResource: vscode.Uri, document: vscode.NotebookDocument, token: vscode.CancellationToken): Thenable<void> {
        return this.saveNotebookToUri(document, targetResource, token);
    }

    async backupNotebook(document: vscode.NotebookDocument, context: vscode.NotebookDocumentBackupContext, token: vscode.CancellationToken): Promise<vscode.NotebookDocumentBackup> {
        const extension = path.extname(document.uri.fsPath);
        const content = await this.notebookAsUint8Array(document, token);
        return backupNotebook(content, context.destination.fsPath + extension);
    }

    private async notebookAsUint8Array(document: vscode.NotebookDocument, token: vscode.CancellationToken): Promise<Uint8Array> {
        const notebookData: vscode.NotebookData = {
            cells: document.getCells().map(cell => new vscode.NotebookCellData(cell.kind, cell.document.getText(), cell.document.languageId)),
            metadata: new vscode.NotebookDocumentMetadata(),
        };
        const content = await this.serializer.serializeNotebook(notebookData, token);
        return content;
    }

    private async saveNotebookToUri(document: vscode.NotebookDocument, uri: vscode.Uri, token: vscode.CancellationToken): Promise<void> {
        const content = await this.notebookAsUint8Array(document, token);
        await vscode.workspace.fs.writeFile(uri, content);
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './../clientMapper';
import { parseNotebook, serializeNotebook, notebookCellLanguages, getSimpleLanguage, getNotebookSpecificLanguage, languageToCellKind, backupNotebook, asNotebookFile } from '../interactiveNotebook';
import { RawNotebookCell } from '../interfaces';
import { ReportChannel } from '../interfaces/vscode';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider, vscode.NotebookKernel {
    private readonly onDidChangeNotebookEventEmitter = new vscode.EventEmitter<vscode.NotebookDocumentEditEvent>();

    kernel: vscode.NotebookKernel;
    label: string;

    constructor(readonly clientMapper: ClientMapper, private readonly globalChannel : ReportChannel) {
        this.kernel = this;
        this.label = ".NET Interactive";
    }

    preloads?: vscode.Uri[] | undefined;

    async executeAllCells(document: vscode.NotebookDocument, token: vscode.CancellationToken): Promise<void> {
        for (let cell of document.cells) {
            await this.executeCell(document, cell, token);
        }
    }

    async openNotebook(uri: vscode.Uri): Promise<vscode.NotebookData> {
        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(uri)).toString('utf-8');
        } catch {
        }

        let notebook = parseNotebook(contents);
        await this.clientMapper.getOrAddClient(uri);

        let notebookData: vscode.NotebookData = {
            languages: notebookCellLanguages,
            metadata: {
                cellHasExecutionOrder: false
            },
            cells: notebook.cells.map(toNotebookCellData)
        };

        return notebookData;
    }

    resolveNotebook(document: vscode.NotebookDocument, webview: vscode.NotebookCommunication): Promise<void> {
        return Promise.resolve();
    }

    saveNotebook(document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, document.uri);
    }

    saveNotebookAs(targetResource: vscode.Uri, document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, targetResource);
    }

    onDidChangeNotebook: vscode.Event<vscode.NotebookDocumentEditEvent> = this.onDidChangeNotebookEventEmitter.event;

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell, token: vscode.CancellationToken): Promise<void> {
        const startTime = Date.now();
        cell.metadata.runStartTime = startTime;
        cell.metadata.runState = vscode.NotebookCellRunState.Running;
        cell.outputs = [];
        let client = await this.clientMapper.getOrAddClient(document.uri);
        let source = cell.document.getText();
        return client.execute(source, getSimpleLanguage(cell.language), outputs => {
            // to properly trigger the UI update, `cell.outputs` needs to be uniquely assigned; simply setting it to the local variable has no effect
            cell.outputs = [];
            cell.outputs = outputs;
        }).then(() => {
            cell.metadata.runState = vscode.NotebookCellRunState.Success;
            cell.metadata.lastRunDuration = Date.now() - startTime;
        }).catch(() => {
            cell.metadata.runState = vscode.NotebookCellRunState.Error;
            cell.metadata.lastRunDuration = Date.now() - startTime;
        });
    }

    backupNotebook(document: vscode.NotebookDocument, context: vscode.NotebookDocumentBackupContext, cancellation: vscode.CancellationToken): Promise<vscode.NotebookDocumentBackup> {
        return backupNotebook(document, context.destination.fsPath);
    }

    private async save(document: vscode.NotebookDocument, targetResource: vscode.Uri): Promise<void> {
        const notebook = asNotebookFile(document);
        const buffer = Buffer.from(serializeNotebook(notebook));
        await vscode.workspace.fs.writeFile(targetResource, buffer);
    }
}

function toNotebookCellData(cell: RawNotebookCell): vscode.NotebookCellData {
    return {
        cellKind: languageToCellKind(cell.language),
        source: cell.contents.join('\n'),
        language: getNotebookSpecificLanguage(cell.language),
        outputs: [],
        metadata: {}
    };
}

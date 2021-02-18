// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from '../clientMapper';
import { notebookCellLanguages, getSimpleLanguage, getNotebookSpecificLanguage, languageToCellKind, backupNotebook } from '../interactiveNotebook';
import { Eol } from '../interfaces';
import { NotebookCell, NotebookCellOutput, NotebookDocument } from 'dotnet-interactive-vscode-interfaces/out/contracts';
import { configureWebViewMessaging, getEol, isInsidersBuild, isUnsavedNotebook } from './vscodeUtilities';

import * as vscodeInsiders from 'dotnet-interactive-vscode-insiders/out/functions';
import * as vscodeStable from 'dotnet-interactive-vscode-stable/out/functions';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider {

    private readonly onDidChangeNotebookEventEmitter = new vscode.EventEmitter<vscode.NotebookDocumentEditEvent>();
    eol: Eol;

    constructor(readonly clientMapper: ClientMapper) {
        this.eol = getEol();
    }

    async openNotebook(uri: vscode.Uri, openContext: vscode.NotebookDocumentOpenContext): Promise<vscode.NotebookData> {
        let fileUri: vscode.Uri | undefined = isUnsavedNotebook(uri)
            ? undefined
            : uri;
        if (openContext.backupId) {
            // restoring a backed up notebook

            // N.B., when F5 debugging, the `backupId` property is _always_ `undefined`, so to properly test this you'll
            // have to build and install a VSIX.
            fileUri = vscode.Uri.file(openContext.backupId);
        }

        const client = await this.clientMapper.getOrAddClient(uri);
        let notebookCells: Array<NotebookCell>;
        if (fileUri) {
            // file on disk
            let buffer = new Uint8Array();
            try {
                buffer = Buffer.from(await vscode.workspace.fs.readFile(fileUri));
            } catch {
            }

            const fileName = path.basename(fileUri.fsPath);
            const notebook = await client.parseNotebook(fileName, buffer);
            notebookCells = notebook.cells;
        } else {
            // new empty/blank notebook
            notebookCells = [];
        }

        const notebookData = this.createNotebookData(notebookCells);
        return notebookData;
    }

    private createNotebookData(cells: Array<NotebookCell>): vscode.NotebookData {
        const notebookData: vscode.NotebookData = {
            languages: notebookCellLanguages,
            metadata: {
                cellHasExecutionOrder: false
            },
            cells: cells.map(toVsCodeNotebookCellData)
        };

        return notebookData;
    }

    async resolveNotebook(document: vscode.NotebookDocument, webview: vscode.NotebookCommunication): Promise<void> {
        configureWebViewMessaging(webview, document.uri, this.clientMapper);
    }

    saveNotebook(document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, document.uri);
    }

    saveNotebookAs(targetResource: vscode.Uri, document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, targetResource);
    }

    onDidChangeNotebook: vscode.Event<vscode.NotebookDocumentEditEvent> = this.onDidChangeNotebookEventEmitter.event;

    async backupNotebook(document: vscode.NotebookDocument, context: vscode.NotebookDocumentBackupContext, cancellation: vscode.CancellationToken): Promise<vscode.NotebookDocumentBackup> {
        const extension = path.extname(document.uri.fsPath);
        const buffer = await this.serializeNotebook(document, document.uri);
        const documentBackup = await backupNotebook(buffer, context.destination.fsPath + extension);
        return documentBackup;
    }

    private async save(document: vscode.NotebookDocument, targetResource: vscode.Uri): Promise<void> {
        const buffer = await this.serializeNotebook(document, targetResource);
        await vscode.workspace.fs.writeFile(targetResource, buffer);
    }

    private async serializeNotebook(document: vscode.NotebookDocument, uri: vscode.Uri): Promise<Uint8Array> {
        // might be an unsaved notebook being assigned a filename for the first time
        this.clientMapper.reassociateClient(document.uri, uri);

        const client = await this.clientMapper.getOrAddClient(uri);
        const fileName = path.basename(uri.fsPath);
        const notebook = toNotebookDocument(document);
        const buffer = await client.serializeNotebook(fileName, notebook, this.eol);
        return buffer;
    }
}

function toVsCodeNotebookCellData(cell: NotebookCell): vscode.NotebookCellData {
    return {
        cellKind: languageToCellKind(cell.language),
        source: cell.contents,
        language: getNotebookSpecificLanguage(cell.language),
        outputs: cell.outputs.map(toVsCodeNotebookCellOutput),
        metadata: {}
    };
}

function toVsCodeNotebookCellOutput(output: NotebookCellOutput): vscode.CellOutput {
    if (isInsidersBuild()) {
        return <vscode.CellOutput><any>vscodeInsiders.contractCellOutputToVsCodeCellOutput(output);
    } else {
        return vscodeStable.contractCellOutputToVsCodeCellOutput(output);
    }
}

export function toNotebookDocument(document: vscode.NotebookDocument): NotebookDocument {
    return {
        cells: document.cells.map(toNotebookCell)
    };
}

function toNotebookCell(cell: vscode.NotebookCell): NotebookCell {
    return {
        language: getSimpleLanguage(cell.language),
        contents: cell.document.getText(),
        outputs: cell.outputs.map(toNotebookCellOutput)
    };
}

function toNotebookCellOutput(output: vscode.CellOutput): NotebookCellOutput {
    if (isInsidersBuild()) {
        return vscodeInsiders.vsCodeCellOutputToContractCellOutput(<vscode.NotebookCellOutput><any>output);
    } else {
        return vscodeStable.vsCodeCellOutputToContractCellOutput(output);
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from './common/clientMapper';
import { getSimpleLanguage, getNotebookSpecificLanguage, languageToCellKind, backupNotebook, defaultNotebookCellLanguage } from './common/interactiveNotebook';
import { Eol } from './common/interfaces';
import { NotebookCell, NotebookCellDisplayOutput, NotebookCellErrorOutput, NotebookCellOutput, NotebookDocument } from './common/interfaces/contracts';
import * as utilities from './common/interfaces/utilities';

import { isIpynbFile, validateNotebookShape } from './common/ipynbUtilities';
import * as vscodeLike from './common/interfaces/vscode-like';
import { getEol, isUnsavedNotebook, toNotebookDocument } from './common/vscode/vscodeUtilities';

import { OutputChannelAdapter } from './common/vscode/OutputChannelAdapter';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider {

    eol: Eol;

    constructor(readonly outputChannel: OutputChannelAdapter, readonly clientMapper: ClientMapper) {
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
        let notebookCells: Array<NotebookCell> = [];
        if (fileUri && fs.existsSync(fileUri.fsPath)) {
            // file on disk
            try {
                const buffer = Buffer.from(await vscode.workspace.fs.readFile(fileUri));
                const fileName = path.basename(fileUri.fsPath);
                const notebook = await client.parseNotebook(fileName, buffer);
                notebookCells = notebook.cells;

                // peek at kernelspec to see if it's possibly not us
                if (isIpynbFile(fileUri.fsPath)) {
                    const notebookObject = JSON.parse(buffer.toString());
                    validateNotebookShape(
                        notebookObject,
                        (isError, message) => {
                            if (isError) {
                                vscode.window.showErrorMessage(message);
                            } else {
                                vscode.window.showWarningMessage(message);
                            }
                        });
                }
            } catch (e) {
                vscode.window.showErrorMessage(`Error opening file '${fileUri.fsPath}'; check the '${this.outputChannel.getName()}' output channel for details`);
                this.outputChannel.appendLine(`Error opening file '${fileUri.fsPath}':\n${e?.message}`);
            }
        } else {
            // new empty/blank notebook, nothing to do
        }

        if (notebookCells.length === 0) {
            // ensure at least one cell
            notebookCells.push({
                language: defaultNotebookCellLanguage,
                contents: '',
                outputs: [],
            });
        }

        const notebookData = this.createNotebookData(notebookCells);
        return notebookData;
    }

    private createNotebookData(cells: Array<NotebookCell>): vscode.NotebookData {
        const notebookData: vscode.NotebookData = {
            metadata: new vscode.NotebookDocumentMetadata().with({ cellHasExecutionOrder: false }),
            cells: cells.map(toVsCodeNotebookCellData)
        };
        return notebookData;
    }

    // soon to be removed; already exists in kernel provider
    async resolveNotebook(document: vscode.NotebookDocument, webview: vscode.NotebookCommunication): Promise<void> {
        configureWebViewMessaging(webview, document.uri, this.clientMapper);
    }

    saveNotebook(document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, document.uri);
    }

    saveNotebookAs(targetResource: vscode.Uri, document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, targetResource);
    }

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
    return new vscode.NotebookCellData(
        <number>languageToCellKind(cell.language),
        cell.contents,
        getNotebookSpecificLanguage(cell.language),
        cell.outputs.map(contractCellOutputToVsCodeCellOutput),
    );
}

export function contractCellOutputToVsCodeCellOutput(output: NotebookCellOutput): vscode.NotebookCellOutput {
    const outputItems: Array<vscode.NotebookCellOutputItem> = [];
    if (utilities.isDisplayOutput(output)) {
        for (const mimeKey in output.data) {
            outputItems.push(generateVsCodeNotebookCellOutputItem(mimeKey, output.data[mimeKey]));
        }
    } else if (utilities.isErrorOutput(output)) {
        outputItems.push(generateVsCodeNotebookCellOutputItem(vscodeLike.ErrorOutputMimeType, output.errorValue));
    } else if (utilities.isTextOutput(output)) {
        outputItems.push(generateVsCodeNotebookCellOutputItem('text/plain', output.text));
    }

    return new vscode.NotebookCellOutput(outputItems);
}

export function generateVsCodeNotebookCellOutputItem(mimeType: string, value: unknown): vscode.NotebookCellOutputItem {
    const displayValue = utilities.reshapeOutputValueForVsCode(mimeType, value);
    return new vscode.NotebookCellOutputItem(mimeType, displayValue);
}

export function configureWebViewMessaging(webview: vscode.NotebookCommunication, documentUri: vscode.Uri, clientMapper: ClientMapper) {
    webview.onDidReceiveMessage(async (message) => {
        switch (message.command) {
            case "getHttpApiEndpoint":
                const client = await clientMapper.tryGetClient(documentUri);
                if (client) {
                    const uri = client.tryGetProperty<vscode.Uri>("externalUri");
                    webview.postMessage({ command: "configureFactories", endpointUri: uri?.toString() });

                    clientMapper.onClientCreate(documentUri, async (client) => {
                        const uri = client.tryGetProperty<vscode.Uri>("externalUri");
                        await webview.postMessage({ command: "resetFactories", endpointUri: uri?.toString() });
                    });
                }
                break;
        }
    });
}

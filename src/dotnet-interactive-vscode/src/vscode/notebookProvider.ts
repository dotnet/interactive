// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as path from 'path';
import { ClientMapper } from './../clientMapper';
import { NotebookFile, parseNotebook, serializeNotebook, editorLanguageAliases } from '../interactiveNotebook';
import { RawNotebookCell } from '../interfaces';
import { trimTrailingCarriageReturn } from '../utilities';
import { DisplayedValueProducedType, ReturnValueProducedType, DisplayEventBase } from '../contracts';
import { CellOutput, ReportChannel } from '../interfaces/vscode';
import { displayEventToCellOutput } from '../interactiveClient';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider {
    private deferredOutput: Array<CellOutput> = [];
    private readonly onDidChangeNotebookEventEmitter = new vscode.EventEmitter<vscode.NotebookDocumentEditEvent>();

    constructor(readonly clientMapper: ClientMapper, private readonly globalChannel : ReportChannel) {
    }

    async openNotebook(uri: vscode.Uri): Promise<vscode.NotebookData> {
        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(uri)).toString('utf-8');
        } catch {
        }

        let notebook = parseNotebook(contents);
        let client = this.clientMapper.getOrAddClient(uri);
        let notebookPath = path.dirname(uri.fsPath);

        let notebookData: vscode.NotebookData = {
            languages: Array.from(editorLanguageAliases.keys()),
            metadata: {
                hasExecutionOrder: false
            },
            cells: notebook.cells.map(toNotebookCellData)
        };

        client.setDeferredCommandEventsListener((eventEnvelope) => {
            switch (eventEnvelope.eventType) {
                case DisplayedValueProducedType:
                case DisplayedValueProducedType:
                case ReturnValueProducedType:
                    let disp = <DisplayEventBase>eventEnvelope.event;
                    let output = displayEventToCellOutput(disp);
                    this.deferredOutput.push(output);
                    break;
            }

        });

        client.changeWorkingDirectory(notebookPath).catch((err) => {
            let message = `Unable to set notebook working directory to '${notebookPath}'.`;
            let detailedMessage = message;
            if (err && err.message) {
                detailedMessage += '\n' + err.message.toString();
            }
            this.globalChannel.appendLine(detailedMessage);
            vscode.window.showInformationMessage(`${message} See output withdow "${this.globalChannel.getName()}"`);
        });

        return notebookData;
    }

    saveNotebook(document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, document.uri);
    }

    saveNotebookAs(targetResource: vscode.Uri, document: vscode.NotebookDocument, _cancellation: vscode.CancellationToken): Promise<void> {
        return this.save(document, targetResource);
    }

    onDidChangeNotebook: vscode.Event<vscode.NotebookDocumentEditEvent> = this.onDidChangeNotebookEventEmitter.event;

    executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell | undefined, token: vscode.CancellationToken): Promise<void> {
        if (!cell) {
            // TODO: run everything
            return new Promise((resolve, reject) => resolve());
        }

        cell.outputs = [];
        let client = this.clientMapper.getOrAddClient(document.uri);
        let source = cell.source.toString();
        return client.execute(source, cell.language, outputs => {
            let cellOutput = outputs;
            if (this.deferredOutput.length) {
                cellOutput = [...this.deferredOutput, ...outputs];
                this.deferredOutput = [];
            }

            // to properly trigger the UI update, `cell.outputs` needs to be uniquely assigned; simply setting it to the local variable has no effect
            cell.outputs = [];
            cell.outputs = cellOutput;
        });
    }

    private async save(document: vscode.NotebookDocument, targetResource: vscode.Uri): Promise<void> {
        let notebook: NotebookFile = {
            cells: [],
        };
        for (let cell of document.cells) {
            notebook.cells.push({
                language: cell.language,
                contents: cell.document.getText().split('\n').map(trimTrailingCarriageReturn),
            });
        }

        let buffer = Buffer.from(serializeNotebook(notebook));
        await vscode.workspace.fs.writeFile(targetResource, buffer);
    }
}

function toNotebookCellData(cell: RawNotebookCell): vscode.NotebookCellData {
    return {
        cellKind: languageToCellKind(cell.language),
        source: cell.contents.join('\n'),
        language: cell.language,
        outputs: [],
        metadata: {}
    };
}

function languageToCellKind(language: string): vscode.CellKind {
    switch (language) {
        case 'md':
        case 'markdown':
            return vscode.CellKind.Markdown;
        default:
            return vscode.CellKind.Code;
    }
}

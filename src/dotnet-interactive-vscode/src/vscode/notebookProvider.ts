// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from './../clientMapper';
import { notebookCellLanguages, getSimpleLanguage, getNotebookSpecificLanguage, languageToCellKind, backupNotebook } from '../interactiveNotebook';
import { Eol } from '../interfaces';
import { CellOutput } from '../interfaces/vscode';
import { Diagnostic, DiagnosticSeverity, NotebookCell, NotebookCellDisplayOutput, NotebookCellErrorOutput, NotebookCellOutput, NotebookCellTextOutput, NotebookDocument } from './../contracts';
import { getEol, toVsCodeDiagnostic } from './vscodeUtilities';
import { getDiagnosticCollection } from './diagnostics';
import { isDisplayOutput, isErrorOutput, isTextOutput } from '../utilities';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider, vscode.NotebookKernel, vscode.NotebookKernelProvider<DotNetInteractiveNotebookContentProvider> {
    private readonly onDidChangeNotebookEventEmitter = new vscode.EventEmitter<vscode.NotebookDocumentEditEvent>();

    eol: Eol;
    label: string;

    constructor(readonly clientMapper: ClientMapper) {
        this.label = ".NET Interactive";
        this.eol = getEol();
    }

    preloads?: vscode.Uri[] | undefined;

    provideKernels(document: vscode.NotebookDocument, token: vscode.CancellationToken): vscode.ProviderResult<DotNetInteractiveNotebookContentProvider[]> {
        if (document.uri.scheme === "untitled")
        {
            return [this];
        }

        const extension = path.extname(document.fileName).toLowerCase();
        switch (extension) {
            case '.dib':
            case '.dotnet-interactive':
            case '.ipynb':
                return [this];
            default:
                return [];
        }
    }

    async executeAllCells(document: vscode.NotebookDocument): Promise<void> {
        for (let cell of document.cells) {
            await this.executeCell(document, cell);
        }
    }

    cancelAllCellsExecution(document: vscode.NotebookDocument) {
        // not supported
    }

    async openNotebook(uri: vscode.Uri): Promise<vscode.NotebookData> {
        let buffer = new Uint8Array();
        try {
            buffer = Buffer.from(await vscode.workspace.fs.readFile(uri));
        } catch {
        }

        const client = await this.clientMapper.getOrAddClient(uri);
        const fileName = path.basename(uri.fsPath);
        const notebook = await client.parseNotebook(fileName, buffer);

        let notebookData: vscode.NotebookData = {
            languages: notebookCellLanguages,
            metadata: {
                cellHasExecutionOrder: false
            },
            cells: notebook.cells.map(toVsCodeNotebookCellData)
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

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        const startTime = Date.now();
        DotNetInteractiveNotebookContentProvider.updateCellMetadata(document, cell, {
            runStartTime: startTime,
            runState: vscode.NotebookCellRunState.Running,
        });
        DotNetInteractiveNotebookContentProvider.updateCellOutputs(document, cell, []);
        let client = await this.clientMapper.getOrAddClient(document.uri);
        let source = cell.document.getText();
        function outputObserver(outputs: Array<CellOutput>) {
            DotNetInteractiveNotebookContentProvider.updateCellOutputs(document, cell, outputs);
        }

        let diagnosticCollection = getDiagnosticCollection(cell.uri);
        function diagnosticObserver(diags: Array<Diagnostic>) {
            diagnosticCollection.set(cell.uri, diags.filter(d => d.severity !== DiagnosticSeverity.Hidden).map(toVsCodeDiagnostic));
        }
        return client.execute(source, getSimpleLanguage(cell.language), outputObserver, diagnosticObserver).then(() => {
            DotNetInteractiveNotebookContentProvider.updateCellMetadata(document, cell, {
                runState: vscode.NotebookCellRunState.Success,
                lastRunDuration: Date.now() - startTime,
            });
        }).catch(() => {
            DotNetInteractiveNotebookContentProvider.updateCellMetadata(document, cell, {
                runState: vscode.NotebookCellRunState.Error,
                lastRunDuration: Date.now() - startTime,
            });
        });
    }

    static updateCellMetadata(document: vscode.NotebookDocument, cell: vscode.NotebookCell, metadata: vscode.NotebookCellMetadata) {
        cell.metadata = metadata;
        // const index = document.cells.findIndex(c => c === cell);
        // if (index >= 0) {
        //     vscode.notebook.activeNotebookEditor?.edit(editBuilder => {
        //         editBuilder.replaceMetadata(index, metadata);
        //     });
        // }
    }

    static updateCellOutputs(document: vscode.NotebookDocument, cell: vscode.NotebookCell, outputs: vscode.CellOutput[]) {
        cell.outputs = [];
        cell.outputs = outputs;
        // const index = document.cells.findIndex(c => c === cell);
        // if (index >= 0) {
        //     vscode.notebook.activeNotebookEditor?.edit(editBuilder => {
        //         editBuilder.replaceOutput(index, outputs);
        //     });
        // }
    }

    cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell) {
        // not supported
    }

    async backupNotebook(document: vscode.NotebookDocument, context: vscode.NotebookDocumentBackupContext, cancellation: vscode.CancellationToken): Promise<vscode.NotebookDocumentBackup> {
        const buffer = await this.serializeNotebook(document, document.uri);
        const documentBackup = await backupNotebook(buffer, context.destination.fsPath);
        return documentBackup;
    }

    private async save(document: vscode.NotebookDocument, targetResource: vscode.Uri): Promise<void> {
        const buffer = await this.serializeNotebook(document, targetResource);
        await vscode.workspace.fs.writeFile(targetResource, buffer);
    }

    private async serializeNotebook(document: vscode.NotebookDocument, uri: vscode.Uri): Promise<Uint8Array> {
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
    if (isDisplayOutput(output)) {
        return {
            outputKind: vscode.CellOutputKind.Rich,
            data: output.data
        };
    } else if (isErrorOutput(output)) {
        return {
            outputKind: vscode.CellOutputKind.Error,
            ename: output.errorName,
            evalue: output.errorValue,
            traceback: output.stackTrace
        };
    } else if (isTextOutput(output)) {
        return {
            outputKind: vscode.CellOutputKind.Text,
            text: output.text
        };
    }

    // unknown, better to return _something_ than to fail entirely
    return {
        outputKind: vscode.CellOutputKind.Rich,
        data: {}
    };
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
    switch (output.outputKind) {
        case vscode.CellOutputKind.Error:
            const error: NotebookCellErrorOutput = {
                errorName: output.ename,
                errorValue: output.evalue,
                stackTrace: output.traceback
            };
            return error;
        case vscode.CellOutputKind.Rich:
            const rich: NotebookCellDisplayOutput = {
                data: output.data
            };
            return rich;
        case vscode.CellOutputKind.Text:
            const text: NotebookCellTextOutput = {
                text: output.text
            };
            return text;
    }
}

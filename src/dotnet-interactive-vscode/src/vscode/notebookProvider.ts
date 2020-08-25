// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { ClientMapper } from './../clientMapper';
import { parseNotebook, serializeNotebook, notebookCellLanguages, getSimpleLanguage, getNotebookSpecificLanguage, languageToCellKind, backupNotebook } from '../interactiveNotebook';
import { Eol } from '../interfaces';
import { CellOutput, NotebookCell } from '../interfaces/vscode';
import { Diagnostic, DiagnosticSeverity } from './../contracts';
import { getEol, toVsCodeDiagnostic } from './vscodeUtilities';
import { getDiagnosticCollection } from './diagnostics';

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
        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(uri)).toString('utf-8');
        } catch {
        }

        let notebook = parseNotebook(uri, contents);
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

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell): Promise<void> {
        const startTime = Date.now();
        cell.metadata.runStartTime = startTime;
        cell.metadata.runState = vscode.NotebookCellRunState.Running;
        cell.outputs = [];
        let client = await this.clientMapper.getOrAddClient(document.uri);
        let source = cell.document.getText();
        function outputObserver(outputs: Array<CellOutput>) {
            // to properly trigger the UI update, `cell.outputs` needs to be uniquely assigned; simply setting it to the local variable has no effect
            cell.outputs = [];
            cell.outputs = outputs;
        }

        let diagnosticCollection = getDiagnosticCollection(cell.uri);
        function diagnosticObserver(diags: Array<Diagnostic>) {
            diagnosticCollection.set(cell.uri, diags.filter(d => d.severity !== DiagnosticSeverity.Hidden).map(toVsCodeDiagnostic));
        }
        return client.execute(source, getSimpleLanguage(cell.language), outputObserver, diagnosticObserver).then(() => {
            cell.metadata.runState = vscode.NotebookCellRunState.Success;
            cell.metadata.lastRunDuration = Date.now() - startTime;
        }).catch(() => {
            cell.metadata.runState = vscode.NotebookCellRunState.Error;
            cell.metadata.lastRunDuration = Date.now() - startTime;
        });
    }

    cancelCellExecution(document: vscode.NotebookDocument, cell: vscode.NotebookCell) {
        // not supported
    }

    backupNotebook(document: vscode.NotebookDocument, context: vscode.NotebookDocumentBackupContext, cancellation: vscode.CancellationToken): Promise<vscode.NotebookDocumentBackup> {
        return backupNotebook(document, context.destination.fsPath, this.eol);
    }

    private async save(document: vscode.NotebookDocument, targetResource: vscode.Uri): Promise<void> {
        const contents = serializeNotebook(targetResource, document, this.eol);
        const buffer = Buffer.from(contents);
        await vscode.workspace.fs.writeFile(targetResource, buffer);
    }
}

function toNotebookCellData(cell: NotebookCell): vscode.NotebookCellData {
    return {
        cellKind: languageToCellKind(cell.language),
        source: cell.document.getText(),
        language: getNotebookSpecificLanguage(cell.language),
        outputs: [],
        metadata: {}
    };
}

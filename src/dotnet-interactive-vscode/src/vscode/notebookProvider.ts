// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './../clientMapper';
import { parseNotebook, serializeNotebook, notebookCellLanguages, getSimpleLanguage, getNotebookSpecificLanguage, languageToCellKind, backupNotebook, asNotebookFile } from '../interactiveNotebook';
import { RawNotebookCell } from '../interfaces';
import { CellOutput, ReportChannel } from '../interfaces/vscode';
import { Diagnostic, DiagnosticSeverity } from './../contracts';
import { convertToRange } from './vscodeUtilities';

export class DotNetInteractiveNotebookContentProvider implements vscode.NotebookContentProvider, vscode.NotebookKernel {
    private diagnosticCollectionMap: Map<vscode.Uri, vscode.DiagnosticCollection> = new Map();
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
        function outputObserver(outputs: Array<CellOutput>) {
            // to properly trigger the UI update, `cell.outputs` needs to be uniquely assigned; simply setting it to the local variable has no effect
            cell.outputs = [];
            cell.outputs = outputs;
        }

        let diagnosticCollection = this.getDiagnosticCollection(cell.uri);
        function diagnosticObserver(diags: Array<Diagnostic>) {
            diagnosticCollection.set(cell.uri, diags.filter(d => d.severity != DiagnosticSeverity.Hidden).map(toVsCodeDiagnostic));
        }
        return client.execute(source, getSimpleLanguage(cell.language), outputObserver, diagnosticObserver).then(() => {
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

    private getDiagnosticCollection(cellUri: vscode.Uri): vscode.DiagnosticCollection {
        let collection = this.diagnosticCollectionMap.get(cellUri);
        if (!collection) {
            collection = vscode.languages.createDiagnosticCollection();
            this.diagnosticCollectionMap.set(cellUri, collection);
        }

        collection.clear();
        return collection;
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

function toVsCodeDiagnostic(diagnostic: Diagnostic): vscode.Diagnostic {
    return {
        range: convertToRange(diagnostic.linePositionSpan)!,
        message: diagnostic.message,
        severity: toDiagnosticSeverity(diagnostic.severity)
    };
}

function toDiagnosticSeverity(severity: DiagnosticSeverity): vscode.DiagnosticSeverity {
    switch (severity) {
        case DiagnosticSeverity.Error:
            return vscode.DiagnosticSeverity.Error;
        case DiagnosticSeverity.Info:
            return vscode.DiagnosticSeverity.Information;
        case DiagnosticSeverity.Warning:
            return vscode.DiagnosticSeverity.Warning;
        default:
            return vscode.DiagnosticSeverity.Error;
    }
}

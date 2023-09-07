// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as os from 'os';
import * as vscode from 'vscode';
import { Eol, WindowsEol, NonWindowsEol } from "./interfaces";
import { Diagnostic, DiagnosticSeverity, LinePosition, LinePositionSpan, DisplayElement, ErrorElement, InteractiveDocumentOutputElement, InteractiveDocument, InteractiveDocumentElement } from './polyglot-notebooks/contracts';

import * as metadataUtilities from './metadataUtilities';
import * as vscodeLike from './interfaces/vscode-like';
import * as constants from './constants';
import * as vscodeNotebookManagement from './vscodeNotebookManagement';

export function isInsidersBuild(): boolean {
    return vscode.version.indexOf('-insider') >= 0;
}

export function isStableBuild(): boolean {
    return !isInsidersBuild();
}

function convertToPosition(linePosition: LinePosition): vscode.Position {
    return new vscode.Position(linePosition.line, linePosition.character);
}

export function convertToRange(linePositionSpan?: LinePositionSpan): (vscode.Range | undefined) {
    if (linePositionSpan === undefined) {
        return undefined;
    }

    return new vscode.Range(
        convertToPosition(linePositionSpan.start),
        convertToPosition(linePositionSpan.end)
    );
}

export function toVsCodeDiagnostic(diagnostic: Diagnostic): vscode.Diagnostic {
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

export function getEol(): Eol {
    const fileConfig = vscode.workspace.getConfiguration('files');
    const eol = fileConfig.get<string>('eol');
    switch (eol) {
        case NonWindowsEol:
            return NonWindowsEol;
        case WindowsEol:
            return WindowsEol;
        default:
            // could be `undefined` or 'auto'
            if (os.platform() === 'win32') {
                return WindowsEol;
            } else {
                return NonWindowsEol;
            }
    }
}

export function toNotebookDocument(document: vscode.NotebookDocument): InteractiveDocument {
    return {
        elements: document.getCells().map(toInteractiveDocumentElement),
        metadata: document.metadata
    };
}

export function getCellKernelName(cell: vscode.NotebookCell): string {
    const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
    return cellMetadata.kernelName ?? 'csharp';
}

export async function setCellKernelName(cell: vscode.NotebookCell, kernelName: string): Promise<void> {
    const cellMetadata: metadataUtilities.NotebookCellMetadata = {
        kernelName
    };
    const rawCellMetadata = metadataUtilities.getRawNotebookCellMetadataFromNotebookCellMetadata(cellMetadata);
    const mergedMetadata = metadataUtilities.mergeRawMetadata(cell.metadata, rawCellMetadata);
    await vscodeNotebookManagement.replaceNotebookCellMetadata(cell.notebook.uri, cell.index, mergedMetadata);
}

export async function ensureCellKernelKind(cell: vscode.NotebookCell, kind: vscode.NotebookCellKind): Promise<vscode.NotebookCell> {
    if (cell.kind === kind) {
        return cell;
    }

    const newCellData: vscode.NotebookCellData = {
        kind: kind,
        languageId: kind === vscode.NotebookCellKind.Markup ? 'markdown' : constants.CellLanguageIdentifier,
        value: cell.document.getText(),
        metadata: cell.metadata,
    };
    const cellIndex = cell.index; // this gets reset to -1 when the cell is replaced so we have to capture it here
    await vscodeNotebookManagement.replaceNotebookCells(cell.notebook.uri, new vscode.NotebookRange(cellIndex, cellIndex + 1), [newCellData]);
    const cells = cell.notebook.getCells();
    return cells[cellIndex];
}

export async function ensureCellLanguage(cell: vscode.NotebookCell): Promise<void> {
    if (cell.kind === vscode.NotebookCellKind.Code) {
        if (cell.document.languageId !== constants.CellLanguageIdentifier) {
            const updatedCellData = new vscode.NotebookCellData(
                vscode.NotebookCellKind.Code,
                cell.document.getText(),
                constants.CellLanguageIdentifier
            );
            updatedCellData.metadata = cell.metadata;

            await vscodeNotebookManagement.replaceNotebookCells(cell.notebook.uri, new vscode.NotebookRange(cell.index, cell.index + 1), [updatedCellData]);

        }
    }
}

export function toInteractiveDocumentElement(cell: vscode.NotebookCell): InteractiveDocumentElement {
    const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
    return {
        executionOrder: cell.executionSummary?.executionOrder ?? 0,
        kernelName: cell.kind === vscode.NotebookCellKind.Code
            ? cellMetadata.kernelName ?? 'csharp'
            : 'markdown',
        contents: cell.document.getText(),
        outputs: cell.outputs.map(vsCodeCellOutputToContractCellOutput)
    };
}

export function vsCodeCellOutputToContractCellOutput(output: vscode.NotebookCellOutput): InteractiveDocumentOutputElement {
    const outputItems = output.items;
    const errorOutputItems = outputItems.filter(oi => oi.mime === vscodeLike.ErrorOutputMimeType);
    if (errorOutputItems.length > 0) {
        // any error-like output takes precedence
        const errorOutputItem = errorOutputItems[0];
        const error: ErrorElement = {
            errorName: 'Error',
            errorValue: '' + errorOutputItem.data,
            stackTrace: [],
        };
        return error;
    } else {
        //otherwise build the mime=>value dictionary
        const data: { [key: string]: any } = {};
        for (const outputItem of outputItems) {
            data[outputItem.mime] = outputItem.data;
        }

        const cellOutput: DisplayElement = {
            data,
            metadata: {}
        };

        return cellOutput;
    }
}

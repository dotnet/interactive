// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as os from 'os';
import * as vscode from 'vscode';
import { Eol, WindowsEol, NonWindowsEol } from "../interfaces";
import { Diagnostic, DiagnosticSeverity, LinePosition, LinePositionSpan, NotebookCell, NotebookCellDisplayOutput, NotebookCellErrorOutput, NotebookCellOutput, NotebookDocument } from '../interfaces/contracts';

import * as versionSpecificFunctions from '../../versionSpecificFunctions';
import { getSimpleLanguage } from '../interactiveNotebook';
import * as vscodeLike from '../interfaces/vscode-like';

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

export function isUnsavedNotebook(uri: vscode.Uri): boolean {
    return uri.scheme === 'untitled';
}
export function toNotebookDocument(document: vscode.NotebookDocument): NotebookDocument {
    return {
        cells: versionSpecificFunctions.getCells(document).map(toNotebookCell)
    };
}

export function toNotebookCell(cell: vscode.NotebookCell): NotebookCell {
    return {
        language: cell.kind === vscode.NotebookCellKind.Code
            ? getSimpleLanguage(cell.document.languageId)
            : 'markdown',
        contents: cell.document.getText(),
        outputs: cell.outputs.map(vsCodeCellOutputToContractCellOutput)
    };
}

export function vsCodeCellOutputToContractCellOutput(output: vscode.NotebookCellOutput): NotebookCellOutput {
    const errorOutputItems = output.outputs.filter(oi => oi.mime === vscodeLike.ErrorOutputMimeType || oi.metadata?.mimeType === vscodeLike.ErrorOutputMimeType);
    if (errorOutputItems.length > 0) {
        // any error-like output takes precedence
        const errorOutputItem = errorOutputItems[0];
        const error: NotebookCellErrorOutput = {
            errorName: 'Error',
            errorValue: '' + errorOutputItem.value,
            stackTrace: [],
        };
        return error;
    } else {
        //otherwise build the mime=>value dictionary
        const data: { [key: string]: any } = {};
        for (const outputItem of output.outputs) {
            data[outputItem.mime] = outputItem.value;
        }

        const cellOutput: NotebookCellDisplayOutput = {
            data,
        };

        return cellOutput;
    }
}

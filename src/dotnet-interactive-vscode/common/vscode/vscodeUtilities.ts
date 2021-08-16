// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as os from 'os';
import * as vscode from 'vscode';
import { Eol, WindowsEol, NonWindowsEol } from "../interfaces";
import { Diagnostic, DiagnosticSeverity, LinePosition, LinePositionSpan, DisplayElement, ErrorElement, InteractiveDocumentOutputElement, InteractiveDocument, InteractiveDocumentElement } from '../interfaces/contracts';

import { getSimpleLanguage } from '../interactiveNotebook';
import * as vscodeLike from '../interfaces/vscode-like';
import * as versionSpecificFunctions from '../../versionSpecificFunctions';

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
        elements: document.getCells().map(toInteractiveDocumentElement)
    };
}

export function toInteractiveDocumentElement(cell: vscode.NotebookCell): InteractiveDocumentElement {
    return {
        language: cell.kind === vscode.NotebookCellKind.Code
            ? getSimpleLanguage(cell.document.languageId)
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
        };

        return cellOutput;
    }
}

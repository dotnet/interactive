// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as os from 'os';
import * as vscode from 'vscode';
import { Eol, WindowsEol, NonWindowsEol } from "../interfaces";
import { Diagnostic, DiagnosticSeverity, LinePosition, LinePositionSpan } from "../contracts";

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

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// set to match vscode notebook types

export enum CellKind {
    Markdown = 1,
    Code = 2
}

export const ErrorOutputMimeType = 'application/x.notebook.error-traceback';

export interface NotebookCellOutputItem {
    readonly mime: string;
    readonly value: unknown;
    readonly metadata?: Record<string, string | number | boolean | unknown>;
}

export interface NotebookCellOutput {
    readonly id: string;
    readonly outputs: NotebookCellOutputItem[];
}

export interface Uri {
    fsPath: string;
    toString: () => string;
}

export interface Document {
    uri: Uri;
    getText: { (): string };
}

export interface NotebookCell {
    cellKind: CellKind;
    document: Document;
    readonly language: string;
    //outputs: CellOutput[];
}

export interface NotebookDocument {
    readonly cells: ReadonlyArray<NotebookCell>;
}

export interface NotebookDocumentBackup {
    readonly id: string;
    delete(): void;
}

export interface ReportChannel {
    getName(): string;
    append(value: string): void;
    appendLine(value: string): void;
    clear(): void;
    show(): void;
    hide(): void;
}

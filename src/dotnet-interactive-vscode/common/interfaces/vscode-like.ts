// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// set to match vscode notebook types

export enum NotebookCellKind {
    Markup = 1,
    Code = 2
}

export const ErrorOutputMimeType = 'application/vnd.code.notebook.error';

export interface NotebookCellOutputItem {
    readonly mime: string;
    readonly data: Uint8Array;
    readonly metadata?: { [key: string]: any };
    [key: string]: any; // this is to make compilation on stable happy
}

export interface NotebookCellOutput {
    id: string;
    outputs: NotebookCellOutputItem[];
    metadata?: { [key: string]: any };
}

export enum NotebookCellRunState {
    Running = 1,
    Idle = 2,
    Success = 3,
    Error = 4
}

export interface NotebookCellMetadata {
    editable?: boolean,
    breakpointMargin?: boolean,
    runnable?: boolean,
    hasExecutionOrder?: boolean,
    executionOrder?: number,
    runState?: NotebookCellRunState,
    runStartTime?: number,
    statusMessage?: string,
    lastRunDuration?: number,
    inputCollapsed?: boolean,
    outputCollapsed?: boolean,
    custom?: Record<string, any>,
}

export interface Uri {
    fsPath: string;
    scheme: string;
    toString: () => string;
}

export interface Document {
    uri: Uri;
    getText: { (): string };
}

export interface NotebookCell {
    cellKind: NotebookCellKind;
    document: Document;
    readonly language: string;
    //outputs: CellOutput[];
}

export interface NotebookDocument {
    readonly cells: ReadonlyArray<NotebookCell>;
}

export interface NotebookCellData {
    cellKind: NotebookCellKind;
    source: string;
    language: string;
    outputs: NotebookCellOutput[];
    metadata?: NotebookCellMetadata;
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

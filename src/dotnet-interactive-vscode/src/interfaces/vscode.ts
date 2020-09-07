// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// set to match vscode notebook types

export enum CellKind {
    Markdown = 1,
    Code = 2
}

export enum CellOutputKind {
    Text = 1,
    Error = 2,
    Rich = 3
}

export interface CellStreamOutput {
    outputKind: CellOutputKind.Text;
    text: string;
}

export interface CellErrorOutput {
    outputKind: CellOutputKind.Error;
    ename: string;
    evalue: string;
    traceback: string[];
}

export interface CellDisplayOutput {
    outputKind: CellOutputKind.Rich;
    data: { [key: string]: any };
}

export type CellOutput = CellStreamOutput | CellErrorOutput | CellDisplayOutput;

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
    outputs: CellOutput[];
}

export interface NotebookDocument {
    readonly cells: ReadonlyArray<NotebookCell>;
}

export interface NotebookDocumentBackup {
    readonly id: string;
    delete(): void;
}

export interface ReportChannel{
    getName(): string;
    append(value:string) : void;
    appendLine(value:string) : void;
    clear(): void;
    show(): void;
    hide(): void;
}

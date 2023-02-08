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
    stream?: 'stdout' | 'stderr';
    [key: string]: any; // this is to make compilation on stable happy
}

export interface NotebookCellOutput {
    id: string;
    items: NotebookCellOutputItem[];
    metadata?: { [key: string]: any };
}

export enum NotebookCellRunState {
    Running = 1,
    Idle = 2,
    Success = 3,
    Error = 4
}

export interface Uri {
    fsPath: string;
    scheme: string;
    toString: () => string;
}

export interface NotebookCell {
    readonly kind: NotebookCellKind;
    metadata: { [key: string]: any };
}

export interface NotebookDocument {
    readonly uri: Uri;
    readonly metadata: { [key: string]: any };
}

export interface NotebookCellData {
    cellKind: NotebookCellKind;
    source: string;
    language: string;
    outputs: NotebookCellOutput[];
    metadata?: { [key: string]: any };
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

export interface OutputChannel {

    /**
     * The human-readable name of this output channel.
     */
    readonly name: string;

    /**
     * Append the given value to the channel.
     *
     * @param value A string, falsy values will not be printed.
     */
    append(value: string): void;

    /**
     * Append the given value and a line feed character
     * to the channel.
     *
     * @param value A string, falsy values will be printed.
     */
    appendLine(value: string): void;

    /**
     * Removes all output from the channel.
     */
    clear(): void;

    /**
     * Reveal this channel in the UI.
     *
     * @param preserveFocus When `true` the channel will not take focus.
     */
    show(preserveFocus?: boolean): void;

    /**
     * Reveal this channel in the UI.
     *
     * @deprecated Use the overload with just one parameter (`show(preserveFocus?: boolean): void`).
     *
     * @param column This argument is **deprecated** and will be ignored.
     * @param preserveFocus When `true` the channel will not take focus.
     */
    show(column?: ViewColumn, preserveFocus?: boolean): void;

    /**
     * Hide this channel from the UI.
     */
    hide(): void;

    /**
     * Dispose and free associated resources.
     */
    dispose(): void;
}

export enum ViewColumn {
    /**
     * A *symbolic* editor column representing the currently active column. This value
     * can be used when opening editors, but the *resolved* {@link TextEditor.viewColumn viewColumn}-value
     * of editors will always be `One`, `Two`, `Three`,... or `undefined` but never `Active`.
     */
    Active = -1,
    /**
     * A *symbolic* editor column representing the column to the side of the active one. This value
     * can be used when opening editors, but the *resolved* {@link TextEditor.viewColumn viewColumn}-value
     * of editors will always be `One`, `Two`, `Three`,... or `undefined` but never `Beside`.
     */
    Beside = -2,
    /**
     * The first editor column.
     */
    One = 1,
    /**
     * The second editor column.
     */
    Two = 2,
    /**
     * The third editor column.
     */
    Three = 3,
    /**
     * The fourth editor column.
     */
    Four = 4,
    /**
     * The fifth editor column.
     */
    Five = 5,
    /**
     * The sixth editor column.
     */
    Six = 6,
    /**
     * The seventh editor column.
     */
    Seven = 7,
    /**
     * The eighth editor column.
     */
    Eight = 8,
    /**
     * The ninth editor column.
     */
    Nine = 9
}

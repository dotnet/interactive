// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RawNotebookCell } from "./interfaces";
import { trimTrailingCarriageReturn } from './utilities';
import { CellKind } from "./interfaces/vscode";

export const notebookCellLanguages: Array<string> = [
    'dotnet-interactive.csharp',
    'dotnet-interactive.fsharp',
    'dotnet-interactive.html',
    'dotnet-interactive.javascript',
    'dotnet-interactive.markdown',
    'dotnet-interactive.powershell'
];

let languageAliases = new Map<string, string>();
// map common language names to private ones that understand magic commands
languageAliases.set('csharp', 'dotnet-interactive.csharp');
languageAliases.set('fsharp', 'dotnet-interactive.fsharp');
languageAliases.set('html', 'dotnet-interactive.html');
languageAliases.set('javascript', 'dotnet-interactive.javascript');
languageAliases.set('markdown', 'dotnet-interactive.markdown');
languageAliases.set('powershell', 'dotnet-interactive.powershell');
// short aliases
languageAliases.set('cs', 'dotnet-interactive.csharp');
languageAliases.set('fs', 'dotnet-interactive.fsharp');
languageAliases.set('js', 'dotnet-interactive.javascript');
languageAliases.set('md', 'dotnet-interactive.markdown');
languageAliases.set('pwsh', 'dotnet-interactive.powershell');

export const editorLanguageAliases = languageAliases;
const notebookLanguagePrefix = 'dotnet-interactive.';

export function getSimpleLanguage(language: string): string {
    if (language.startsWith(notebookLanguagePrefix)) {
        return language.substr(notebookLanguagePrefix.length);
    }

    return language;
}

export function getNotebookSpecificLanguage(language: string): string {
    if (!language.startsWith(notebookLanguagePrefix)) {
        return notebookLanguagePrefix + language;
    }

    return language;
}

export interface NotebookFile {
    cells: Array<RawNotebookCell>;
}

const languageSpecifier = '#!';

export function parseNotebook(contents: string): NotebookFile {
    let cells: Array<RawNotebookCell> = [];
    let currentLanguage = 'dotnet-interactive.csharp';
    let lines: Array<string> = [];

    function addCell() {
        // trim from the front
        while (lines.length > 0 && lines[0] === '') {
            lines.shift();
        }

        // trim from the back
        while (lines.length > 0 && lines[lines.length - 1] === '') {
            lines.pop();
        }

        if (lines.length > 0) {
            cells.push({
                language: currentLanguage,
                contents: lines
            });
        }
    }

    function addLine(line: string) {
        lines.push(trimTrailingCarriageReturn(line));
    }

    for (let line of contents.split('\n')) {
        if (line.startsWith(languageSpecifier)) {
            let rawLanguage = line.substr(languageSpecifier.length).trim();
            let language = editorLanguageAliases.get(rawLanguage);
            if (language) {
                // recognized language, finalize the current cell
                if (lines.length > 0) {
                    addCell();
                }

                // found a new cell
                currentLanguage = language;
                lines = [];
            } else {
                // unrecognized language, probably a magic command
                addLine(line);
            }
        } else {
            addLine(line);
        }
    }

    if (lines.length > 0) {
        addCell();
    }

    if (cells.length === 0) {
        // ensure there's at least one cell available
        cells.push({
            language: 'dotnet-interactive.csharp',
            contents: []
        });
    }

    return {
        cells
    };
}

export function serializeNotebook(notebook: NotebookFile): string {
    let lines: Array<string> = [];
    for (let cell of notebook.cells) {
        let firstNonBlank = cell.contents.findIndex(line => line.length > 0);
        let lastNonBlank = findIndexReverse(cell.contents, line => line.length > 0);
        if (firstNonBlank >= 0 && lastNonBlank >= 0) {
            lines.push(`#!${getSimpleLanguage(cell.language)}`);
            lines.push('');
            lines.push(...cell.contents.slice(firstNonBlank, lastNonBlank + 1));
            lines.push('');
        }
    }

    return lines.join('\r\n');
}

export function languageToCellKind(language: string): CellKind {
    switch (language) {
        case 'dotnet-interactive.markdown':
            return CellKind.Markdown;
        default:
            return CellKind.Code;
    }
}

function findIndexReverse<T>(arr: Array<T>, predicate: { (val: T): boolean }): number {
    let i = arr.length - 1;
    for (; i >= 0; i--) {
        let item = arr[i];
        if (predicate(item)) {
            break;
        }
    }

    return i;
}

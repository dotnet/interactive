// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CellKind, NotebookCell, NotebookDocument } from "../interfaces/vscode";
import { editorLanguageAliases, getSimpleLanguage } from "../interactiveNotebook";
import { splitAndCleanLines, trimTrailingCarriageReturn } from "../utilities";

const languageSpecifier = '#!';

export function parseAsInteractiveNotebook(contents: string): NotebookDocument {
    let cells: Array<NotebookCell> = [];
    let currentLanguage = 'dotnet-interactive.csharp';
    let lines: Array<string> = [];

    function createCell(language: string, lines: Array<string>): NotebookCell {
        return {
            cellKind: language === 'dotnet-interactive.markdown' ? CellKind.Markdown : CellKind.Code,
            document: {
                uri: {
                    fsPath: 'unused'
                },
                getText: () => lines.join('\n')
            },
            language,
            outputs: []
        };
    }

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
            cells.push(createCell(currentLanguage, lines));
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
        cells.push(createCell('dotnet-interactive.csharp', ['']));
    }

    return {
        cells
    };
}

export function serializeAsInteractiveNotebook(document: NotebookDocument): string {
    let lines: Array<string> = [];
    for (let cell of document.cells) {
        const cellContents = cell.document.getText();
        const cellLines = splitAndCleanLines(cellContents);
        let firstNonBlank = cellLines.findIndex(line => line.length > 0);
        let lastNonBlank = findIndexReverse(cellLines, line => line.length > 0);
        if (firstNonBlank >= 0 && lastNonBlank >= 0) {
            lines.push(`#!${getSimpleLanguage(cell.language)}`);
            lines.push('');
            lines.push(...cellLines.slice(firstNonBlank, lastNonBlank + 1));
            lines.push('');
        }
    }

    return lines.join('\r\n');
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

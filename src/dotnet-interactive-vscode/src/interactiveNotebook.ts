// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as path from 'path';
import { debounce } from './utilities';
import { CellKind, Document, NotebookDocumentBackup } from "./interfaces/vscode";
import { ClientMapper } from './clientMapper';
import { Diagnostic } from './contracts';

export const notebookCellLanguages: Array<string> = [
    'dotnet-interactive.csharp',
    'dotnet-interactive.fsharp',
    'dotnet-interactive.html',
    'dotnet-interactive.javascript',
    'dotnet-interactive.pwsh'
];

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

export function languageToCellKind(language: string): CellKind {
    switch (language) {
        case 'markdown':
            return CellKind.Markdown;
        default:
            return CellKind.Code;
    }
}

export function backupNotebook(rawData: Uint8Array, location: string): Promise<NotebookDocumentBackup> {
    return new Promise<NotebookDocumentBackup>((resolve, reject) => {
        // ensure backup directory exists
        const parsedPath = path.parse(location);
        fs.mkdir(parsedPath.dir, {recursive: true}, async (err, _path) => {
            if (err) {
                reject(err);
                return;
            }

            // save notebook to location
            fs.writeFile(location, rawData, () => {
                resolve({
                    id: location,
                    delete: () => {
                        fs.unlinkSync(location);
                    }
                });
            });
        });
    });
}

export function notebookCellChanged(clientMapper: ClientMapper, document: Document, language: string, diagnosticDelay: number, callback: (diagnostics: Array<Diagnostic>) => void) {
    debounce(document.uri.toString(), diagnosticDelay, async () => {
        const client = await clientMapper.getOrAddClient(document.uri);
        const diagnostics = await client.getDiagnostics(language, document.getText());
        callback(diagnostics);
    });
}

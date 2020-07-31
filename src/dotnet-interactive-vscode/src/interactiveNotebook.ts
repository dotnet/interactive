// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as path from 'path';
import { Eol } from './interfaces';
import { debounce } from './utilities';
import { CellKind, Document, NotebookDocument, NotebookDocumentBackup, Uri } from "./interfaces/vscode";
import { ClientMapper } from './clientMapper';
import { Diagnostic } from './contracts';
import { serializeAsInteractiveNotebook, parseAsInteractiveNotebook } from './fileFormats/interactive';
import { serializeAsJupyterNotebook, parseAsJupyterNotebook } from './fileFormats/jupyter';

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

export function parseNotebook(uri: Uri, contents: string): NotebookDocument {
    const extension = path.extname(uri.fsPath);
    switch (extension.toLowerCase()) {
        case '.ipynb':
            return parseAsJupyterNotebook(contents);
        case '.dib':
        case '.dotnet-interactive':
        default: // unknown
            return parseAsInteractiveNotebook(contents);
    }
}

export function serializeNotebook(uri: Uri, notebook: NotebookDocument, eol: Eol): string {
    switch (path.extname(uri.fsPath).toLowerCase()) {
        case '.ipynb':
            return serializeAsJupyterNotebook(notebook);
        case '.dib':
        case '.dotnet-interactive':
        default: // unknown
            return serializeAsInteractiveNotebook(notebook, eol);
    }
}

export function languageToCellKind(language: string): CellKind {
    switch (language) {
        case 'dotnet-interactive.markdown':
            return CellKind.Markdown;
        default:
            return CellKind.Code;
    }
}

export function backupNotebook(document: NotebookDocument, location: string, eol: Eol): Promise<NotebookDocumentBackup> {
    return new Promise<NotebookDocumentBackup>((resolve, reject) => {
        // ensure backup directory exists
        const parsedPath = path.parse(location);
        fs.mkdir(parsedPath.dir, {recursive: true}, async (err, _path) => {
            if (err) {
                reject(err);
                return;
            }

            // save notebook to location
            const backupFileName = location + '.dib';
            const backupUri = {
                fsPath: backupFileName,
                toString: () => backupFileName
            };
            const backupData = serializeNotebook(backupUri, document, eol);
            fs.writeFile(backupFileName, backupData, () => {
                resolve({
                    id: backupFileName,
                    delete: () => {
                        fs.unlinkSync(backupFileName);
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

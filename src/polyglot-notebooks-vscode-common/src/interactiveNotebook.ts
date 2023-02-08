// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as path from 'path';
import { debounce } from './utilities';
import { NotebookCellKind, NotebookDocumentBackup } from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { Diagnostic } from './polyglot-notebooks/contracts';
import { Uri } from 'vscode';
import * as constants from './constants';

export function isJupyterNotebookViewType(viewType: string): boolean {
    return viewType === constants.JupyterViewType;
}

export function languageToCellKind(language?: string): NotebookCellKind {
    switch (language) {
        case 'markdown':
            return NotebookCellKind.Markup;
        default:
            return NotebookCellKind.Code;
    }
}

export function backupNotebook(rawData: Uint8Array, location: string): Promise<NotebookDocumentBackup> {
    return new Promise<NotebookDocumentBackup>((resolve, reject) => {
        // ensure backup directory exists
        const parsedPath = path.parse(location);
        fs.mkdir(parsedPath.dir, { recursive: true }, async (err, _path) => {
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

export function notebookCellChanged(clientMapper: ClientMapper, documentUri: Uri, documentText: string, kernelName: string, diagnosticDelay: number, callback: (diagnostics: Array<Diagnostic>) => void) {
    debounce(`diagnostics-${documentUri.toString()}`, diagnosticDelay, async () => {
        let diagnostics: Diagnostic[] = [];
        try {
            const client = await clientMapper.getOrAddClient(documentUri);
            diagnostics = await client.getDiagnostics(kernelName, documentText);
        } finally {
            callback(diagnostics);
        }
    });
}

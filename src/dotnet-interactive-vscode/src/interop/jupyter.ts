import { CellKind, CellOutput, CellOutputKind, NotebookDocument } from "../interfaces/vscode";
import { JupyterCell, JupyterMetadata, JupyterNotebook, JupyterOutput } from "../interfaces/jupyter";
import { NotebookFile } from "../interactiveNotebook";
import { RawNotebookCell } from "../interfaces";
import { trimTrailingCarriageReturn } from '../utilities';

export function convertToJupyter(document: NotebookDocument): JupyterNotebook {
    let cells: Array<JupyterCell> = [];
    for (let cell of document.cells) {
        let jcell: JupyterCell | undefined = undefined;
        switch (cell.cellKind) {
            case CellKind.Markdown:
                jcell = {
                    cell_type: 'markdown',
                    metadata: {},
                    source: cell.source
                };
                break;
            case CellKind.Code:
                jcell = {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        `#!${cell.language}\r\n`,
                        ...splitAndEnsureNewlineTerminators(cell.source)
                    ],
                    outputs: cell.outputs.map(convertCellOutputToJupyter).filter(notUndefined)
                };
                break;
            default:
                break;
        }

        if (jcell) {
            cells.push(jcell);
        }
    }

    // VS Code Notebooks don't have the concept of a global notebook language, so we have to fake it.
    let notebookLanguage = 'csharp';
    let metadata: JupyterMetadata = {
        kernelspec: {
            display_name: displayNameFromLanguage(notebookLanguage),
            language: shortLanguageName(notebookLanguage),
            name: `.net-${notebookLanguage}`
        },
        language_info: {
            file_extension: fileExtensionFromLanguage(notebookLanguage),
            mimetype: `text/x-${notebookLanguage}`,
            name: shortLanguageName(notebookLanguage),
            pygments_lexer: notebookLanguage,
            version: versionFromLanguage(notebookLanguage)
        }
    };
    let notebook: JupyterNotebook = {
        cells: cells,
        metadata: metadata,
        nbformat: 4,
        nbformat_minor: 4
    };
    return notebook;
}

export function convertFromJupyter(jupyter: JupyterNotebook): NotebookFile {
    let cells: Array<RawNotebookCell> = [];
    for (let jcell of jupyter.cells) {
        switch (jcell.cell_type) {
            case 'code':
                cells.push({
                    language: getCellLanguage(jcell.source, expandLanguageName(jupyter.metadata.kernelspec.language)),
                    contents: ensureNoNewlineTerminators(jcell.source)
                });
                break;
            case 'markdown':
                cells.push({
                    language: 'markdown',
                    contents: splitAndCleanLines(jcell.source)
                });
                break;
        }
    }
    return {
        cells
    };
}

function getCellLanguage(contents: Array<string>, defaultLanguage: string): string {
    if (contents.length > 0 && contents[0].startsWith('#!')) {
        return contents[0].substr(2).trimRight();
    }

    return defaultLanguage;
}

function expandLanguageName(languageName: string): string {
    switch (languageName) {
        case 'C#':
            return 'csharp';
        case 'F#':
            return 'fsharp';
        default:
            return languageName;
    }
}

function notUndefined<T>(x: T | undefined): x is T {
    return x !== undefined;
}

function convertCellOutputToJupyter(output: CellOutput): JupyterOutput | undefined {
    switch (output.outputKind) {
        case CellOutputKind.Error:
            return {
                output_type: 'error',
                ename: output.ename,
                evalue: output.evalue,
                traceback: output.traceback
            };
        case CellOutputKind.Rich:
            return {
                output_type: 'execute_result',
                execution_count: 1,
                data: convertCellOutputDataToJupyter(output.data),
                metadata: {}
            };
        case CellOutputKind.Text:
            return {
                output_type: 'display_data',
                data: {
                    'text/plain': splitAndEnsureNewlineTerminators(output.text)
                },
                metadata: {}
            };
        default:
            return undefined;
    }
}

function convertCellOutputDataToJupyter(data: { [key: string]: string }): { [key: string]: string[] } {
    let result: { [key: string]: string[] } = {};
    for (let key in data) {
        result[key] = splitAndEnsureNewlineTerminators(data[key]);
    }

    return result;
}

function displayNameFromLanguage(language: string): string {
    switch (language) {
        case 'csharp':
            return '.NET (C#)';
        case 'fsharp':
            return '.NET (F#)';
        case 'powershell':
            return '.NET (PowerShell)';
        default:
            return 'unknown';
    }
}

function fileExtensionFromLanguage(language: string): string {
    switch (language) {
        case 'csharp':
            return '.cs';
        case 'fsharp':
            return '.fs';
        case 'powershell':
            return '.ps1';
        default:
            return 'unknown';
    }
}

function shortLanguageName(language: string): string {
    switch (language) {
        case 'csharp':
            return 'C#';
        case 'fsharp':
            return 'F#';
        default:
            return language;
    }
}

function versionFromLanguage(language: string): string {
    switch (language) {
        case 'csharp':
            return '8.0';
        case 'fsharp':
            return '4.5';
        case 'powershell':
            return '7.0';
        default:
            return 'unknown';
    }
}

function splitAndCleanLines(source: string): Array<string> {
    let lines = source.split('\n').map(trimTrailingCarriageReturn);
    return lines;
}

function splitAndEnsureNewlineTerminators(source: string): Array<string> {
    // With the exception of markdown text, jupyter stores strings in an array, one entry per line, where each line has
    // a terminating `\r\n` except for the last line.
    let lines = splitAndCleanLines(source);
    for (let i = 0; i < lines.length - 1; i++) {
        lines[i] += '\r\n';
    }

    return lines;
}

function ensureNoNewlineTerminators(lines: Array<string>): Array<string> {
    let result = [];
    for (let line of lines) {
        if (line.endsWith('\n')) {
            line = line.substr(0, line.length - 1);
        }
        if (line.endsWith('\r')) {
            line = line.substr(0, line.length - 1);
        }

        result.push(line);
    }

    // a language-specific cell was handled elsewhere
    if (result.length > 0 && result[0].startsWith('#!'))
    {
        result.shift();
    }

    return result;
}

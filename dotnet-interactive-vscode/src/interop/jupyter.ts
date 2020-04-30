import { CellKind, CellOutput, CellOutputKind, NotebookDocument } from "../interfaces/vscode";
import { JupyterCell, JupyterMetadata, JupyterNotebook, JupyterOutput } from "../interfaces/jupyter";

export function convertToJupyter(document: NotebookDocument): JupyterNotebook {
    let cells: Array<JupyterCell> = [];
    for (let cell of document.cells) {
        let jcell: JupyterCell | undefined = undefined;
        switch (cell.cellKind) {
            case CellKind.Markdown:
                jcell = {
                    cell_type: 'markdown',
                    metadata: {},
                    source: cell.source.split('\n')
                };
                break;
            case CellKind.Code:
                jcell = {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: cell.source.split('\n'),
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

    // these values are essentially hard-coded regardless of notebook content
    let notebookLanguage = document.languages.length > 0
        ? document.languages[0]
        : 'csharp';
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
                    'text/plain': output.text.split('\n')
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
        result[key] = data[key].split('\n');
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

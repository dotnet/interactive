import { RawNotebookCell } from "./interfaces";

export const editorLanguages = ['csharp', 'fsharp', 'html', 'javascript', 'markdown', 'powershell'];

export interface NotebookFile {
    cells: Array<RawNotebookCell>;
}

const languageSpecifier = '#!';

export function parseNotebook(contents: string): NotebookFile {
    // build a map of aliases to full language names
    let languageMap: { [key: string]: string } = {};
    for (let supportedLanguage of editorLanguages) {
        // the officially supported languages map to themselves
        languageMap[supportedLanguage] = supportedLanguage;
    }

    // short langauge names
    languageMap['cs'] = 'csharp';
    languageMap['fs'] = 'fsharp';
    languageMap['js'] = 'javascript';
    languageMap['md'] = 'markdown';
    languageMap['pwsh'] = 'powershell';

    let cells: Array<RawNotebookCell> = [];
    let currentLanguage = 'csharp';
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
        while (line.endsWith('\r')) {
            line = line.substr(0, line.length - 1);
        }

        lines.push(line);
    }

    for (let line of contents.split('\n')) {
        if (line.startsWith(languageSpecifier)) {
            let rawLanguage = line.substr(languageSpecifier.length).trimRight();
            let language = languageMap[rawLanguage] || rawLanguage;
            if (language) {
                // finalize the current cell
                if (lines.length > 0) {
                    addCell();
                }
                // found a new cell
                currentLanguage = language;
                lines = [];
            } else {
                addLine(line);
            }
        } else {
            addLine(line);
        }
    }

    if (lines.length > 0) {
        addCell();
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
            lines.push(`#!${cell.language}`);
            lines.push('');
            lines.push(...cell.contents.slice(firstNonBlank, lastNonBlank + 1));
            lines.push('');
        }
    }

    return lines.join('\r\n');
}

function findIndexReverse<T>(arr: Array<T>, predicate: {(val: T): boolean}): number {
    let i = arr.length - 1;
    for (; i >= 0; i--) {
        let item = arr[i];
        if (predicate(item)) {
            break;
        }
    }

    return i;
}

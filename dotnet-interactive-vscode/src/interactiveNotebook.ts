// begin interface matching from vscode

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

// end of interface matching

export interface RawNotebookCell {
    kind: CellKind;
    language: string;
    content: string;
    outputs: Array<CellOutput>;
}

export interface NotebookFile {
    targetKernelName: string;
    cells: Array<RawNotebookCell>;
}

export class InteractiveNotebook {
    static parse(contents: string): NotebookFile {
        let notebook: NotebookFile;
        try {
            notebook = <NotebookFile>JSON.parse(contents);
        } catch {
            notebook = {
                targetKernelName: 'csharp',
                cells: [],
            };
        }

        return notebook;
    }
}

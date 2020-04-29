import { InteractiveClient } from "./interactiveClient";
import { CommandFailed, StandardOutputValueProduced, ReturnValueProduced } from "./events";

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
    static async execute(source: string, client: InteractiveClient): Promise<Array<CellOutput>> {
        return new Promise((resolve, reject) => {
            client.submitCode(source).subscribe({
                next: value => {
                    switch (value.eventType) {
                        case 'CommandFailed':
                            {
                                let err = <CommandFailed>value.event;
                                let output: CellErrorOutput = {
                                    outputKind: CellOutputKind.Error,
                                    ename: 'Error',
                                    evalue: err.message,
                                    traceback: [],
                                };
                                resolve([output]);
                            }
                            break;
                        case 'StandardOutputValueProduced':
                            {
                                let st = <StandardOutputValueProduced>value.event;
                                let output: CellStreamOutput = {
                                    outputKind: CellOutputKind.Text,
                                    text: st.value.toString(),
                                };
                                resolve([output]);
                            }
                            break;
                        case 'ReturnValueProduced':
                            {
                                let rvt = <ReturnValueProduced>value.event;
                                let data: { [key: string]: any } = {};
                                for (let formatted of rvt.formattedValues) {
                                    data[formatted.mimeType] = formatted.value;
                                }
                                let output: CellDisplayOutput = {
                                    outputKind: CellOutputKind.Rich,
                                    data: data
                                };
                                resolve([output]);
                            }
                            break;
                    }
                },
                error: err => {
                    reject({
                        err: err
                    });
                },
                complete: () => {}
            });
        });
    }

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

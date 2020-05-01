import { InteractiveClient } from "./interactiveClient";
import { CommandFailed, StandardOutputValueProduced, ReturnValueProduced } from "./events";
import { RawNotebookCell } from "./interfaces";
import { CellDisplayOutput, CellErrorOutput, CellKind, CellOutput, CellOutputKind, CellStreamOutput, NotebookDocument } from "./interfaces/vscode";

export interface NotebookFile {
    cells: Array<RawNotebookCell>;
}

export async function execute(language: string, source: string, client: InteractiveClient): Promise<Array<CellOutput>> {
    return new Promise((resolve, reject) => {
        client.submitCode(language, source).subscribe({
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
            complete: () => {
                resolve([]);
            }
        });
    });
}

export function parseNotebook(contents: string): NotebookFile {
    let notebook: NotebookFile;
    try {
        notebook = <NotebookFile>JSON.parse(contents);
    } catch {
        notebook = {
            cells: [],
        };
    }

    return notebook;
}

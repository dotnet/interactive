import * as vscode from 'vscode';
import { CommandFailed, ReturnValueProduced, StandardOutputValueProduced, EventEnvelope } from '../events';
import { ClientMapper } from '../clientMapper';

export interface NotebookFile {
    targetKernelName: string;
    cells: Array<RawNotebookCell>;
}

export interface RawNotebookCell {
    kind: vscode.CellKind;
    language: string;
    content: string;
    outputs: Array<vscode.CellOutput>;
}

export class DotNetInteractiveNotebookProvider implements vscode.NotebookProvider {
    constructor(readonly clientMapper: ClientMapper) {
    }

    async resolveNotebook(editor: vscode.NotebookEditor): Promise<void> {
        editor.document.languages = ['dotnet-interactive'];

        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(editor.document.uri)).toString('utf-8');
        } catch {
        }

        let notebook = DotNetInteractiveNotebookProvider.parseNotebook(contents);
        this.clientMapper.addClient(notebook.targetKernelName, editor.document.uri);
        editor.edit(editBuilder => {
            for (let cell of notebook.cells) {
                editBuilder.insert(
                    0,
                    cell.content,
                    cell.language,
                    cell.kind,
                    cell.outputs,
                    {
                        editable: true,
                        runnable: true
                    }
                );
            }
        });

        setTimeout(() => {
            for (let cell of editor.document.cells) {
                if (cell.cellKind === vscode.CellKind.Code) {
                    //
                }
            }
        }, 0);
    }

    executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell | undefined, token: vscode.CancellationToken): Promise<void> {
        if (!cell) {
            // TODO: run everything
            return Promise.resolve();
        }

        let client = this.clientMapper.getClient(document.uri);
        if (client === undefined) {
            return Promise.resolve();
        }

        let source = cell.source.toString();
        client.submitCode(source).subscribe({
            next: value => {
                switch (value.eventType) {
                    case 'CommandFailed':
                        {
                            let err = <CommandFailed>value.event;
                            let output: vscode.CellErrorOutput = {
                                outputKind: vscode.CellOutputKind.Error,
                                ename: 'Error',
                                evalue: err.message,
                                traceback: [],
                            };
                            cell.outputs = [output];
                        }
                        break;
                    case 'StandardOutputValueProduced':
                        {
                            let st = <StandardOutputValueProduced>value.event;
                            let output: vscode.CellStreamOutput = {
                                outputKind: vscode.CellOutputKind.Text,
                                text: st.value.toString(),
                            };
                            cell.outputs = [output];
                        }
                        break;
                    case 'ReturnValueProduced':
                        {
                            let rvt = <ReturnValueProduced>value.event;
                            let data: { [key: string]: any } = {};
                            for (let formatted of rvt.formattedValues) {
                                data[formatted.mimeType] = formatted.value;
                            }
                            let output: vscode.CellDisplayOutput = {
                                outputKind: vscode.CellOutputKind.Rich,
                                data: data
                            };
                            cell.outputs = [output];
                        }
                        break;
                }
            },
            error: err => {
                cell.outputs = [{
                    outputKind: vscode.CellOutputKind.Error,
                    ename: 'Error',
                    evalue: `Unknown error: ${err}`,
                    traceback: [],
                }];
            },
            complete: () => {}
        });

        return Promise.resolve();
    }

    async save(document: vscode.NotebookDocument): Promise<boolean> {
        let client = this.clientMapper.getClient(document.uri);
        if (client === undefined) {
            return false;
        }

        let notebook: NotebookFile = {
            targetKernelName: client.targetKernelName,
            cells: [],
        };
        for (let cell of document.cells) {
            notebook.cells.push({
                language: cell.language,
                content: cell.document.getText(),
                outputs: cell.outputs,
                kind: cell.cellKind
            });
        }

        await vscode.workspace.fs.writeFile(document.uri, Buffer.from(JSON.stringify(notebook, null, 2)));
        return true;
    }

    static parseNotebook(contents: string): NotebookFile {
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

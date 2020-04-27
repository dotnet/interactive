import * as vscode from 'vscode';
import { CommandFailed, ReturnValueProduced, StandardOutputValueProduced } from './interfaces';
import { ClientMapper } from './clientMapper';

interface NotebookFile {
    targetKernelName: string;
    cells: Array<RawNotebookCell>;
}

interface RawNotebookCell {
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

        let notebook: NotebookFile;
        try {
            notebook = <NotebookFile>JSON.parse(contents);
        } catch {
            notebook = {
                targetKernelName: 'csharp',
                cells: [],
            };
        }

        let _client = this.clientMapper.addClient(notebook.targetKernelName, editor.document.uri);

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
        return client.submitCode(source, (event, eventType) => {
            switch (eventType) {
                case 'CommandFailed':
                    {
                        let err = <CommandFailed>event;
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
                        let st = <StandardOutputValueProduced>event;
                        let output: vscode.CellStreamOutput = {
                            outputKind: vscode.CellOutputKind.Text,
                            text: st.value.toString(),
                        };
                        cell.outputs = [output];
                    }
                    break;
                case 'ReturnValueProduced':
                    {
                        let rvt = <ReturnValueProduced>event;
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
        });
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
}

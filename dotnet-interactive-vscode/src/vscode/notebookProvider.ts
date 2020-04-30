import * as vscode from 'vscode';
import { ClientMapper } from './../clientMapper';
import { NotebookFile, execute, parseNotebook } from '../interactiveNotebook';

export class DotNetInteractiveNotebookProvider implements vscode.NotebookProvider {
    constructor(readonly clientMapper: ClientMapper) {
    }

    async resolveNotebook(editor: vscode.NotebookEditor): Promise<void> {
        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(editor.document.uri)).toString('utf-8');
        } catch {
        }

        let notebook = parseNotebook(contents);
        editor.document.languages = [notebook.targetKernelName];
        this.clientMapper.addClient(notebook.targetKernelName, editor.document.uri);
        editor.edit(editBuilder => {
            for (let cell of notebook.cells) {
                editBuilder.insert(
                    0,
                    cell.content,
                    notebook.targetKernelName,
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

    async executeCell(document: vscode.NotebookDocument, cell: vscode.NotebookCell | undefined, token: vscode.CancellationToken): Promise<void> {
        if (!cell) {
            // TODO: run everything
            return;
        }

        let client = this.clientMapper.getClient(document.uri);
        if (client === undefined) {
            return;
        }

        let source = cell.source.toString();
        let outputs = await execute(source, client);
        cell.outputs = outputs;
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
                content: cell.document.getText(),
                outputs: cell.outputs,
                kind: cell.cellKind
            });
        }

        await vscode.workspace.fs.writeFile(document.uri, Buffer.from(JSON.stringify(notebook, null, 2)));
        return true;
    }
}

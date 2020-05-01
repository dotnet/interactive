import * as vscode from 'vscode';
import { ClientMapper } from './../clientMapper';
import { NotebookFile, execute, parseNotebook, serializeNotebook, editorLanguages } from '../interactiveNotebook';

export class DotNetInteractiveNotebookProvider implements vscode.NotebookProvider {
    constructor(readonly clientMapper: ClientMapper) {
    }

    async resolveNotebook(editor: vscode.NotebookEditor): Promise<void> {
        editor.document.languages = editorLanguages;
        let contents = '';
        try {
            contents = Buffer.from(await vscode.workspace.fs.readFile(editor.document.uri)).toString('utf-8');
        } catch {
        }

        let notebook = parseNotebook(contents);
        this.clientMapper.getOrAddClient(editor.document.uri);
        editor.edit(editBuilder => {
            for (let cell of notebook.cells) {
                editBuilder.insert(
                    0,
                    cell.contents.join('\n'),
                    cell.language,
                    languageToCellKind(cell.language),
                    [], // outputs
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

        let client = this.clientMapper.getOrAddClient(document.uri);
        let source = cell.source.toString();
        let outputs = await execute(cell.language, source, client);
        cell.outputs = outputs;
    }

    async save(document: vscode.NotebookDocument): Promise<boolean> {
        let notebook: NotebookFile = {
            cells: [],
        };
        for (let cell of document.cells) {
            notebook.cells.push({
                language: cell.language,
                contents: cell.document.getText().split('\n'),
            });
        }

        await vscode.workspace.fs.writeFile(document.uri, Buffer.from(serializeNotebook(notebook)));
        return true;
    }
}

function languageToCellKind(language: string): vscode.CellKind {
    switch (language) {
        case 'md':
        case 'markdown':
            return vscode.CellKind.Markdown;
        default:
            return vscode.CellKind.Code;
    }
}

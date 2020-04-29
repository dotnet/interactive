import { expect } from 'chai';

//import * as vscode from 'vscode';
//import { ClientMapper } from '../../clientMapper';
//import { DotNetInteractiveNotebookProvider } from '../../notebookProvider';

/*suite('Extension Test Suite', () => {
    vscode.window.showInformationMessage('Start all tests.');

    test('Parse notebook cell URI', () => {
        expect(ClientMapper.keyFromUri(vscode.Uri.parse("vscode-notebook:/c:/path/to/file"))).to.equal("/c:/path/to/file");
        expect(ClientMapper.keyFromUri(vscode.Uri.parse("vscode-notebook:/c:/path/to/file, cell 1"))).to.equal("/c:/path/to/file");
    });

    test('Parse notebook from valid JSON', () => {
        let valid = {
            targetKernelName: 'fsharp',
            cells: [
                {
                    kind: vscode.CellKind.Code,
                    language: 'dotnet-interactive',
                    content: 'let x = 1',
                    outputs: [],
                }
            ]
        };
        let validJson = JSON.stringify(valid);
        let notebook = DotNetInteractiveNotebookProvider.parseNotebook(validJson);
        expect(notebook.targetKernelName).to.equal("fsharp");
        expect(notebook.cells.length).to.equal(1);
        expect(notebook.cells[0].content).to.equal("let x = 1");
    });

    test('Parse notebook from invalid JSON', () => {
        let notebook = DotNetInteractiveNotebookProvider.parseNotebook('invalid json should still result in a notebook');
        expect(notebook.targetKernelName).to.equal("csharp");
        expect(notebook.cells).length.to.be.empty;
    });
}); // */

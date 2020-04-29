import { expect } from 'chai';

//import * as vscode from 'vscode';
import { ClientMapper } from './../../clientMapper';
//import { DotNetInteractiveNotebookProvider } from '../../notebookProvider';

describe('Extension Test Suite', () => {
    it('Parse notebook cell URI', () => {
        expect(ClientMapper.keyFromUri({path: "/c:/path/to/file"})).to.equal("/c:/path/to/file");
        expect(ClientMapper.keyFromUri({path: "/c:/path/to/file, cell 1"})).to.equal("/c:/path/to/file");
    });

    /*it('Parse notebook from valid JSON', () => {
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

    it('Parse notebook from invalid JSON', () => {
        let notebook = DotNetInteractiveNotebookProvider.parseNotebook('invalid json should still result in a notebook');
        expect(notebook.targetKernelName).to.equal("csharp");
        expect(notebook.cells).length.to.be.empty;
    }); // */
});

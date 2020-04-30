import { expect } from 'chai';

import { ClientMapper } from './../../clientMapper';
import { parseNotebook } from '../../interactiveNotebook';
import { CellKind } from '../../interfaces/vscode';

describe('Extension Test Suite', () => {
    it('Parse notebook cell URI', () => {
        expect(ClientMapper.keyFromUri({path: "/c:/path/to/file"})).to.equal("/c:/path/to/file");
        expect(ClientMapper.keyFromUri({path: "/c:/path/to/file, cell 1"})).to.equal("/c:/path/to/file");
    });

    it('Parse notebook from valid JSON', () => {
        let valid = {
            targetKernelName: 'fsharp',
            cells: [
                {
                    kind: CellKind.Code,
                    language: 'dotnet-interactive',
                    content: 'let x = 1',
                    outputs: [],
                }
            ]
        };
        let validJson = JSON.stringify(valid);
        let notebook = parseNotebook(validJson);
        expect(notebook.targetKernelName).to.equal("fsharp");
        expect(notebook.cells.length).to.equal(1);
        expect(notebook.cells[0].content).to.equal("let x = 1");
    });

    it('Parse notebook from invalid JSON', () => {
        let notebook = parseNotebook('invalid json should still result in a notebook');
        expect(notebook.targetKernelName).to.equal("csharp");
        expect(notebook.cells).length.to.be.empty;
    });
});

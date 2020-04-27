import { expect } from 'chai';

import * as vscode from 'vscode';
import { ClientMapper } from '../../clientMapper';

suite('Extension Test Suite', () => {
    vscode.window.showInformationMessage('Start all tests.');

    test('Parse notebook cell URI', () => {
        expect(ClientMapper.keyFromUri(vscode.Uri.parse("vscode-notebook:/c:/path/to/file"))).to.equal("/c:/path/to/file");
        expect(ClientMapper.keyFromUri(vscode.Uri.parse("vscode-notebook:/c:/path/to/file, cell 1"))).to.equal("/c:/path/to/file");
    });
});

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as constants from './constants';
import * as metadataUtilities from './metadataUtilities';
import { DynamicGrammarSemanticTokenProvider } from './dynamicGrammarSemanticTokenProvider';

export class LanguageConfigurationManager {
    private _disposables: { dispose(): any }[] = [];
    private _lastLanguageConfigurationObject: any | undefined = undefined;

    constructor(private readonly dynamicTokensProvider: DynamicGrammarSemanticTokenProvider) {
        // whenever a new notebok cell is selected...
        this._disposables.push(vscode.window.onDidChangeActiveTextEditor(textEditor => {
            const document = textEditor?.document;
            if (document) {
                this.ensureLanguageConfigurationForDocument(document);
            }
        }));
    }

    ensureLanguageConfigurationForDocument(document: vscode.TextDocument) {
        const cell = vscode.workspace.notebookDocuments.flatMap(notebook => notebook.getCells()).find(cell => cell.document === document);
        if (cell && metadataUtilities.isDotNetNotebook(cell.notebook)) {
            const notebookCellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
            if (notebookCellMetadata.kernelName) {
                const languageConfiguration = this.dynamicTokensProvider.getLanguageConfigurationFromKernelNameOrAlias(cell.notebook, notebookCellMetadata.kernelName);
                if (languageConfiguration !== this._lastLanguageConfigurationObject) {
                    this._lastLanguageConfigurationObject = languageConfiguration;
                    const typedLanguageConfiguration = <vscode.LanguageConfiguration>languageConfiguration;
                    vscode.languages.setLanguageConfiguration(constants.CellLanguageIdentifier, typedLanguageConfiguration);
                }
            }
        }
    }

    dispose() {
        for (const disposable of this._disposables) {
            disposable.dispose();
        }
    }
}

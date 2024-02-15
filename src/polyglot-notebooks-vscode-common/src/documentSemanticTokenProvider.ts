// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as vscode from 'vscode';
import * as metadataUtilities from './metadataUtilities';
import { DynamicGrammarSemanticTokenProvider, VSCodeExtensionLike } from './dynamicGrammarSemanticTokenProvider';
import * as constants from './constants';
import * as vscodeNotebookManagement from './vscodeNotebookManagement';
import { Logger } from './polyglot-notebooks';

// https://code.visualstudio.com/api/language-extensions/semantic-highlight-guide#standard-token-types-and-modifiers
const defaultTokenTypes = [
    'namespace', // For identifiers that declare or reference a namespace, module, or package.
    'class', // For identifiers that declare or reference a class type.
    'enum', // For identifiers that declare or reference an enumeration type.
    'interface', // For identifiers that declare or reference an interface type.
    'struct', // For identifiers that declare or reference a struct type.
    'typeParameter', // For identifiers that declare or reference a type parameter.
    'type', // For identifiers that declare or reference a type that is not covered above.
    'parameter', // For identifiers that declare or reference a function or method parameters.
    'variable', // For identifiers that declare or reference a local or global variable.
    'property', // For identifiers that declare or reference a member property, member field, or member variable.
    'enumMember', // For identifiers that declare an enumeration property, constant, or member.
    'event', // For identifiers that declare an event property.
    'function', // For identifiers that declare a function.
    'method', // For identifiers that declare a member function or method.
    'macro', // For identifiers that declare a macro.
    'label', // For identifiers that declare a label.
    'comment', // For tokens that represent a comment.
    'string', // For tokens that represent a string literal.
    'keyword', // For tokens that represent a language keyword.
    'number', // For tokens that represent a number literal.
    'regexp', // For tokens that represent a regular expression literal.
    'operator', // For tokens that represent an operator.
];

// default semantic token modifiers
const defaultTokenModifiers = [
    'declaration', // For declarations of symbols.
    'definition', // For definitions of symbols, for example, in header files.
    'readonly', // For readonly variables and member fields (constants).
    'static', // For class members (static members).
    'deprecated', // For symbols that should no longer be used.
    'abstract', // For types and member functions that are abstract.
    'async', // For functions that are marked async.
    'modification', // For variable references where the variable is assigned to.
    'documentation', // For occurrences of symbols in documentation.
    'defaultLibrary', // For symbols that are part of the standard library.
];

export const selector: vscode.DocumentSelector = [
    { language: constants.CellLanguageIdentifier },
];

export class DocumentSemanticTokensProvider implements vscode.DocumentSemanticTokensProvider {
    private _dynamicTokenProvider: DynamicGrammarSemanticTokenProvider;
    private _onDidChangeSemanticTokensEmitter: vscode.EventEmitter<void> = new vscode.EventEmitter<void>();
    private _semanticTokensLegend: vscode.SemanticTokensLegend;

    constructor(packageJSON: any) {
        const extensionData = vscode.extensions.all.map(extension => <VSCodeExtensionLike>extension);
        this._dynamicTokenProvider = new DynamicGrammarSemanticTokenProvider(packageJSON, extensionData, path => fs.existsSync(path), path => fs.readFileSync(path, 'utf8'));
        const tokenTypes = [...defaultTokenTypes, ...this._dynamicTokenProvider.semanticTokenTypes];
        this._semanticTokensLegend = new vscode.SemanticTokensLegend(tokenTypes, defaultTokenModifiers);
    }

    get semanticTokensLegend(): vscode.SemanticTokensLegend {
        return this._semanticTokensLegend;
    }

    get dynamicTokenProvider(): DynamicGrammarSemanticTokenProvider {
        return this._dynamicTokenProvider;
    }

    async init(context: vscode.ExtensionContext): Promise<void> {
        await this._dynamicTokenProvider.init();

        context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.refreshSemanticTokens', () => {
            this.refresh();
        }));

        context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.resetNotebookKernelCollection', async (notebook?: vscode.NotebookDocument | undefined) => {
            if (notebook) {
                const isIpynb = metadataUtilities.isIpynbNotebook(notebook);
                const bareMetadata = metadataUtilities.createDefaultNotebookDocumentMetadata();
                const rawBareMetadata = metadataUtilities.getMergedRawNotebookDocumentMetadataFromNotebookDocumentMetadata(bareMetadata, notebook.metadata, isIpynb);
                const _succeeded = await vscodeNotebookManagement.replaceNotebookMetadata(notebook.uri, rawBareMetadata);
                const kernelInfos = metadataUtilities.getKernelInfosFromNotebookDocument(notebook);
                this.dynamicTokenProvider.rebuildNotebookGrammar(notebook.uri, kernelInfos, true);
            }
        }));
    }

    onDidChangeSemanticTokens: vscode.Event<void> = this._onDidChangeSemanticTokensEmitter.event;

    refresh() {
        this._onDidChangeSemanticTokensEmitter.fire();
    }

    async provideDocumentSemanticTokens(document: vscode.TextDocument, _cancellationToken: vscode.CancellationToken): Promise<vscode.SemanticTokens> {
        Logger.default.info(`[documentSemanticTokenProvider] provideDocumentSemanticTokens called for ${document.uri.toString()}`)
        try {
            let tokenCount = 0;
            const tokensBuilder = new vscode.SemanticTokensBuilder(this.semanticTokensLegend);
            const notebookDocument = vscode.workspace.notebookDocuments.find(notebook => notebook.getCells().some(cell => cell.document === document));
            if (notebookDocument) {
                const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(notebookDocument);
                const text = document.getText();
                const cell = notebookDocument.getCells().find(cell => cell.document === document);
                if (cell) {
                    const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
                    const cellKernelName = cellMetadata.kernelName ?? notebookMetadata.kernelInfo.defaultKernelName;
                    const tokens = await this._dynamicTokenProvider.getTokens(notebookDocument.uri, cellKernelName, text);
                    for (const token of tokens) {
                        tokenCount++;
                        tokensBuilder.push(
                            new vscode.Range(
                                new vscode.Position(token.line, token.startColumn),
                                new vscode.Position(token.line, token.endColumn)
                            ),
                            token.tokenType,
                            token.tokenModifiers);
                    }

                    if (tokenCount === 0 && text !== '') {
                        // there was text, but nothing was produced
                        Logger.default.info(`[documentSemanticTokenProvider] No tokens were produced for cell ${cell.index} of notebook ${notebookDocument.uri.toString()} with text: ${text}`);
                    }
                } else {
                    Logger.default.warn(`[documentSemanticTokenProvider] No cell found for document ${document.uri.toString()}`);
                }
            } else {
                Logger.default.warn(`[documentSemanticTokenProvider] No notebook found for document ${document.uri.toString()}`);
            }

            // TODO: agument with real semantic tokens?

            const tokens = tokensBuilder.build();
            return tokens;
        } catch (ex) {
            Logger.default.warn(`[documentSemanticTokenProvider] Error generating dynamic semantic tokens: ${JSON.stringify(ex)}`);
            return new vscode.SemanticTokens(new Uint32Array(0));
        }
    }
}

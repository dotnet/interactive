"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.DocumentSemanticTokensProvider = exports.selector = void 0;
const fs = require("fs");
const vscode = require("vscode");
const metadataUtilities = require("./metadataUtilities");
const dynamicGrammarSemanticTokenProvider_1 = require("./dynamicGrammarSemanticTokenProvider");
const constants = require("./constants");
const versionSpecificFunctions = require("../versionSpecificFunctions");
// https://code.visualstudio.com/api/language-extensions/semantic-highlight-guide#standard-token-types-and-modifiers
const defaultTokenTypes = [
    'namespace',
    'class',
    'enum',
    'interface',
    'struct',
    'typeParameter',
    'type',
    'parameter',
    'variable',
    'property',
    'enumMember',
    'event',
    'function',
    'method',
    'macro',
    'label',
    'comment',
    'string',
    'keyword',
    'number',
    'regexp',
    'operator', // For tokens that represent an operator.
];
// default semantic token modifiers
const defaultTokenModifiers = [
    'declaration',
    'definition',
    'readonly',
    'static',
    'deprecated',
    'abstract',
    'async',
    'modification',
    'documentation',
    'defaultLibrary', // For symbols that are part of the standard library.
];
exports.selector = [
    { language: constants.CellLanguageIdentifier },
];
class DocumentSemanticTokensProvider {
    constructor(packageJSON) {
        this._onDidChangeSemanticTokensEmitter = new vscode.EventEmitter();
        this.onDidChangeSemanticTokens = this._onDidChangeSemanticTokensEmitter.event;
        const extensionData = vscode.extensions.all.map(extension => extension);
        this._dynamicTokenProvider = new dynamicGrammarSemanticTokenProvider_1.DynamicGrammarSemanticTokenProvider(packageJSON, extensionData, path => fs.existsSync(path), path => fs.readFileSync(path, 'utf8'));
        const tokenTypes = [...defaultTokenTypes, ...this._dynamicTokenProvider.semanticTokenTypes];
        this._semanticTokensLegend = new vscode.SemanticTokensLegend(tokenTypes, defaultTokenModifiers);
    }
    get semanticTokensLegend() {
        return this._semanticTokensLegend;
    }
    get dynamicTokenProvider() {
        return this._dynamicTokenProvider;
    }
    init(context) {
        return __awaiter(this, void 0, void 0, function* () {
            yield this._dynamicTokenProvider.init();
            context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.refreshSemanticTokens', () => {
                this.refresh();
            }));
            context.subscriptions.push(vscode.commands.registerCommand('polyglot-notebook.resetNotebookKernelCollection', (notebook) => __awaiter(this, void 0, void 0, function* () {
                if (notebook) {
                    const isIpynb = metadataUtilities.isIpynbNotebook(notebook);
                    const bareMetadata = metadataUtilities.createDefaultNotebookDocumentMetadata();
                    const rawBareMetadata = metadataUtilities.getRawNotebookDocumentMetadataFromNotebookDocumentMetadata(bareMetadata, isIpynb);
                    const _succeeded = yield versionSpecificFunctions.replaceNotebookMetadata(notebook.uri, rawBareMetadata);
                    const kernelInfos = metadataUtilities.getKernelInfosFromNotebookDocument(notebook);
                    this.dynamicTokenProvider.rebuildNotebookGrammar(notebook.uri, kernelInfos, true);
                }
            })));
        });
    }
    refresh() {
        this._onDidChangeSemanticTokensEmitter.fire();
    }
    provideDocumentSemanticTokens(document, _cancellationToken) {
        var _a;
        return __awaiter(this, void 0, void 0, function* () {
            const tokensBuilder = new vscode.SemanticTokensBuilder(this.semanticTokensLegend);
            const notebookDocument = vscode.workspace.notebookDocuments.find(notebook => notebook.getCells().some(cell => cell.document === document));
            if (notebookDocument) {
                const notebookMetadata = metadataUtilities.getNotebookDocumentMetadataFromNotebookDocument(notebookDocument);
                const text = document.getText();
                const cell = notebookDocument.getCells().find(cell => cell.document === document);
                if (cell) {
                    const cellMetadata = metadataUtilities.getNotebookCellMetadataFromNotebookCellElement(cell);
                    const cellKernelName = (_a = cellMetadata.kernelName) !== null && _a !== void 0 ? _a : notebookMetadata.kernelInfo.defaultKernelName;
                    const tokens = yield this._dynamicTokenProvider.getTokens(notebookDocument.uri, cellKernelName, text);
                    for (const token of tokens) {
                        try {
                            tokensBuilder.push(new vscode.Range(new vscode.Position(token.line, token.startColumn), new vscode.Position(token.line, token.endColumn)), token.tokenType, token.tokenModifiers);
                        }
                        catch (e) {
                            const x = e;
                        }
                    }
                }
            }
            // TODO: agument with real semantic tokens?
            return tokensBuilder.build();
        });
    }
}
exports.DocumentSemanticTokensProvider = DocumentSemanticTokensProvider;
//# sourceMappingURL=documentSemanticTokenProvider.js.map
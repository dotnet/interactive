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
exports.DynamicGrammarSemanticTokenProvider = void 0;
const fs = require("fs");
const path = require("path");
const oniguruma = require("vscode-oniguruma");
const vsctm = require("vscode-textmate");
const logger_1 = require("./dotnet-interactive/logger");
const customScopePrefix = 'polyglot-notebook';
;
const languageSelector = '#!';
const magicCommandSelector = '#!';
// grammars shipped with this extension
const wellKnownLanguages = [
    { languageName: 'kql', grammarFileName: 'kql.tmGrammar.json', aliases: [] },
];
// aliases that we might want to manually specify
const wellKnownAliases = [
    { languageName: 'sql', aliases: ['SQLite', 'T-SQL', 'PL/SQL'] }
];
class DynamicGrammarSemanticTokenProvider {
    constructor(packageJSON, extensionData, fileExists, fileReader) {
        this.fileExists = fileExists;
        this.fileReader = fileReader;
        this._documentKernelInfos = new Map();
        this._documentGrammarRegistries = new Map();
        this._textMateScopeToSemanticType = new Map();
        this._languageNameInfoMap = new Map();
        this.buildInstalledLanguageInfosMap(extensionData);
        this.buildSemanticTokensLegendAndCustomScopes(packageJSON);
    }
    get semanticTokenTypes() {
        return [...this._textMateScopeToSemanticType.values()];
    }
    buildSemanticTokensLegendAndCustomScopes(packageJSON) {
        var _a;
        if (Array.isArray((_a = packageJSON === null || packageJSON === void 0 ? void 0 : packageJSON.contributes) === null || _a === void 0 ? void 0 : _a.semanticTokenScopes)) {
            for (const semanticTokenScope of packageJSON.contributes.semanticTokenScopes) {
                if (semanticTokenScope === null || semanticTokenScope === void 0 ? void 0 : semanticTokenScope.scopes) {
                    for (const scopeName in semanticTokenScope.scopes) {
                        // build a mapping of custom scopes
                        for (const textMateScope of semanticTokenScope.scopes[scopeName]) {
                            this._textMateScopeToSemanticType.set(textMateScope, scopeName);
                        }
                    }
                }
            }
        }
    }
    init() {
        return __awaiter(this, void 0, void 0, function* () {
            // prepare grammar parser
            const nodeModulesDir = path.join(__dirname, '..', '..', '..', 'node_modules');
            const onigWasmPath = path.join(nodeModulesDir, 'vscode-oniguruma', 'release', 'onig.wasm');
            const wasmBin = fs.readFileSync(onigWasmPath).buffer;
            yield oniguruma.loadWASM(wasmBin);
        });
    }
    getLanguageNameFromKernelNameOrAlias(notebookDocument, kernelNameOrAlias) {
        // get the notebook's kernel info map so we can look up the language
        const kernelInfoMap = this._documentKernelInfos.get(notebookDocument.uri);
        if (kernelInfoMap) {
            // first do a direct kernel name lookup
            const kernelInfo = kernelInfoMap.get(kernelNameOrAlias);
            if (kernelInfo) {
                return kernelInfo.languageName;
            }
            // no direct match found, search through the aliases
            for (const kernelInfo of kernelInfoMap.values()) {
                if (kernelInfo.aliases.indexOf(kernelNameOrAlias) >= 0) {
                    return kernelInfo.languageName;
                }
            }
        }
        // no match found
        return undefined;
    }
    getTokens(notebookUri, initialKernelName, code) {
        return __awaiter(this, void 0, void 0, function* () {
            let registry = this._documentGrammarRegistries.get(notebookUri);
            if (!registry) {
                // no grammar registry for this notebook, nothing to provide
                return [];
            }
            try {
                const grammar = yield registry.loadGrammar(`source.${customScopePrefix}.${initialKernelName}`);
                if (grammar) {
                    let ruleStack = vsctm.INITIAL;
                    const semanticTokens = [];
                    const lines = code.split('\n');
                    for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
                        const line = lines[lineIndex];
                        const parsedLineTokens = grammar.tokenizeLine(line, ruleStack);
                        for (let tokenIndex = 0; tokenIndex < parsedLineTokens.tokens.length; tokenIndex++) {
                            const token = parsedLineTokens.tokens[tokenIndex];
                            const tokenText = line.substring(token.startIndex, token.endIndex);
                            const tokenType = this.scopeNameToTokenType(token.scopes, tokenText);
                            if (tokenType) {
                                const semanticToken = {
                                    line: lineIndex,
                                    startColumn: token.startIndex,
                                    endColumn: token.endIndex,
                                    tokenType: tokenType,
                                    tokenModifiers: [],
                                };
                                semanticTokens.push(semanticToken);
                            }
                        }
                        ruleStack = parsedLineTokens.ruleStack;
                    }
                    return semanticTokens;
                }
            }
            catch (e) {
                const x = e;
            }
            // if we got here we didn't recognize the given language
            return [];
        });
    }
    /**
     * Update the given notebook's grammar registry with the provided language infos.
     * @param notebookUri The URI of the notebook for which we're updating the grammars.
     * @param kernelInfos The kernel info objects used to build the custom grammar.
     */
    rebuildNotebookGrammar(notebookUri, kernelInfos, replaceExistingGrammar) {
        // ensure we have a collection of <kernelName, KernelInfo[]>
        let documentKernelInfos = this._documentKernelInfos.get(notebookUri);
        if (!documentKernelInfos || replaceExistingGrammar) {
            // no existing KernelInfo collection, or we're explicitly replacing an existing one
            documentKernelInfos = new Map();
        }
        // update that collection with new/changed values
        for (const kernelInfo of kernelInfos) {
            documentKernelInfos.set(kernelInfo.localName, kernelInfo);
        }
        // keep that collection for next time
        this._documentKernelInfos.set(notebookUri, documentKernelInfos);
        // rebuild a new grammar with it
        const updatedKernelInfos = [...documentKernelInfos.values()];
        const notebookGrammarRegistry = this.createGrammarRegistry(updatedKernelInfos);
        this._documentGrammarRegistries.set(notebookUri, notebookGrammarRegistry);
    }
    buildInstalledLanguageInfosMap(extensionData) {
        var _a, _b, _c, _d;
        // crawl all extensions for languages and grammars
        this._languageNameInfoMap.clear();
        const seenLanguages = new Set();
        // grammars shipped with this extension
        const grammarDir = path.join(__dirname, '..', '..', '..', 'grammars');
        for (const wellKnown of wellKnownLanguages) {
            const grammarPath = path.join(grammarDir, wellKnown.grammarFileName);
            const languageInfo = this.createLanguageInfoFromGrammar(normalizeLanguageName(wellKnown.languageName), `source.${wellKnown.languageName}`, grammarPath);
            for (const languageNameOrAlias of [wellKnown.languageName, ...wellKnown.aliases].map(normalizeLanguageName)) {
                this._languageNameInfoMap.set(languageNameOrAlias, languageInfo);
                seenLanguages.add(languageNameOrAlias);
            }
        }
        for (let extensionIndex = 0; extensionIndex < extensionData.length; extensionIndex++) {
            const extension = extensionData[extensionIndex];
            // gather all grammars
            if (Array.isArray((_b = (_a = extension.packageJSON) === null || _a === void 0 ? void 0 : _a.contributes) === null || _b === void 0 ? void 0 : _b.grammars)) {
                for (let grammarIndex = 0; grammarIndex < extension.packageJSON.contributes.grammars.length; grammarIndex++) {
                    const grammar = extension.packageJSON.contributes.grammars[grammarIndex];
                    if (typeof (grammar === null || grammar === void 0 ? void 0 : grammar.scopeName) === 'string' &&
                        typeof (grammar === null || grammar === void 0 ? void 0 : grammar.path) === 'string') {
                        // ensure language is in the map
                        const languageName = normalizeLanguageName(typeof grammar.language === 'string' ? grammar.language : extension.packageJSON.name);
                        if (!seenLanguages.has(languageName)) {
                            const grammarPath = path.join(extension.extensionPath, grammar.path);
                            if (this.fileExists(grammarPath)) {
                                const languageInfo = this.createLanguageInfoFromGrammar(languageName, grammar.scopeName, grammarPath);
                                this._languageNameInfoMap.set(languageName, languageInfo);
                                seenLanguages.add(languageName);
                            }
                        }
                    }
                }
            }
            // set any aliases
            if (Array.isArray((_d = (_c = extension.packageJSON) === null || _c === void 0 ? void 0 : _c.contributes) === null || _d === void 0 ? void 0 : _d.languages)) {
                for (let languageIndex = 0; languageIndex < extension.packageJSON.contributes.languages.length; languageIndex++) {
                    const language = extension.packageJSON.contributes.languages[languageIndex];
                    const languageId = normalizeLanguageName(language.id);
                    const languageInfo = this._languageNameInfoMap.get(languageId);
                    if (languageInfo) {
                        const aliases = Array.isArray(language.aliases) ? language.aliases : [];
                        for (const alias of aliases.map(normalizeLanguageName)) {
                            this._languageNameInfoMap.set(alias, languageInfo);
                        }
                    }
                }
            }
        }
        // set any extra aliases, but only if they're not already set
        for (const wellKnownAlias of wellKnownAliases) {
            const languageInfo = this._languageNameInfoMap.get(normalizeLanguageName(wellKnownAlias.languageName));
            if (languageInfo) {
                for (const alias of wellKnownAlias.aliases.map(normalizeLanguageName)) {
                    if (!this._languageNameInfoMap.has(alias)) {
                        this._languageNameInfoMap.set(alias, languageInfo);
                    }
                }
            }
        }
    }
    createLanguageInfoFromGrammar(languageName, scopeName, grammarPath) {
        const grammarContents = this.fileReader(grammarPath);
        const grammarExtension = path.extname(grammarPath).substring(1);
        const languageInfo = { languageName, scopeName, grammar: { grammarContents, extension: grammarExtension } };
        return languageInfo;
    }
    createGrammarRegistry(kernelInfos) {
        const scopeNameToGrammarMap = new Map();
        const allKernelNamesSet = new Set();
        for (const kernelInfo of kernelInfos) {
            allKernelNamesSet.add(kernelInfo.localName);
            for (const alias of kernelInfo.aliases) {
                allKernelNamesSet.add(alias);
            }
        }
        // create root language
        const allKernelNames = Array.from(allKernelNamesSet);
        const endPattern = `(?=^${languageSelector}(${allKernelNames.join('|')})\\s+$)`;
        const rootGrammar = {
            scopeName: `source.${customScopePrefix}`,
            patterns: kernelInfos.map(kernelInfo => {
                const selectors = [kernelInfo.localName, ...kernelInfo.aliases];
                return {
                    begin: `^${languageSelector}(${selectors.join('|')})\\s+$`,
                    end: endPattern,
                    name: `language.switch.${languageNameFromKernelInfo(kernelInfo)}`,
                    patterns: [
                        {
                            include: `source.${customScopePrefix}.${kernelInfo.localName}`,
                        }
                    ]
                };
            })
        };
        const rootGrammarContents = JSON.stringify(rootGrammar);
        scopeNameToGrammarMap.set(rootGrammar.scopeName, { grammarContents: rootGrammarContents, extension: 'json', });
        // create magic command language
        const magicCommandBegin = `^(${magicCommandSelector})(?!(${allKernelNames.join('|')}))`;
        const magicCommandGrammar = {
            scopeName: `source.${customScopePrefix}.magic-commands`,
            patterns: [
                {
                    name: 'comment.line.magic-commands',
                    begin: magicCommandBegin,
                    end: '(?<=$)',
                    beginCaptures: {
                        '1': {
                            name: 'comment.line.magic-commands.hash-bang'
                        }
                    },
                    patterns: [
                        {
                            include: '#magic-command-name',
                        },
                        {
                            include: '#strings',
                        },
                        {
                            include: '#option',
                        },
                        {
                            include: '#argument',
                        },
                    ]
                }
            ],
            repository: {
                'magic-command-name': {
                    patterns: [
                        {
                            name: 'keyword.control.magic-commands',
                            match: `(?<=^${magicCommandSelector})[^\\s\\\"]+`,
                        }
                    ]
                },
                'option': {
                    patterns: [
                        {
                            name: 'constant.language.magic-commands',
                            match: '(--?|/)[^\\s\\\"]+',
                        }
                    ]
                },
                'argument': {
                    patterns: [
                        {
                            name: 'constant.numeric.magic-commands',
                            match: '[^\\s\\\"]+',
                        }
                    ]
                },
                'strings': {
                    patterns: [
                        {
                            name: 'string.quoted.double.magic-commands',
                            begin: '\"',
                            end: '\"',
                            patterns: [
                                {
                                    name: 'constant.character.escape.magic-commands',
                                    match: '\\.',
                                }
                            ]
                        }
                    ]
                }
            }
        };
        const magicCommandGrammarContents = JSON.stringify(magicCommandGrammar, null, 2);
        scopeNameToGrammarMap.set(magicCommandGrammar.scopeName, { grammarContents: magicCommandGrammarContents, extension: 'json', });
        // create individual langauges
        kernelInfos.forEach(kernelInfo => {
            // ensure we have some kind of language name, even if it doesn't map to anything
            const scopeName = `source.${customScopePrefix}.${kernelInfo.localName}`;
            const patterns = [
                {
                    include: `source.${customScopePrefix}.magic-commands`
                },
                {
                    include: `source.${customScopePrefix}`
                }
            ];
            const languageInfo = this._languageNameInfoMap.get(languageNameFromKernelInfo(kernelInfo));
            if (languageInfo) {
                patterns.push({
                    include: languageInfo.scopeName
                });
            }
            const languageGrammar = {
                scopeName,
                patterns
            };
            const languageGrammarContents = JSON.stringify(languageGrammar);
            // set custom version of the language
            scopeNameToGrammarMap.set(languageGrammar.scopeName, { grammarContents: languageGrammarContents, extension: 'json', });
            // set the real language
            if (languageInfo === null || languageInfo === void 0 ? void 0 : languageInfo.grammar) {
                scopeNameToGrammarMap.set(languageInfo.scopeName, { grammarContents: languageInfo.grammar.grammarContents, extension: languageInfo.grammar.extension, });
            }
        });
        // prepare grammar scope loader
        const registry = new vsctm.Registry({
            onigLib: Promise.resolve({
                createOnigScanner: (sources) => new oniguruma.OnigScanner(sources),
                createOnigString: (str) => new oniguruma.OnigString(str)
            }),
            loadGrammar: (scopeName) => {
                return new Promise((resolve, _reject) => {
                    const grammarContentPair = scopeNameToGrammarMap.get(scopeName);
                    if (grammarContentPair) {
                        const grammar = vsctm.parseRawGrammar(grammarContentPair.grammarContents, `${scopeName}.${grammarContentPair.extension}`);
                        resolve(grammar);
                    }
                    else {
                        resolve(null);
                    }
                });
            }
        });
        return registry;
    }
    scopeNameToTokenType(scopeNames, tokenText) {
        const attemptedScopeNames = [];
        for (let i = scopeNames.length - 1; i >= 0; i--) {
            const scopeName = scopeNames[i];
            const scopeParts = scopeName.split('.');
            for (let j = scopeParts.length; j >= 1; j--) {
                const rebuiltScopeName = scopeParts.slice(0, j).join('.');
                attemptedScopeNames.push(rebuiltScopeName);
                const customScopeName = this._textMateScopeToSemanticType.get(rebuiltScopeName);
                if (customScopeName) {
                    return customScopeName;
                }
            }
        }
        if (scopeNames.length === 1 && scopeNames[0].startsWith(`source.${customScopePrefix}.`)) {
            // suppress log for scopes like "source.polyglot-notebook.csharp"
        }
        else {
            logger_1.Logger.default.info(`Unsupported scope mapping [${attemptedScopeNames.join(', ')}] for token text [${tokenText}]`);
        }
        return null;
    }
}
exports.DynamicGrammarSemanticTokenProvider = DynamicGrammarSemanticTokenProvider;
function normalizeLanguageName(languageName) {
    return languageName.toLowerCase();
}
function languageNameFromKernelInfo(kernelInfo) {
    var _a;
    // ensure we have some kind of language name, even if it doesn't map to anything
    return normalizeLanguageName((_a = kernelInfo.languageName) !== null && _a !== void 0 ? _a : `unknown-language-from-kernel-${kernelInfo.localName}`);
}
//# sourceMappingURL=dynamicGrammarSemanticTokenProvider.js.map
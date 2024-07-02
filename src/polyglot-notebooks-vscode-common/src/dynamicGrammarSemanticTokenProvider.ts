// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from './polyglot-notebooks/commandsAndEvents';
import * as fs from 'fs';
import * as path from 'path';
import * as oniguruma from 'vscode-oniguruma';
import * as vscodeLike from './interfaces/vscode-like';
import * as vsctm from 'vscode-textmate';

import { Logger } from './polyglot-notebooks/logger';
import { t } from '@vscode/l10n';

const customScopePrefix = 'polyglot-notebook';

export interface GrammarPair {
    grammarContents: string;
    extension: string;
}

export interface LanguageInfo {
    languageName: string;
    scopeName: string;
    grammar: GrammarPair | null;
};

export interface SemanticToken {
    line: number;
    startColumn: number;
    endColumn: number;
    tokenType: string;
    tokenModifiers: string[];
}

export interface VSCodeExtensionLike {
    id: string;
    extensionPath: string;
    packageJSON: any;
}

const languageSelector: string = '#!';
const magicCommandSelector: string = '#!';

// grammars shipped with this extension
const wellKnownLanguages: { languageName: string, aliases: string[] }[] = [
    { languageName: 'kql', aliases: [] },
    { languageName: 'http', aliases: [] },
];

// aliases that we might want to manually specify
const wellKnownAliases: { languageName: string, aliases: string[] }[] = [
    { languageName: 'sql', aliases: ['SQLite', 'T-SQL', 'PL/SQL'] }
];

export class DynamicGrammarSemanticTokenProvider {
    private _documentKernelInfos: Map<vscodeLike.Uri, Map<string, commandsAndEvents.KernelInfo>> = new Map();
    private _documentGrammarRegistries: Map<vscodeLike.Uri, vsctm.Registry> = new Map();
    private _textMateScopeToSemanticType: Map<string, string> = new Map();
    private _languageNameConfigurationMap: Map<string, any> = new Map();
    private _languageNameInfoMap: Map<string, LanguageInfo> = new Map();

    // This is used as a fallback when a language doesn't have a registered grammar, e.g., Mermaid.  Empty properties
    // are required to prevent the editor from falling back to the previously applied language configuration.
    private _emptyLanguageConfiguration: any = {
        comments: {},
        brackets: [],
        autoClosingPairs: [],
        surroundingPairs: [],
        folding: {},
    };

    constructor(packageJSON: any, extensionData: VSCodeExtensionLike[], private readonly fileExists: (path: string) => boolean, private readonly fileReader: (path: string) => string) {
        try {
            this.buildInstalledLanguageInfosMap(extensionData);
            this.buildSemanticTokensLegendAndCustomScopes(packageJSON);
        } catch (error) {
            Logger.default.error(`Error building dynamic grammar semantic token provider: ${error}`);
            throw error;
        }
    }

    get semanticTokenTypes(): string[] {
        return [...this._textMateScopeToSemanticType.values()];
    }

    private buildSemanticTokensLegendAndCustomScopes(packageJSON: any) {
        if (Array.isArray(packageJSON?.contributes?.semanticTokenScopes)) {
            for (const semanticTokenScope of packageJSON.contributes.semanticTokenScopes) {
                if (semanticTokenScope?.scopes) {
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

    async init(): Promise<void> {
        try {
            // prepare grammar parser
            const nodeModulesDir = path.join(__dirname, '..', '..', '..', 'node_modules');
            const onigWasmPath = path.join(nodeModulesDir, 'vscode-oniguruma', 'release', 'onig.wasm');
            const wasmBin = fs.readFileSync(onigWasmPath).buffer;
            await oniguruma.loadWASM(wasmBin);
        }
        catch (error) {
            Logger.default.error(`Error itnitialising the DynamicGrammarSemanticTokenProvider : ${error}`);
            throw error;
        }
    }

    getLanguageNameFromKernelNameOrAlias(notebookDocument: vscodeLike.NotebookDocument, kernelNameOrAlias: string): string | undefined {
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

    getLanguageConfigurationFromKernelNameOrAlias(notebookDocument: vscodeLike.NotebookDocument, kernelNameOrAlias: string): any {
        try {
            let languageConfiguration = this._emptyLanguageConfiguration;
            const languageName = this.getLanguageNameFromKernelNameOrAlias(notebookDocument, kernelNameOrAlias);
            if (languageName) {
                const normalizedLanguageName = normalizeLanguageName(languageName);
                languageConfiguration = this._languageNameConfigurationMap.get(normalizedLanguageName) ?? languageConfiguration;
            }

            return languageConfiguration;
        }
        catch (error) {
            Logger.default.error(`Error getting language configuration for kernel ${kernelNameOrAlias}: ${error}`);
            throw error;
        }
    }

    async getTokens(notebookUri: vscodeLike.Uri, initialKernelName: string, code: string): Promise<SemanticToken[]> {
        let registry = this._documentGrammarRegistries.get(notebookUri);
        if (!registry) {
            // no grammar registry for this notebook, nothing to provide
            return [];
        }

        try {
            Logger.default.info(`loading tokens for: source.${customScopePrefix}.${initialKernelName}`);
            const grammar = await registry.loadGrammar(`source.${customScopePrefix}.${initialKernelName}`);
            if (grammar) {
                let ruleStack = vsctm.INITIAL;
                const semanticTokens: SemanticToken[] = [];
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
        } catch (error) {
            Logger.default.error(`Error getting tokens for notebook ${notebookUri.toString()}: ${error}`);
            // not rethrowing, skipping
        }

        // if we got here we didn't recognize the given language
        return [];
    }

    /**
     * Update the given notebook's grammar registry with the provided language infos.
     * @param notebookUri The URI of the notebook for which we're updating the grammars.
     * @param kernelInfos The kernel info objects used to build the custom grammar.
     */
    rebuildNotebookGrammar(notebookUri: vscodeLike.Uri, kernelInfos: commandsAndEvents.KernelInfo[], replaceExistingGrammar?: boolean | undefined) {
        // ensure we have a collection of <kernelName, KernelInfo[]>
        let documentKernelInfos = this._documentKernelInfos.get(notebookUri);
        if (!documentKernelInfos || replaceExistingGrammar) {
            // no existing KernelInfo collection, or we're explicitly replacing an existing one
            documentKernelInfos = new Map();
        }

        // update that collection with new/changed values
        let hasMarkdownKernel = false;
        for (const kernelInfo of kernelInfos) {
            documentKernelInfos.set(kernelInfo.localName, kernelInfo);
            if (kernelInfo.localName.toLocaleLowerCase() === 'markdown') {
                hasMarkdownKernel = true;
            }
        }

        if (!hasMarkdownKernel) {
            // the markdown kernel isn't real, but still needs to be accounted for in the grammar
            documentKernelInfos.set('markdown', {
                isComposite: false,
                isProxy: false,
                localName: 'markdown', // always assume it's called #!markdown
                displayName: 'unused',
                languageName: 'markdown',
                aliases: ['md'],
                supportedKernelCommands: [],
                uri: 'unused',
            });
        }

        // keep that collection for next time
        this._documentKernelInfos.set(notebookUri, documentKernelInfos);

        // rebuild a new grammar with it
        const updatedKernelInfos = [...documentKernelInfos.values()];
        const notebookGrammarRegistry = this.createGrammarRegistry(updatedKernelInfos);
        this._documentGrammarRegistries.set(notebookUri, notebookGrammarRegistry);
    }

    private buildInstalledLanguageInfosMap(extensionData: VSCodeExtensionLike[]) {
        Logger.default.info(`Building installed language infos map...`);
        // crawl all extensions for languages and grammars
        this._languageNameInfoMap.clear();
        const seenLanguages: Set<string> = new Set();

        // grammars shipped with this extension
        const grammarDir = path.join(__dirname, '..', '..', '..', 'grammars');
        for (const wellKnown of wellKnownLanguages) {
            const grammarPath = path.join(grammarDir, `${wellKnown.languageName}.tmGrammar.json`);
            const languageInfo = this.createLanguageInfoFromGrammar(normalizeLanguageName(wellKnown.languageName), `source.${wellKnown.languageName}`, grammarPath);
            const allNames = [wellKnown.languageName, ...wellKnown.aliases].map(normalizeLanguageName);
            for (const languageNameOrAlias of allNames) {
                this._languageNameInfoMap.set(languageNameOrAlias, languageInfo);
                seenLanguages.add(languageNameOrAlias);
            }

            // set language configuration
            const languageConfigurationFilePath = path.join(grammarDir, `${wellKnown.languageName}.language-configuration.json`);
            Logger.default.info(`Looking for language configuration file at ${languageConfigurationFilePath}`);
            if (this.fileExists(languageConfigurationFilePath)) {
                try {
                    Logger.default.info(`Found language configuration file at ${languageConfigurationFilePath}`);
                    const languageConfigurationContents = this.fileReader(languageConfigurationFilePath);
                    const languageConfiguration = parseLanguageConfiguration(languageConfigurationContents);
                    for (const languageNameOrAlias of allNames) {
                        this._languageNameConfigurationMap.set(languageNameOrAlias, languageConfiguration);
                    }
                    Logger.default.info(`Parsed language configuration file at ${languageConfigurationFilePath}`);
                } catch (error) {
                    Logger.default.error(`Error parsing language configuration file at ${languageConfigurationFilePath}: ${error}`);
                    // not rethrowing
                }
            }
        }

        for (let extensionIndex = 0; extensionIndex < extensionData.length; extensionIndex++) {
            const extension = extensionData[extensionIndex];

            // gather all grammars
            if (Array.isArray(extension.packageJSON?.contributes?.grammars)) {
                for (let grammarIndex = 0; grammarIndex < extension.packageJSON.contributes.grammars.length; grammarIndex++) {
                    const grammar = extension.packageJSON.contributes.grammars[grammarIndex];
                    if (typeof grammar?.scopeName === 'string' &&
                        typeof grammar?.path === 'string') {
                        // ensure language is in the map
                        const languageName = normalizeLanguageName(typeof grammar.language === 'string' ? <string>grammar.language : <string>extension.packageJSON.name);
                        if (!seenLanguages.has(languageName)) {
                            const grammarPath = path.join(extension.extensionPath, grammar.path);
                            Logger.default.info(`Looking for grammar file at ${grammarPath}`);
                            if (this.fileExists(grammarPath)) {
                                try {
                                    Logger.default.info(`Found grammar file at ${grammarPath}`);
                                    const languageInfo = this.createLanguageInfoFromGrammar(languageName, grammar.scopeName, grammarPath);
                                    this._languageNameInfoMap.set(languageName, languageInfo);
                                    seenLanguages.add(languageName);
                                    Logger.default.info(`Parsed grammar file at ${grammarPath}`);
                                }
                                catch (error) {
                                    Logger.default.error(`Error parsing grammar file at ${grammarPath}: ${error}`);
                                    throw error;
                                }
                            }
                        }
                    }
                }
            }

            // set any aliases
            if (Array.isArray(extension.packageJSON?.contributes?.languages)) {
                for (let languageIndex = 0; languageIndex < extension.packageJSON.contributes.languages.length; languageIndex++) {
                    const language = extension.packageJSON.contributes.languages[languageIndex];
                    const languageId = normalizeLanguageName(<string>language.id);

                    // set language configuration
                    let languageConfigurationObject: any | undefined = undefined;
                    if (typeof language.configuration === 'string') {
                        const languageConfiguration = <string>language.configuration;
                        const languageConfigurationPath = path.join(extension.extensionPath, languageConfiguration);
                        Logger.default.info(`Looking for language configuration file at ${languageConfigurationPath}`);
                        if (this.fileExists(languageConfigurationPath)) {
                            Logger.default.info(`Found language configuration file at ${languageConfigurationPath}`);
                            const languageConfigurationContents = this.fileReader(languageConfigurationPath);
                            try {
                                languageConfigurationObject = parseLanguageConfiguration(languageConfigurationContents);
                                this._languageNameConfigurationMap.set(languageId, languageConfigurationObject);
                                Logger.default.info(`Parsed language configuration file at ${languageConfigurationPath}`);
                            } catch {
                                Logger.default.error(`Error parsing language configuration for language ${languageId}`);
                                // not rethrowing
                            }
                        }
                    }

                    // set language info
                    const languageInfo = this._languageNameInfoMap.get(languageId);
                    if (languageInfo) {
                        const aliases: string[] = (Array.isArray(language.aliases) ? <any[]>language.aliases : []).filter(a => typeof a === 'string');
                        for (const alias of aliases.map(normalizeLanguageName)) {
                            this._languageNameInfoMap.set(alias, languageInfo);
                            if (languageConfigurationObject) {
                                this._languageNameConfigurationMap.set(alias, languageConfigurationObject);
                            }
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

    private createLanguageInfoFromGrammar(languageName: string, scopeName: string, grammarPath: string): LanguageInfo {
        const grammarContents = this.fileReader(grammarPath);
        const grammarExtension = path.extname(grammarPath).substring(1);
        const languageInfo = { languageName, scopeName, grammar: { grammarContents, extension: grammarExtension } };
        return languageInfo;
    }

    private createGrammarRegistry(kernelInfos: commandsAndEvents.KernelInfo[]): vsctm.Registry {
        const scopeNameToGrammarMap: Map<string, GrammarPair> = new Map();
        const allKernelNamesSet: Set<string> = new Set();
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
                const selectors: string[] = [kernelInfo.localName, ...kernelInfo.aliases];
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
            if (languageInfo?.grammar) {
                scopeNameToGrammarMap.set(languageInfo.scopeName, { grammarContents: languageInfo.grammar.grammarContents, extension: languageInfo.grammar.extension, });
            }
        });

        const isEmpty = (text: string) => {
            return text === null || text.match(/^\s*$/) !== null;
        };

        // prepare grammar scope loader
        const registry = new vsctm.Registry({
            onigLib: Promise.resolve({
                createOnigScanner: (sources) => new oniguruma.OnigScanner(sources),
                createOnigString: (str) => new oniguruma.OnigString(str)
            }),
            loadGrammar: (scopeName) => {
                return new Promise<vsctm.IRawGrammar | null>((resolve, reject) => {
                    Logger.default.info(`-------------------------Loading grammar for scope ${scopeName}`);
                    const grammarContentPair = scopeNameToGrammarMap.get(scopeName);
                    if (grammarContentPair) {
                        try {
                            if (isEmpty(grammarContentPair.grammarContents)) {
                                Logger.default.warn(`Empty grammar for scope ${scopeName}`);
                            }
                            const grammar = vsctm.parseRawGrammar(grammarContentPair.grammarContents, `${scopeName}.${grammarContentPair.extension}`);
                            Logger.default.info(`Finished loading rammar for scope ${scopeName} :
                            name            : ${grammar.name}
                            pattern count   : ${grammar.patterns?.length}
                            pattern names   : ${grammar.patterns?.map(p => p.name).join(', ')}
                            file location   : ${grammar.$vscodeTextmateLocation?.filename}
                            -------------------------`);
                            resolve(grammar);
                        } catch (error) {
                            Logger.default.error(`Error loading grammar for scope ${scopeName}: ${error}
                            -------------------------`);
                            reject(error);
                        }
                    } else {
                        Logger.default.error(`Error loading grammar for scope ${scopeName}: no grammar found
                        -------------------------`);
                        resolve(null);
                    }
                });
            }
        });

        return registry;
    }



    private scopeNameToTokenType(scopeNames: string[], tokenText: string): string | null {
        const attemptedScopeNames: string[] = [];
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
        } else {
            Logger.default.info(`Unsupported scope mapping [${attemptedScopeNames.join(', ')}] for token text [${tokenText}]`);
        }

        return null;
    }
}

function normalizeLanguageName(languageName: string): string {
    return languageName.toLowerCase();
}

function languageNameFromKernelInfo(kernelInfo: commandsAndEvents.KernelInfo): string {
    // ensure we have some kind of language name, even if it doesn't map to anything
    return normalizeLanguageName(kernelInfo.languageName ?? `unknown-language-from-kernel-${kernelInfo.localName}`);
}

export function parseLanguageConfiguration(content: string): any {
    const languageConfigurationObject = JSON.parse(content);

    fixRegExpProperty(languageConfigurationObject, 'wordPattern');

    fixAutoClosingPairs(languageConfigurationObject);

    if (typeof languageConfigurationObject.indentationRules === 'object') {
        fixRegExpProperty(languageConfigurationObject.indentationRules, 'decreaseIndentPattern');
        fixRegExpProperty(languageConfigurationObject.indentationRules, 'increaseIndentPattern');
        fixRegExpProperty(languageConfigurationObject.indentationRules, 'indentNextLinePattern');
        fixRegExpProperty(languageConfigurationObject.indentationRules, 'unIndentedLinePattern');
    }

    if (Array.isArray(languageConfigurationObject.onEnterRules)) {
        languageConfigurationObject.onEnterRules.forEach((rule: any) => {
            fixRegExpProperty(rule, 'beforeText');
            fixRegExpProperty(rule, 'afterText');
            fixRegExpProperty(rule, 'previousLineText');
        });
    }

    return languageConfigurationObject;
}

function fixAutoClosingPairs(value: any) {
    if (value.autoClosingPairs && Array.isArray(value.autoClosingPairs)) {
        const newAutoClosingPairs: any[] = [];
        for (let i = 0; i < value.autoClosingPairs.length; i++) {
            const pair = value.autoClosingPairs[i];
            if (Array.isArray(pair) && pair.length === 2) {
                newAutoClosingPairs.push({
                    open: pair[0],
                    close: pair[1]
                }
                );
            } else if (typeof pair === 'object' && pair.open && pair.close) {
                newAutoClosingPairs.push(pair);
            }
        }

        value.autoClosingPairs = newAutoClosingPairs;
    }
}

function fixRegExpProperty(value: any, propertyName: string) {
    if (typeof value[propertyName] === 'string') {
        value[propertyName] = new RegExp(value[propertyName]);
    } else if (typeof value[propertyName] === 'object' && typeof value[propertyName].pattern === 'string') {
        value[propertyName] = new RegExp(value[propertyName].pattern);
    }
}

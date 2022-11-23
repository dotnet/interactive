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
const path = require("path");
const chai_1 = require("chai");
const dynamicGrammarSemanticTokenProvider_1 = require("../../src/vscode-common/dynamicGrammarSemanticTokenProvider");
const dotnet_interactive_1 = require("../../src/vscode-common/dotnet-interactive");
describe('dynamic grammar tests', () => __awaiter(void 0, void 0, void 0, function* () {
    let logMessages = [];
    let grammarContentsByPath;
    const testUri = {
        fsPath: '',
        scheme: ''
    };
    let dynamicTokenProvider;
    const defaultKernelInfos = [
        {
            localName: 'test-csharp',
            uri: 'kernel://test-csharp',
            languageName: 'csharp',
            aliases: ['see-sharp-alias'],
            displayName: '',
            supportedKernelCommands: [],
            supportedDirectives: [],
        },
        {
            localName: 'test-python',
            uri: 'kernel://test-python',
            languageName: 'python',
            aliases: [],
            displayName: '',
            supportedKernelCommands: [],
            supportedDirectives: [],
        }
    ];
    // set this value to true to see all diagnostic messages produced during the test
    const displayDiagnosticMessages = false;
    dotnet_interactive_1.Logger.configure('test-host', (entry) => {
        logMessages.push(entry.message);
    });
    function getTokens(initialKernelName, code) {
        return __awaiter(this, void 0, void 0, function* () {
            const lines = code.split('\n');
            const tokens = yield dynamicTokenProvider.getTokens(testUri, initialKernelName, code);
            return tokens.map(token => {
                return {
                    tokenText: lines[token.line].substring(token.startColumn, token.endColumn),
                    tokenType: token.tokenType
                };
            });
        });
    }
    it('sub parsing within magic commands', () => __awaiter(void 0, void 0, void 0, function* () {
        const code = '#!some-magic-command --option1 value1 /option2 value2 argument1 "some string"';
        const tokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(tokens).to.deep.equal([
            {
                tokenText: '#!',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'some-magic-command',
                tokenType: 'polyglot-notebook-keyword-control'
            },
            {
                tokenText: ' ',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '--option1',
                tokenType: 'polyglot-notebook-constant-language'
            },
            {
                tokenText: ' ',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'value1',
                tokenType: 'polyglot-notebook-constant-numeric'
            },
            {
                tokenText: ' ',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '/option2',
                tokenType: 'polyglot-notebook-constant-language'
            },
            {
                tokenText: ' ',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'value2',
                tokenType: 'polyglot-notebook-constant-numeric'
            },
            {
                tokenText: ' ',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'argument1',
                tokenType: 'polyglot-notebook-constant-numeric'
            },
            {
                tokenText: ' ',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '"',
                tokenType: 'polyglot-notebook-string'
            },
            {
                tokenText: 'some string',
                tokenType: 'polyglot-notebook-string'
            },
            {
                tokenText: '"',
                tokenType: 'polyglot-notebook-string'
            }
        ]);
    }));
    it("magic commands don't invalidate the language selector", () => __awaiter(void 0, void 0, void 0, function* () {
        const code = `
// csharp comment 1
#!some-magic-command
// csharp comment 2
`;
        const tokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(tokens).to.deep.equal([
            {
                tokenText: '// csharp comment 1',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '#!',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'some-magic-command',
                tokenType: 'polyglot-notebook-keyword-control'
            },
            {
                tokenText: '// csharp comment 2',
                tokenType: 'polyglot-notebook-comment'
            },
        ]);
    }));
    it("tokens are classified according to each language's grammar", () => __awaiter(void 0, void 0, void 0, function* () {
        const code = `
// C# comment
# C# keyword

#!test-python

# Python comment
// Python keyword
`;
        const tokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(tokens).to.deep.equal([
            {
                tokenText: '// C# comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '# C# keyword',
                tokenType: 'polyglot-notebook-keyword'
            },
            {
                tokenText: '# Python comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '// Python keyword',
                tokenType: 'polyglot-notebook-keyword'
            }
        ]);
    }));
    it('language switch can be specified with a kernel alias', () => __awaiter(void 0, void 0, void 0, function* () {
        const code = `
#!see-sharp-alias
// C# alias comment
`;
        // defaulting to `python` to prove that the kernel selector worked
        const tokens = yield getTokens('test-python', code);
        (0, chai_1.expect)(tokens).to.deep.equal([
            {
                tokenText: '// C# alias comment',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    }));
    it('token parsing is updated when grammar is rebuilt with _ALL_ new KernelInfos', () => __awaiter(void 0, void 0, void 0, function* () {
        const code = `
// C# Comment
#!test-erlang
% Erlang comment
`;
        // looks like a magic command followed by garbage
        const initialTokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(initialTokens).to.deep.equal([
            {
                tokenText: '// C# Comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '#!',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'test-erlang',
                tokenType: 'polyglot-notebook-keyword-control'
            }
        ]);
        // rebuild the grammar with all new KernelInfos
        const updatedKernelInfos = [
            {
                localName: 'test-erlang',
                uri: 'kernel://test-erlang',
                languageName: 'erlang',
                aliases: [],
                displayName: '',
                supportedKernelCommands: [],
                supportedDirectives: [],
            },
            ...defaultKernelInfos
        ];
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, updatedKernelInfos);
        // looks like a proper token
        const realTokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(realTokens).to.deep.equal([
            {
                tokenText: '// C# Comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '% Erlang comment',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    }));
    it('token parsing is updated when grammar is rebuilt with _SOME_ new KernelInfos', () => __awaiter(void 0, void 0, void 0, function* () {
        const code = `
// C# Comment
#!test-erlang
% Erlang comment
`;
        // looks like a magic command followed by garbage
        const initialTokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(initialTokens).to.deep.equal([
            {
                tokenText: '// C# Comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '#!',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'test-erlang',
                tokenType: 'polyglot-notebook-keyword-control'
            }
        ]);
        // rebuild the grammar with an additional KernelInfo
        const newKernelInfo = {
            localName: 'test-erlang',
            uri: 'kernel://test-erlang',
            languageName: 'erlang',
            aliases: [],
            displayName: '',
            supportedKernelCommands: [],
            supportedDirectives: [],
        };
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, [newKernelInfo]);
        // looks like a proper token
        const realTokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(realTokens).to.deep.equal([
            {
                tokenText: '// C# Comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '% Erlang comment',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    }));
    it('tokens are parsed when kernel specifies a language alias instead of the root language name', () => __awaiter(void 0, void 0, void 0, function* () {
        const kernelInfoWithAlias = {
            localName: 'test-kernel-with-alias',
            uri: 'kernel://test-kernel-with-alias',
            languageName: 'see_sharp',
            aliases: [],
            displayName: '',
            supportedKernelCommands: [],
            supportedDirectives: []
        };
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, [kernelInfoWithAlias]);
        const code = `
#!${kernelInfoWithAlias.localName}
// C# comment from an alias
`;
        const tokens = yield getTokens(kernelInfoWithAlias.localName, code);
        (0, chai_1.expect)(tokens).to.deep.equal([
            {
                tokenText: '// C# comment from an alias',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    }));
    it('tokens are not parsed when no language name is specified in KernelInfo', () => __awaiter(void 0, void 0, void 0, function* () {
        const updatedKernelInfos = [
            {
                localName: 'test-perl',
                uri: 'kernel://test-perl',
                languageName: undefined,
                aliases: [],
                displayName: '',
                supportedKernelCommands: [],
                supportedDirectives: [],
            },
            ...defaultKernelInfos
        ];
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, updatedKernelInfos);
        const code = `
#!test-perl
$x = "this is perl code";
# Perl comment`;
        const tokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(tokens).to.deep.equal([]); // should be empty, but this gives better error messages
    }));
    it('tokens are not parsed when language name in KernelInfo does not match any known language', () => __awaiter(void 0, void 0, void 0, function* () {
        const updatedKernelInfos = [
            {
                localName: 'test-perl',
                uri: 'kernel://test-perl',
                languageName: 'not-perl',
                aliases: [],
                displayName: '',
                supportedKernelCommands: [],
                supportedDirectives: [],
            },
            ...defaultKernelInfos
        ];
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, updatedKernelInfos);
        const code = `
#!test-perl
$x = "this is perl code";
# Perl comment`;
        const tokens = yield getTokens('test-csharp', code);
        (0, chai_1.expect)(tokens).to.deep.equal([]); // should be empty, but this gives better error messages
    }));
    ////////////////////////////////////////////////////////////////////////////
    //                                                               setup stuff
    ////////////////////////////////////////////////////////////////////////////
    beforeEach(() => __awaiter(void 0, void 0, void 0, function* () {
        logMessages = [];
        const packageJSON = require(path.join(__dirname, '..', '..', '..', 'package.json'));
        // this mimics other extensions' data and grammar specifications
        // TODO: dump and rehydrate a real extension list?  seems excessive
        const testExtensionGrammars = [
            {
                id: 'test-extension.csharp',
                extensionPath: '',
                packageJSON: {
                    contributes: {
                        grammars: [
                            {
                                language: 'csharp',
                                scopeName: 'source.cs',
                                path: 'csharp.tmGrammar.json'
                            }
                        ],
                        languages: [
                            {
                                id: 'csharp',
                                aliases: [
                                    'see_sharp'
                                ]
                            }
                        ]
                    }
                }
            },
            {
                id: 'test-extension.python',
                extensionPath: '',
                packageJSON: {
                    contributes: {
                        grammars: [
                            {
                                language: 'python',
                                scopeName: 'source.python',
                                path: 'python.tmGrammar.json'
                            }
                        ]
                        // no aliases
                    }
                }
            },
            // Erlang isn't in the default kernel list, but it's used later
            {
                id: 'test-extension.erlang',
                extensionPath: '',
                packageJSON: {
                    contributes: {
                        grammars: [
                            {
                                language: 'erlang',
                                scopeName: 'source.erlang',
                                path: 'erlang.tmGrammar.json'
                            }
                        ]
                        // no aliases
                    }
                }
            }
        ];
        grammarContentsByPath = new Map();
        grammarContentsByPath.set('csharp.tmGrammar.json', JSON.stringify({
            scopeName: 'source.cs',
            patterns: [
                {
                    name: 'comment.line.csharp',
                    match: '//.*'
                },
                {
                    name: 'keyword.hash.csharp',
                    match: '#.*'
                }
            ]
        }));
        grammarContentsByPath.set('erlang.tmGrammar.json', JSON.stringify({
            scopeName: 'source.erlang',
            patterns: [
                {
                    name: 'comment.line.erlang',
                    match: '%.*'
                }
            ]
        }));
        grammarContentsByPath.set('python.tmGrammar.json', JSON.stringify({
            scopeName: 'source.python',
            patterns: [
                {
                    name: 'comment.line.python',
                    match: '#.*'
                },
                {
                    name: 'keyword.div.python',
                    match: '//.*'
                }
            ]
        }));
        dynamicTokenProvider = new dynamicGrammarSemanticTokenProvider_1.DynamicGrammarSemanticTokenProvider(packageJSON, testExtensionGrammars, path => true, path => grammarContentsByPath.get(path));
        yield dynamicTokenProvider.init();
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, defaultKernelInfos);
    }));
    afterEach(() => {
        if (displayDiagnosticMessages) {
            (0, chai_1.expect)(logMessages).to.deep.equal([]);
        }
    });
}));
//# sourceMappingURL=dynamicGrammarSemanticTokenProvider.test.js.map
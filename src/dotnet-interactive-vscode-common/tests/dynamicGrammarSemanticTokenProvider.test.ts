// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../../src/vscode-common/dotnet-interactive/contracts';
import * as fs from 'fs';
import * as path from 'path';
import * as vscodeLike from '../../src/vscode-common/interfaces/vscode-like';
import { expect } from 'chai';
import { DynamicGrammarSemanticTokenProvider, VSCodeExtensionLike } from '../../src/vscode-common/dynamicGrammarSemanticTokenProvider';
import { Logger } from '../../src/vscode-common/dotnet-interactive';

describe('dynamic grammar tests', async () => {
    let logMessages: string[] = [];
    let grammarContentsByPath: Map<string, string>;
    const testUri: vscodeLike.Uri = {
        fsPath: '',
        scheme: ''
    };
    let dynamicTokenProvider: DynamicGrammarSemanticTokenProvider;

    const defaultKernelInfos: contracts.KernelInfo[] = [
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

    Logger.configure('test-host', (entry) => {
        logMessages.push(entry.message);
    });

    async function getTokens(initialKernelName: string, code: string): Promise<{ tokenText: string, tokenType: string }[]> {
        const lines = code.split('\n');
        const tokens = await dynamicTokenProvider.getTokens(testUri, initialKernelName, code);
        return tokens.map(token => {
            return {
                tokenText: lines[token.line].substring(token.startColumn, token.endColumn),
                tokenType: token.tokenType
            };
        });
    }

    it('sub parsing within magic commands', async () => {
        const code = '#!some-magic-command --option1 value1 /option2 value2 argument1 "some string"';
        const tokens = await getTokens('test-csharp', code);
        expect(tokens).to.deep.equal([
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
    });

    it("magic commands don't invalidate the language selector", async () => {
        const code = `
// csharp comment 1
#!some-magic-command
// csharp comment 2
`;
        const tokens = await getTokens('test-csharp', code);
        expect(tokens).to.deep.equal([
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
    });

    it("tokens are classified according to each language's grammar", async () => {
        const code = `
// C# comment
# C# keyword

#!test-python

# Python comment
// Python keyword
`;
        const tokens = await getTokens('test-csharp', code);
        expect(tokens).to.deep.equal([
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
    });

    it('language switch can be specified with a kernel alias', async () => {
        const code = `
#!see-sharp-alias
// C# alias comment
`;
        // defaulting to `python` to prove that the kernel selector worked
        const tokens = await getTokens('test-python', code);
        expect(tokens).to.deep.equal([
            {
                tokenText: '// C# alias comment',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    });

    it('token parsing is updated when grammar is rebuilt with _ALL_ new KernelInfos', async () => {
        const code = `
// C# Comment
#!test-erlang
% Erlang comment
`;

        // looks like a magic command followed by garbage
        const initialTokens = await getTokens('test-csharp', code);
        expect(initialTokens).to.deep.equal([
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
        const updatedKernelInfos: contracts.KernelInfo[] = [
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
        const realTokens = await getTokens('test-csharp', code);
        expect(realTokens).to.deep.equal([
            {
                tokenText: '// C# Comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '% Erlang comment',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    });

    it('token parsing is updated when grammar is rebuilt with _SOME_ new KernelInfos', async () => {
        const code = `
// C# Comment
#!test-erlang
% Erlang comment
`;

        // looks like a magic command followed by garbage
        const initialTokens = await getTokens('test-csharp', code);
        expect(initialTokens).to.deep.equal([
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
        const newKernelInfo: contracts.KernelInfo = {
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
        const realTokens = await getTokens('test-csharp', code);
        expect(realTokens).to.deep.equal([
            {
                tokenText: '// C# Comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: '% Erlang comment',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    });

    it('tokens are parsed when kernel specifies a language alias instead of the root language name', async () => {
        const kernelInfoWithAlias: contracts.KernelInfo = {
            localName: 'test-kernel-with-alias',
            uri: 'kernel://test-kernel-with-alias',
            languageName: 'see_sharp', // this is an alias and not the real name "csharp"
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
        const tokens = await getTokens(kernelInfoWithAlias.localName, code);
        expect(tokens).to.deep.equal([
            {
                tokenText: '// C# comment from an alias',
                tokenType: 'polyglot-notebook-comment'
            }
        ]);
    });

    it('tokens are not parsed when no language name is specified in KernelInfo', async () => {
        const updatedKernelInfos: contracts.KernelInfo[] = [
            {
                localName: 'test-perl',
                uri: 'kernel://test-perl',
                languageName: undefined, // not specified; no grammar should be applied
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
        const tokens = await getTokens('test-csharp', code);
        expect(tokens).to.deep.equal([]); // should be empty, but this gives better error messages
    });

    it('tokens are not parsed when language name in KernelInfo does not match any known language', async () => {
        const updatedKernelInfos: contracts.KernelInfo[] = [
            {
                localName: 'test-perl',
                uri: 'kernel://test-perl',
                languageName: 'not-perl', // language name is specified, but doesn't match any known language
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
        const tokens = await getTokens('test-csharp', code);
        expect(tokens).to.deep.equal([]); // should be empty, but this gives better error messages
    });

    it('http grammar can classify code', async () => {
        const updatedKernelInfos: contracts.KernelInfo[] = [
            {
                localName: 'httpRequest',
                uri: 'kernel://httpRequest',
                languageName: 'http',
                aliases: [],
                displayName: '',
                supportedKernelCommands: [],
                supportedDirectives: [],
            }
        ];
        dynamicTokenProvider.rebuildNotebookGrammar(testUri, updatedKernelInfos);

        const code = `
// some comment
GET /{{subPath}}/page.html
User-Agent: abc/{{userAgent}}/def`;
        const tokens = await getTokens('httpRequest', code);
        expect(tokens).to.deep.equal([
            {
                tokenText: '// some comment',
                tokenType: 'polyglot-notebook-comment'
            },
            {
                tokenText: 'GET',
                tokenType: 'polyglot-notebook-keyword'
            },
            {
                tokenText: '{{',
                tokenType: 'polyglot-notebook-punctuation-section-brackets-single-begin'
            },
            {
                tokenText: 'subPath',
                tokenType: 'polyglot-notebook-variable-language'
            },
            {
                tokenText: '}}',
                tokenType: 'polyglot-notebook-punctuation-section-brackets-single-end'
            },
            {
                tokenText: 'User-Agent:',
                tokenType: 'polyglot-notebook-keyword-control'
            },
            {
                tokenText: '{{',
                tokenType: 'polyglot-notebook-punctuation-section-brackets-single-begin'
            },
            {
                tokenText: 'userAgent',
                tokenType: 'polyglot-notebook-variable-language'
            },
            {
                tokenText: '}}',
                tokenType: 'polyglot-notebook-punctuation-section-brackets-single-end'
            }
        ]);
    });

    ////////////////////////////////////////////////////////////////////////////
    //                                                               setup stuff
    ////////////////////////////////////////////////////////////////////////////

    beforeEach(async () => {
        logMessages = [];

        const packageJSON = require(path.join(__dirname, '..', '..', '..', 'package.json'));

        // this mimics other extensions' data and grammar specifications
        // TODO: dump and rehydrate a real extension list?  seems excessive
        const testExtensionGrammars: VSCodeExtensionLike[] = [
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
                                id: 'csharp', // same as grammars.language above
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

        function getGrammarContents(grammarPath: string): string {
            const grammarContents = grammarContentsByPath.get(grammarPath);
            if (grammarContents) {
                return grammarContents;
            }

            return fs.readFileSync(grammarPath, 'utf8');
        }

        dynamicTokenProvider = new DynamicGrammarSemanticTokenProvider(packageJSON, testExtensionGrammars, _path => true, path => getGrammarContents(path)!);
        await dynamicTokenProvider.init();

        dynamicTokenProvider.rebuildNotebookGrammar(testUri, defaultKernelInfos);
    });

    afterEach(() => {
        if (displayDiagnosticMessages) {
            expect(logMessages).to.deep.equal([]);
        }
    });
});

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import * as fs from 'fs';
import * as path from 'path';
import * as oniguruma from 'vscode-oniguruma';
import * as vsctm from 'vscode-textmate';

describe('TextMate grammar tests', async () => {
    before(async () => {
        // prepare grammar parser
        const nodeModulesDir = path.join(__dirname, '..', '..', '..', 'node_modules');
        const onigWasmPath = path.join(nodeModulesDir, 'vscode-oniguruma', 'release', 'onig.wasm');
        const wasmBin = fs.readFileSync(onigWasmPath).buffer;
        await oniguruma.loadWASM(wasmBin);
    });

    // prepare grammar scope loader
    const grammarDir = path.join(__dirname, '..', '..', '..', 'syntaxes');
    const registry = new vsctm.Registry({
        onigLib: Promise.resolve({
            createOnigScanner: (sources) => new oniguruma.OnigScanner(sources),
            createOnigString: (str) => new oniguruma.OnigString(str)
        }),
        loadGrammar: (scopeName) => {
            return new Promise<vsctm.IRawGrammar | null>((resolve, reject) => {
                const grammarFileName = `${scopeName}.tmGrammar.json`;
                const grammarFilePath = path.join(grammarDir, grammarFileName);
                let contents: string;
                if (!fs.existsSync(grammarFilePath)) {
                    // tests can't delegate to well-known languages because those grammars aren't in this repo, so we create a catch-all
                    const emptyGrammar = {
                        scopeName,
                        patterns: [
                            {
                                name: `language.line.${scopeName}`,
                                match: '^.*$'
                            }
                        ]
                    };
                    contents = JSON.stringify(emptyGrammar);
                } else {
                    const buffer = fs.readFileSync(grammarFilePath);
                    contents = buffer.toString('utf-8');
                }

                const grammar = vsctm.parseRawGrammar(contents, grammarFilePath);
                resolve(grammar);
            });
        }
    });

    async function getTokens(text: Array<string>, initialScope?: string): Promise<Array<Array<any>>> {
        const grammar = await registry.loadGrammar(initialScope ?? 'source.dotnet-interactive');
        let ruleStack = vsctm.INITIAL;
        let allTokens = [];
        for (let i = 0; i < text.length; i++) {
            let lineTokens = [];
            const line = text[i];
            const parsedLineTokens = grammar!.tokenizeLine(line, ruleStack);
            for (let j = 0; j < parsedLineTokens.tokens.length; j++) {
                const token = parsedLineTokens.tokens[j];
                const tokenText = line.substring(token.startIndex, token.endIndex);
                lineTokens.push({
                    tokenText,
                    scopes: token.scopes
                });
            }

            allTokens.push(lineTokens);
            ruleStack = parsedLineTokens.ruleStack;
        }

        return allTokens;
    }

    it('all supported language specifiers', async () => {
        const text = [
            '#!csharp',
            '#!cs',
            '#!fsharp',
            '#!fs',
            '#!html',
            '#!javascript',
            '#!js',
            '#!markdown',
            '#!md',
            '#!powershell',
            '#!pwsh'
        ];
        const tokens = await getTokens(text);
        expect(tokens).to.deep.equal([
            [
                {
                    tokenText: '#!csharp',
                    scopes: ['source.dotnet-interactive', 'language.switch.csharp']
                }
            ],
            [
                {
                    tokenText: '#!cs',
                    scopes: ['source.dotnet-interactive', 'language.switch.csharp']
                }
            ],
            [
                {
                    tokenText: '#!fsharp',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp']
                }
            ],
            [
                {
                    tokenText: '#!fs',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp']
                }
            ],
            [
                {
                    tokenText: '#!html',
                    scopes: ['source.dotnet-interactive', 'language.switch.html']
                }
            ],
            [
                {
                    tokenText: '#!javascript',
                    scopes: ['source.dotnet-interactive', 'language.switch.javascript']
                }
            ],
            [
                {
                    tokenText: '#!js',
                    scopes: ['source.dotnet-interactive', 'language.switch.javascript']
                }
            ],
            [
                {
                    tokenText: '#!markdown',
                    scopes: ['source.dotnet-interactive', 'language.switch.markdown']
                }
            ],
            [
                {
                    tokenText: '#!md',
                    scopes: ['source.dotnet-interactive', 'language.switch.markdown']
                }
            ],
            [
                {
                    tokenText: '#!powershell',
                    scopes: ['source.dotnet-interactive', 'language.switch.powershell']
                }
            ],
            [
                {
                    tokenText: '#!pwsh',
                    scopes: ['source.dotnet-interactive', 'language.switch.powershell']
                }
            ]
        ]);
    });

    it("magic command doesn't invalidate language", async () => {
        const text = [
            '#!fsharp',
            '// this is fsharp',
            '#!some-magic-command',
            '// this is still fsharp'
        ];
        const tokens = await getTokens(text);
        expect(tokens).to.deep.equal([
            [
                {
                    tokenText: '#!fsharp',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp']
                }
            ],
            [
                {
                    tokenText: '// this is fsharp',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp', 'language.line.source.fsharp']
                }
            ],
            [
                {
                    tokenText: '#!',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp', 'comment.line.magic-commands', 'comment.line.magic-commands.hash-bang']
                },
                {
                    tokenText: 'some-magic-command',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp', 'comment.line.magic-commands', 'keyword.control.magic-commands']
                }
            ],
            [
                {
                    tokenText: '// this is still fsharp',
                    scopes: ['source.dotnet-interactive', 'language.switch.fsharp', 'language.line.source.fsharp']
                }
            ]
        ]);
    });

    const allLanguages = [
        'csharp',
        'fsharp',
        'html',
        'javascript',
        'markdown',
        'powershell'
    ];

    for (const language of allLanguages) {
        it(`language ${language} can switch to all other languages`, async () => {
            let text = [`#!${language}`];
            let expected = [
                [
                    {
                        tokenText: `#!${language}`,
                        scopes: ['source.dotnet-interactive', `language.switch.${language}`]
                    }
                ]
            ];
            for (const otherLanguage of allLanguages) {
                text.push(`#!${otherLanguage}`);
                expected.push([
                    {
                        tokenText: `#!${otherLanguage}`,
                        scopes: ['source.dotnet-interactive', `language.switch.${otherLanguage}`]
                    }
                ]);
            }

            const tokens = await getTokens(text);
            expect(tokens).to.deep.equal(expected);
        });
    }

    it('sub-parsing within magic commands', async () => {
        const text = ['#!share --from csharp x "some string" /a b'];
        const tokens = await getTokens(text, 'source.dotnet-interactive.magic-commands');
        expect(tokens).to.deep.equal([
            [
                {
                    tokenText: '#!',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'comment.line.magic-commands.hash-bang']
                },
                {
                    tokenText: 'share',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'keyword.control.magic-commands']
                },
                {
                    tokenText: ' ',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands']
                },
                {
                    tokenText: '--from',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'constant.language.magic-commands']
                },
                {
                    tokenText: ' ',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands']
                },
                {
                    tokenText: 'csharp',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'variable.parameter.magic-commands']
                },
                {
                    tokenText: ' ',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands']
                },
                {
                    tokenText: 'x',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'variable.parameter.magic-commands']
                },
                {
                    tokenText: ' ',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands']
                },
                {
                    tokenText: '"',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'string.quoted.double.magic-commands']
                },
                {
                    tokenText: 'some string',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'string.quoted.double.magic-commands']
                },
                {
                    tokenText: '"',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'string.quoted.double.magic-commands']
                },
                {
                    tokenText: ' ',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands']
                },
                {
                    tokenText: '/a',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'constant.language.magic-commands']
                },
                {
                    tokenText: ' ',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands']
                },
                {
                    tokenText: 'b',
                    scopes: ['source.dotnet-interactive.magic-commands', 'comment.line.magic-commands', 'variable.parameter.magic-commands']
                }
            ]
        ]);
    });
});

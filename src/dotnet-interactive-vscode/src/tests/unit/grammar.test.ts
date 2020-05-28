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
                if (!fs.existsSync(grammarFilePath)) {
                    resolve(null);
                    return;
                }

                fs.readFile(grammarFilePath, (_err, data) => {
                    const contents = data.toString('utf-8');
                    const grammar = vsctm.parseRawGrammar(contents, grammarFilePath);
                    resolve(grammar);
                });
                
            });
        }
    });

    async function getTokens(text: Array<string>): Promise<Array<Array<any>>> {
        const grammar = await registry.loadGrammar('source.dotnet-interactive');
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
                    scopes: ['source.dotnet-interactive', 'language.csharp']
                }
            ],
            [
                {
                    tokenText: '#!cs',
                    scopes: ['source.dotnet-interactive', 'language.csharp']
                }
            ],
            [
                {
                    tokenText: '#!fsharp',
                    scopes: ['source.dotnet-interactive', 'language.fsharp']
                }
            ],
            [
                {
                    tokenText: '#!fs',
                    scopes: ['source.dotnet-interactive', 'language.fsharp']
                }
            ],
            [
                {
                    tokenText: '#!html',
                    scopes: ['source.dotnet-interactive', 'language.html']
                }
            ],
            [
                {
                    tokenText: '#!javascript',
                    scopes: ['source.dotnet-interactive', 'language.javascript']
                }
            ],
            [
                {
                    tokenText: '#!js',
                    scopes: ['source.dotnet-interactive', 'language.javascript']
                }
            ],
            [
                {
                    tokenText: '#!markdown',
                    scopes: ['source.dotnet-interactive', 'language.markdown']
                }
            ],
            [
                {
                    tokenText: '#!md',
                    scopes: ['source.dotnet-interactive', 'language.markdown']
                }
            ],
            [
                {
                    tokenText: '#!powershell',
                    scopes: ['source.dotnet-interactive', 'language.powershell']
                }
            ],
            [
                {
                    tokenText: '#!pwsh',
                    scopes: ['source.dotnet-interactive', 'language.powershell']
                }
            ]
        ]);
    });

    it('magic command doesnt invalidate language', async () => {
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
                    scopes: ['source.dotnet-interactive', 'language.fsharp']
                }
            ],
            [
                {
                    tokenText: '// this is fsharp',
                    scopes: ['source.dotnet-interactive', 'language.fsharp']
                }
            ],
            [
                {
                    tokenText: '#!some-magic-command',
                    scopes: ['source.dotnet-interactive', 'language.fsharp', 'comment.magic-command']
                }
            ],
            [
                {
                    tokenText: '// this is still fsharp',
                    scopes: ['source.dotnet-interactive', 'language.fsharp']
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
                        scopes: ['source.dotnet-interactive', `language.${language}`]
                    }
                ]
            ];
            for (const otherLanguage of allLanguages) {
                text.push(`#!${otherLanguage}`);
                expected.push([
                    {
                        tokenText: `#!${otherLanguage}`,
                        scopes: ['source.dotnet-interactive', `language.${otherLanguage}`]
                    }
                ]);
            }

            const tokens = await getTokens(text);
            expect(tokens).to.deep.equal(expected);
        });
    }
});

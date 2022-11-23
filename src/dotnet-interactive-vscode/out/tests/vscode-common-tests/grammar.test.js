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
const chai_1 = require("chai");
const fs = require("fs");
const path = require("path");
const oniguruma = require("vscode-oniguruma");
const vsctm = require("vscode-textmate");
describe('TextMate grammar tests', () => __awaiter(void 0, void 0, void 0, function* () {
    before(() => __awaiter(void 0, void 0, void 0, function* () {
        // prepare grammar parser
        const nodeModulesDir = path.join(__dirname, '..', '..', '..', 'node_modules');
        const onigWasmPath = path.join(nodeModulesDir, 'vscode-oniguruma', 'release', 'onig.wasm');
        const wasmBin = fs.readFileSync(onigWasmPath).buffer;
        yield oniguruma.loadWASM(wasmBin);
    }));
    // prepare grammar scope loader
    const grammarDir = path.join(__dirname, '..', '..', '..', 'syntaxes');
    const registry = new vsctm.Registry({
        onigLib: Promise.resolve({
            createOnigScanner: (sources) => new oniguruma.OnigScanner(sources),
            createOnigString: (str) => new oniguruma.OnigString(str)
        }),
        loadGrammar: (scopeName) => {
            return new Promise((resolve, reject) => {
                const grammarFileName = `${scopeName}.tmGrammar.json`;
                const grammarFilePath = path.join(grammarDir, grammarFileName);
                let contents;
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
                }
                else {
                    const buffer = fs.readFileSync(grammarFilePath);
                    contents = buffer.toString('utf-8');
                }
                const grammar = vsctm.parseRawGrammar(contents, grammarFilePath);
                resolve(grammar);
            });
        }
    });
    function getTokens(text, initialScope) {
        return __awaiter(this, void 0, void 0, function* () {
            const grammar = yield registry.loadGrammar(initialScope !== null && initialScope !== void 0 ? initialScope : 'source.dotnet-interactive');
            let ruleStack = vsctm.INITIAL;
            let allTokens = [];
            for (let i = 0; i < text.length; i++) {
                let lineTokens = [];
                const line = text[i];
                const parsedLineTokens = grammar.tokenizeLine(line, ruleStack);
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
        });
    }
    it('all supported language specifiers', () => __awaiter(void 0, void 0, void 0, function* () {
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
            '#!mermaid',
            '#!powershell',
            '#!pwsh',
            '#!sql',
            '#!sql-adventureworks',
            '#!kql',
            '#!kql-default',
        ];
        const tokens = yield getTokens(text);
        (0, chai_1.expect)(tokens).to.deep.equal([
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
                    tokenText: '#!mermaid',
                    scopes: ['source.dotnet-interactive', 'language.switch.mermaid']
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
            ],
            [
                {
                    tokenText: '#!sql',
                    scopes: ['source.dotnet-interactive', 'language.switch.sql']
                }
            ],
            [
                {
                    tokenText: '#!sql-adventureworks',
                    scopes: ['source.dotnet-interactive', 'language.switch.sql']
                }
            ],
            [
                {
                    tokenText: '#!kql',
                    scopes: ['source.dotnet-interactive', 'language.switch.kql']
                }
            ],
            [
                {
                    tokenText: '#!kql-default',
                    scopes: ['source.dotnet-interactive', 'language.switch.kql']
                }
            ],
        ]);
    }));
    it("magic command doesn't invalidate language", () => __awaiter(void 0, void 0, void 0, function* () {
        const text = [
            '#!fsharp',
            '// this is fsharp',
            '#!some-magic-command',
            '// this is still fsharp'
        ];
        const tokens = yield getTokens(text);
        (0, chai_1.expect)(tokens).to.deep.equal([
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
    }));
    const allLanguages = [
        ['csharp', 'csharp'],
        ['fsharp', 'fsharp'],
        ['html', 'html'],
        ['javascript', 'javascript'],
        ['markdown', 'markdown'],
        ['mermaid', 'mermaid'],
        ['powershell', 'powershell'],
        ['sql', 'sql'],
        ['sql-adventureworks', 'sql'],
        ['kql', 'kql'],
        ['kql-default', 'kql'],
    ];
    for (const [magicCommand, language] of allLanguages) {
        it(`language ${language} can switch to all other languages`, () => __awaiter(void 0, void 0, void 0, function* () {
            let text = [`#!${magicCommand}`];
            let expected = [
                [
                    {
                        tokenText: `#!${magicCommand}`,
                        scopes: ['source.dotnet-interactive', `language.switch.${language}`]
                    }
                ]
            ];
            for (const [otherMagicCommand, otherLanguage] of allLanguages) {
                text.push(`#!${otherMagicCommand}`);
                expected.push([
                    {
                        tokenText: `#!${otherMagicCommand}`,
                        scopes: ['source.dotnet-interactive', `language.switch.${otherLanguage}`]
                    }
                ]);
            }
            const tokens = yield getTokens(text);
            (0, chai_1.expect)(tokens).to.deep.equal(expected);
        }));
    }
    it('sub-parsing within magic commands', () => __awaiter(void 0, void 0, void 0, function* () {
        const text = ['#!share --from csharp x "some string" /a b'];
        const tokens = yield getTokens(text, 'source.dotnet-interactive.magic-commands');
        (0, chai_1.expect)(tokens).to.deep.equal([
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
    }));
}));
//# sourceMappingURL=grammar.test.js.map
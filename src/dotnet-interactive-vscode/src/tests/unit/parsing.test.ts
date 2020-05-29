// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { parseNotebook, serializeNotebook, NotebookFile, languageToCellKind } from '../../interactiveNotebook';
import { CellKind } from '../../interfaces/vscode';

describe('Extension Test Suite', () => {
    it('Parse notebook from valid contents', () => {
        let validContents = `
#!fsharp
let x = 1
x + 2

#!csharp
var x = 3;
x + 4

#!markdown
This is \`markdown\`.
`;
        let notebook = parseNotebook(validContents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.fsharp',
                    contents: [
                        'let x = 1',
                        'x + 2'
                    ]
                },
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        'var x = 3;',
                        'x + 4'
                    ]
                },
                {
                    language: 'dotnet-interactive.markdown',
                    contents: [
                        'This is `markdown`.'
                    ]
                }
            ]
        });
    });

    it('empty language cells are removed when parsing', () => {
        let contents = `
#!csharp
//

#!fsharp

#!powershell
Get-Item

#!fsharp
`;
        let notebook = parseNotebook(contents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        '//'
                    ]
                },
                {
                    language: 'dotnet-interactive.powershell',
                    contents: [
                        'Get-Item'
                    ]
                },
            ]
        });
    });

    it('lots of blank lines are removed between cells', () => {
        let contents = `


#!csharp
// first line of C#



// last line of C#





#!fsharp

// first line of F#



// last line of F#



`;
        let notebook = parseNotebook(contents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        '// first line of C#',
                        '',
                        '',
                        '',
                        '// last line of C#'
                    ]
                },
                {
                    language: 'dotnet-interactive.fsharp',
                    contents: [
                        '// first line of F#',
                        '',
                        '',
                        '',
                        '// last line of F#'
                    ]
                }
            ]
        });
    });

    it('Parse notebook without language specifier', () => {
        let contents = `
var x = 1234;

#!fsharp
let x = 1
`;
        let notebook = parseNotebook(contents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        'var x = 1234;'
                    ]
                },
                {
                    language: 'dotnet-interactive.fsharp',
                    contents: [
                        'let x = 1'
                    ]
                }
            ]
        });
    });

    it('unrecognized language specifier is treated like a magic command in the current cell', () => {
        let contents = `
#!csharp
#!probably-a-magic-command
var x = 1;
`;
        let notebook = parseNotebook(contents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        '#!probably-a-magic-command',
                        'var x = 1;'
                    ]
                }
            ]
        });
    });

    it('unrecognized language specifier at top of file is treated like a magic command in the first cell', () => {
        let contents = `
#!probably-a-magic-command
var x = 1;
`;
        let notebook = parseNotebook(contents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        '#!probably-a-magic-command',
                        'var x = 1;'
                    ]
                }
            ]
        });
    });

    it('empty file parses as a single empty cell', () => {
        let notebook = parseNotebook('');
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: []
                }
            ]
        });
    });

    it('language aliases are expanded on open', () => {
        let contents = `
#!cs
// this is C#

#!fs
// this is F#

#!js
alert('javascript');

#!md
This is \`markdown\`.

#!pwsh
# this is PowerShell
`;
        let notebook = parseNotebook(contents);
        expect(notebook).to.deep.equal({
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        '// this is C#'
                    ]
                },
                {
                    language: 'dotnet-interactive.fsharp',
                    contents: [
                        '// this is F#'
                    ]
                },
                {
                    language: 'dotnet-interactive.javascript',
                    contents: [
                        'alert(\'javascript\');'
                    ]
                },
                {
                    language: 'dotnet-interactive.markdown',
                    contents: [
                        'This is `markdown`.'
                    ]
                },
                {
                    language: 'dotnet-interactive.powershell',
                    contents: [
                        '# this is PowerShell'
                    ]
                }
            ]
        });
    });

    for (let lineSeparator of ['\n', '\r\n']) {
        it (`can read file with line separator: ${JSON.stringify(lineSeparator)}`, () => {
            let lines = [
                '#!csharp',
                '1+1',
                '',
                '#!fsharp',
                '[1;2;3;4]',
                '|> List.sum',
            ];
            let content = lines.join(lineSeparator);
            let notebook = parseNotebook(content);
            expect(notebook).to.deep.equal({
                cells: [
                    {
                        language: 'dotnet-interactive.csharp',
                        contents: [
                            '1+1',
                        ]
                    },
                    {
                        language: 'dotnet-interactive.fsharp',
                        contents: [
                            '[1;2;3;4]',
                            '|> List.sum'
                        ]
                    }
                ]
            });
        });
    }

    it('round trip notebook file', () => {
        let notebook: NotebookFile = {
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        'var x = 1234;'
                    ]
                },
                {
                    language: 'dotnet-interactive.fsharp',
                    contents: [
                        '[1;2;3;4]',
                        '|> List.sum'
                    ]
                }
            ]
        };
        let asText = serializeNotebook(notebook);
        let reHydrated = parseNotebook(asText);
        expect(notebook).to.deep.equal(reHydrated);
    });

    it('extra blank lines are removed from beginning and end on save', () => {
        let notebook: NotebookFile = {
            cells: [
                {
                    language: 'dotnet-interactive.csharp',
                    contents: [
                        '',
                        '',
                        '',
                        '',
                        '// this is csharp',
                        '',
                    ]
                }
            ]
        };
        let str = serializeNotebook(notebook);
        let expectedLines = [
            '#!csharp',
            '',
            '// this is csharp',
            '',
        ];
        let expected = expectedLines.join('\r\n');
        expect(str).to.equal(expected);
    });

    it('code cells are appropriately classified by language', () => {
        const codeLanguages = [
            'dotnet-interactive.csharp',
            'dotnet-interactive.fsharp',
            'dotnet-interactive.html',
            'dotnet-interactive.javascript',
            'dotnet-interactive.powershell'
        ];
        for (let language of codeLanguages) {
            expect(languageToCellKind(language)).to.equal(CellKind.Code);
        }
    });

    it('markdown cells are appropriately classified', () => {
        const markdownLanguages = [
            'dotnet-interactive.markdown'
        ];
        for (let language of markdownLanguages) {
            expect(languageToCellKind(language)).to.equal(CellKind.Markdown);
        }
    });
});

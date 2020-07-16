// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { parseNotebook, serializeNotebook, languageToCellKind } from '../../interactiveNotebook';
import { CellKind, NotebookDocument } from '../../interfaces/vscode';
import { createUri } from '../../utilities';
import { parseAsInteractiveNotebook, serializeAsInteractiveNotebook } from '../../fileFormats/interactive';

describe('dib read/write tests', () => {

    //Throughout this file, the `cell.document.getText()` function causes problems
    // with `.deep.equal()`, so we have to pull out the relevant parts.

    //-------------------------------------------------------------------------
    //                                                            parsing tests
    //-------------------------------------------------------------------------

    it('top-level code without a language specifier is assigned a default language', () => {
        const contents = 'var x = 1';
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells[0].cellKind).to.equal(CellKind.Code);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.csharp');
        expect(notebook.cells[0].document.getText()).to.equal('var x = 1');
    });

    it('parsed cells can specify their language without retaining the language specifier', () => {
        const contents = `
#!fsharp
let x = 1`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells[0].cellKind).to.equal(CellKind.Code);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.fsharp');
        expect(notebook.cells[0].document.getText()).to.equal('let x = 1');
    });

    it('parsed cells without a language specifier retain magic commands and the default language', () => {
        const contents = `
#!probably-a-magic-command
var x = 1;`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells[0].cellKind).to.equal(CellKind.Code);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.csharp');
        expect(notebook.cells[0].document.getText()).to.equal('#!probably-a-magic-command\nvar x = 1;');
    });

    it('parsed cells with a language specifier retain magic commands', () => {
        const contents = `
#!fsharp
#!probably-a-magic-command
let x = 1`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells[0].cellKind).to.equal(CellKind.Code);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.fsharp');
        expect(notebook.cells[0].document.getText()).to.equal('#!probably-a-magic-command\nlet x = 1');
    });

    it('multiple cells can be parsed', () => {
        const contents = `
#!csharp
var x = 1;
var y = 2;

#!fsharp
let x = 1
let y = 2`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells.map(c => { return { kind: c.cellKind, language: c.language, code: c.document.getText() }; })).to.deep.equal([
            {
                kind: CellKind.Code,
                language: 'dotnet-interactive.csharp',
                code: 'var x = 1;\nvar y = 2;'
            },
            {
                kind: CellKind.Code,
                language: 'dotnet-interactive.fsharp',
                code: 'let x = 1\nlet y = 2'
            }
        ]);
    });

    it('empty language cells are removed when parsing', () => {
        const contents = `
#!csharp
//

#!fsharp

#!powershell
Get-Item

#!fsharp
`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells.map(c => { return { kind: c.cellKind, language: c.language, code: c.document.getText() }; })).to.deep.equal([
            {
                kind: CellKind.Code,
                language: 'dotnet-interactive.csharp',
                code: '//'
            },
            {
                kind: CellKind.Code,
                language: 'dotnet-interactive.powershell',
                code: 'Get-Item'
            }
        ]);
    });

    it('empty lines are removed between cells', () => {
        const contents = `


#!csharp
// first line of C#



// last line of C#





#!fsharp

// first line of F#



// last line of F#



`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells.map(c => c.document.getText())).to.deep.equal([
            '// first line of C#\n\n\n\n// last line of C#',
            '// first line of F#\n\n\n\n// last line of F#'
        ]);
    });

    it('empty file parses as a single empty cell', () => {
        const contents = '';
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells.map(c => { return { language: c.language, code: c.document.getText() }; })).to.deep.equal([
            {
                language: 'dotnet-interactive.csharp',
                code: ''
            }
        ]);
    });

    it('language aliases are expanded when parsed', () => {
        const contents = `
#!cs
// this is C#

#!fs
// this is F#

#!js
alert('javascript');

#!md
This is \`markdown\`.

#!pwsh
# this is PowerShell`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells.map(c => c.language)).to.deep.equal([
            'dotnet-interactive.csharp',
            'dotnet-interactive.fsharp',
            'dotnet-interactive.javascript',
            'dotnet-interactive.markdown',
            'dotnet-interactive.powershell',
        ]);
    });

    for (const lineSeparator of ['\n', '\r\n']) {
        it(`can be parsed with '${JSON.stringify(lineSeparator)}' line separator`, () => {
            const lines = [
                '#!csharp',
                '1+1',
                '',
                '#!fsharp',
                '[1;2;3;4]',
                '|> List.sum'
            ];
            const contents = lines.join(lineSeparator);
            const notebook = parseAsInteractiveNotebook(contents);
            expect(notebook.cells.map(c => c.document.getText())).to.deep.equal([
                '1+1',
                '[1;2;3;4]\n|> List.sum'
            ]);
        });
    }

    it('parsed document never specifies outputs', () => {
        const contents = `
#!csharp
var x = 1;

#!fsharp
let x = 1
`;
        const notebook = parseAsInteractiveNotebook(contents);
        expect(notebook.cells.map(c => c.outputs)).to.deep.equal([
            [],
            []
        ]);
    });

    //-------------------------------------------------------------------------
    //                                                      serialization tests
    //-------------------------------------------------------------------------

    it('multiple cells are serialized with appropriate separators', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => '// C# line 1\n// C# line 2'
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: []
                },
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => '// F# line 1\n// F# line 2'
                    },
                    language: 'dotnet-interactive.fsharp',
                    outputs: []
                },
                {
                    cellKind: CellKind.Markdown,
                    document: {
                        uri: createUri('unused'),
                        getText: () => 'This is `markdown`.'
                    },
                    language: 'dotnet-interactive.markdown',
                    outputs: []
                }
            ]
        };
        const serialized = serializeAsInteractiveNotebook(notebook);
        const expected = [
            '#!csharp',
            '',
            '// C# line 1',
            '// C# line 2',
            '',
            '#!fsharp',
            '',
            '// F# line 1',
            '// F# line 2',
            '',
            '#!markdown',
            '',
            'This is `markdown`.',
            '',
        ].join('\r\n');
        expect(serialized).to.equal(expected);
    });

    it('extra blank lines are removed from beginning and end on save', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => '\n\n\n\n// this is csharp\n'
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: []
                }
            ]
        };
        const str = serializeAsInteractiveNotebook(notebook);
        const expectedLines = [
            '#!csharp',
            '',
            '// this is csharp',
            '',
        ];
        const expected = expectedLines.join('\r\n');
        expect(str).to.equal(expected);
    });

    //-------------------------------------------------------------------------
    //                                                extension selection tests
    //-------------------------------------------------------------------------

    for (const extension of ['dib', 'dotnet-interactive']) {
        it(`parse notebook from extension '.${extension}'`, () => {
            const fileContents = `
#!csharp
var x = 1;
`;

            const notebook = parseNotebook(createUri(`notebook.${extension}`), fileContents);
            expect(notebook.cells.map(c => { return { cellKind: c.cellKind, code: c.document.getText(), language: c.language }; })).to.deep.equal([
                {
                    cellKind: CellKind.Code,
                    code: 'var x = 1;',
                    language: 'dotnet-interactive.csharp'
                }
            ]);
        });

        it(`serialize notebook to extension '.${extension}'`, () => {
            const notebook: NotebookDocument = {
                cells: [
                    {
                        cellKind: CellKind.Code,
                        document: {
                            uri: createUri('unused'),
                            getText: () => '// this is csharp'
                        },
                        language: 'dotnet-interactive.csharp',
                        outputs: []
                    }
                ]
            };
            const serialized = serializeNotebook(createUri(`notebook.${extension}`), notebook);
            const expected = [
                '#!csharp',
                '',
                '// this is csharp',
                ''
            ].join('\r\n');
            expect(serialized).to.equal(expected);
        });
    }

    //-------------------------------------------------------------------------
    //                                                               misc tests
    //-------------------------------------------------------------------------

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

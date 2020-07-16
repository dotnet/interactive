// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { parseAsJupyterNotebook, serializeAsJupyterNotebook } from '../../fileFormats/jupyter';
import { CellKind, CellOutputKind, NotebookDocument } from '../../interfaces/vscode';
import { JupyterNotebook } from '../../interfaces/jupyter';
import { createUri } from '../../utilities';
import { serializeNotebook, parseNotebook } from '../../interactiveNotebook';

describe('jupyter read/write tests', () => {

    //-------------------------------------------------------------------------
    //                                                            parsing tests
    //-------------------------------------------------------------------------

    it('notebook metadata default language is honored in cells without language specifier set: C#', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: '// this should be C#'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.csharp');
    });

    it('notebook metadata default language is honored in cells without language specifier set: F#', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: '// this should be F#'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (F#)',
                    language: 'F#',
                    name: '.net-fsharp'
                },
                language_info: {
                    file_extension: '.fs',
                    mimetype: 'text/x-fsharp',
                    name: 'F#',
                    pygments_lexer: 'fsharp',
                    version: '5.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.fsharp');
    });

    it("parsed cells don't retain redundant language specifier", () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: '#!csharp\n// this should be C#'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.csharp');
        expect(notebook.cells[0].document.getText()).to.equal('// this should be C#');
    });

    it('parsed cells can override default language with language specifier', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: '#!fsharp\n// this should be F#'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.fsharp');
        expect(notebook.cells[0].document.getText()).to.equal('// this should be F#');
    });

    it('parsed cells can contain polyglot blobs with appropriate default language', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: '// this is C#\n#!fsharp\n// and now it is another language (F#)'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.csharp');
        expect(notebook.cells[0].document.getText()).to.equal('// this is C#\n#!fsharp\n// and now it is another language (F#)');
    });

    it('parsed cells treat non-languge specifier first lines as magic commands', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: '#!probably-a-magic-command\n// but this is C#'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].language).to.equal('dotnet-interactive.csharp');
        expect(notebook.cells[0].document.getText()).to.equal('#!probably-a-magic-command\n// but this is C#');
    });

    it('parsed cells can specify source as a single string', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: 'line 1\nline 2\r\nline 3\n'
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].document.getText()).to.equal('line 1\nline 2\nline 3\n');
    });

    it('parsed cells can specify source as a string array', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        'line 1\n',
                        'line 2\r\n',
                        'line 3\n'
                    ]
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseAsJupyterNotebook(serialized);
        expect(notebook.cells[0].document.getText()).to.equal('line 1\nline 2\nline 3');
    });

    //-------------------------------------------------------------------------
    //                                                      serialization tests
    //-------------------------------------------------------------------------

    it('serialized notebook has appropriate metadata', () => {
        const notebook: NotebookDocument = {
            cells: []
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter: JupyterNotebook = JSON.parse(serialized);
        expect(jupyter.metadata).to.deep.equal({
            kernelspec: {
                display_name: '.NET (C#)',
                language: 'C#',
                name: '.net-csharp'
            },
            language_info: {
                file_extension: '.cs',
                mimetype: 'text/x-csharp',
                name: 'C#',
                pygments_lexer: 'csharp',
                version: '8.0'
            }
        });
        expect(jupyter.nbformat).to.equal(4);
        expect(jupyter.nbformat_minor).to.equal(4);
    });

    it("code cells with default jupyter kernel language don't have language specifier", () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => 'var x = 1;'
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: []
                }
            ]
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter: JupyterNotebook = JSON.parse(serialized);
        expect(jupyter.cells[0].source).to.deep.equal([
            'var x = 1;'
        ]);
    });

    it('code cells with non-default jupyter kernel language have language specifier', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => 'let x = 1'
                    },
                    language: 'dotnet-interactive.fsharp',
                    outputs: []
                }
            ]
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter: JupyterNotebook = JSON.parse(serialized);
        expect(jupyter.cells[0].source).to.deep.equal([
            '#!fsharp\r\n',
            'let x = 1'
        ]);
    });

    it('code cells with multi-line text are serialized as an array', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => 'var x = 1;\nvar y = 2;'
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: []
                }
            ]
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter: JupyterNotebook = JSON.parse(serialized);
        expect(jupyter.cells[0].source).to.deep.equal([
            'var x = 1;\r\n',
            'var y = 2;'
        ]);
    });

    it('markdown cells are serialized appropriately', () => {
        const notebook: NotebookDocument = {
            cells: [
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
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter: JupyterNotebook = JSON.parse(serialized);
        expect(jupyter.cells[0]).to.deep.equal({
            cell_type: 'markdown',
            metadata: {},
            source: 'This is `markdown`.'
        });
    });

    it('text cell outputs are serialized', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => ''
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: [
                        {
                            outputKind: CellOutputKind.Text,
                            text: 'this is text'
                        }
                    ]
                }
            ]
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter = JSON.parse(serialized);
        expect(jupyter.cells[0].outputs).to.deep.equal([
            {
                output_type: 'display_data',
                data: {
                    'text/plain': [
                        'this is text'
                    ]
                },
                metadata: {}
            }
        ]);
    });

    it('rich cell outputs are serialized', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => ''
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: [
                        {
                            outputKind: CellOutputKind.Rich,
                            data: {
                                'text/html': 'this is html'
                            }
                        }
                    ]
                }
            ]
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter = JSON.parse(serialized);
        expect(jupyter.cells[0].outputs).to.deep.equal([
            {
                output_type: 'execute_result',
                data: {
                    'text/html': [
                        'this is html'
                    ]
                },
                execution_count: 1,
                metadata: {}
            }
        ]);
    });

    it('error cell outputs are serialized', () => {
        const notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    document: {
                        uri: createUri('unused'),
                        getText: () => ''
                    },
                    language: 'dotnet-interactive.csharp',
                    outputs: [
                        {
                            outputKind: CellOutputKind.Error,
                            ename: 'e-name',
                            evalue: 'e-value',
                            traceback: []
                        }
                    ]
                }
            ]
        };
        const serialized = serializeAsJupyterNotebook(notebook);
        const jupyter = JSON.parse(serialized);
        expect(jupyter.cells[0].outputs).to.deep.equal([
            {
                output_type: 'error',
                ename: 'e-name',
                evalue: 'e-value',
                traceback: []
            }
        ]);
    });

    //-------------------------------------------------------------------------
    //                                                extension selection tests
    //-------------------------------------------------------------------------

    it('parse jupyter notebook based on file extension', () => {
        const jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        '#!csharp',
                        'var x = 1;'
                    ]
                },
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        '#!fsharp',
                        'let x = 1'
                    ]
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const serialized = JSON.stringify(jupyter);
        const notebook = parseNotebook(createUri('notebook.ipynb'), serialized);
        const cellText = notebook.cells.map(c => c.document.getText());
        expect(cellText).to.deep.equal([
            'var x = 1;',
            'let x = 1'
        ]);
    });

    it('serialize jupyter notebook based on file extension', () => {
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
        const serialized = serializeNotebook(createUri('notebook.ipynb'), notebook);
        const expected: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    outputs: [],
                    source: [
                        '// this is csharp'
                    ]
                }
            ],
            metadata: {
                kernelspec: {
                    display_name: '.NET (C#)',
                    language: 'C#',
                    name: '.net-csharp'
                },
                language_info: {
                    file_extension: '.cs',
                    mimetype: 'text/x-csharp',
                    name: 'C#',
                    pygments_lexer: 'csharp',
                    version: '8.0'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        };
        const reParsed = JSON.parse(serialized);
        expect(reParsed).to.deep.equal(expected);
    });
});

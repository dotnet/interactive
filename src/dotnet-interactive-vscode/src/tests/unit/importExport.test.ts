// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { convertFromJupyter, convertToJupyter } from '../../interop/jupyter';
import { CellKind, CellOutputKind, NotebookDocument } from '../../interfaces/vscode';
import { JupyterNotebook } from '../../interfaces/jupyter';
import { NotebookFile } from '../../interactiveNotebook';

describe('File export tests', () => {

    it('Export notebook to Jupyter', () => {
        let notebook: NotebookDocument = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    source: '[1;2;3;4]\r\n|> List.sum',
                    language: 'fsharp',
                    outputs: [
                        {
                            outputKind: CellOutputKind.Rich,
                            data: {
                                'text/html': '10'
                            }
                        }
                    ]
                },
                {
                    cellKind: CellKind.Code,
                    source: '1 + 1',
                    language: 'csharp',
                    outputs: [
                        {
                            outputKind: CellOutputKind.Rich,
                            data: {
                                'text/html': '2'
                            }
                        }
                    ]
                },
                {
                    cellKind: CellKind.Markdown,
                    source: 'This is `markdown`.',
                    language: 'markdown',
                    outputs: []
                },
            ]
        };
        let jupyter = convertToJupyter(notebook);
        let expected: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    outputs: [
                        {
                            data: {
                                'text/html': [
                                    '10'
                                ]
                            },
                            execution_count: 1,
                            metadata: {},
                            output_type: 'execute_result'
                        }
                    ],
                    source: [
                        '#!fsharp\r\n',
                        '[1;2;3;4]\r\n',
                        '|> List.sum'
                    ]
                },
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    outputs: [
                        {
                            data: {
                                'text/html': [
                                    '2'
                                ]
                            },
                            execution_count: 1,
                            metadata: {},
                            output_type: 'execute_result'
                        }
                    ],
                    source: [
                        '1 + 1'
                    ]
                },
                {
                    cell_type: 'markdown',
                    metadata: {},
                    source: 'This is `markdown`.'
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
        expect(jupyter).to.deep.equal(expected);
    });

    it('import from jupyter', () => {
        let jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        // assume the notebook's language
                        '1+1',
                    ]
                },
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        // if a cell starts with a specific directive, use that
                        '#!fsharp\r\n',
                        '[1;2;3;4]\r\n',
                        '|> List.sum'
                    ]
                },
                {
                    cell_type: 'markdown',
                    metadata: {},
                    source: 'This is `markdown`.\r\nIt has 2 lines.'
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

        let expected: NotebookFile = {
            cells: [
                {
                    language: 'csharp',
                    contents: [
                        '1+1'
                    ]
                },
                {
                    language: 'fsharp',
                    contents: [
                        '[1;2;3;4]',
                        '|> List.sum',
                    ]
                },
                {
                    language: 'markdown',
                    contents: [
                        'This is `markdown`.',
                        'It has 2 lines.'
                    ]
                }
            ]
        };
        let notebook = convertFromJupyter(jupyter);
        expect(notebook).to.deep.equal(expected);
    });

    it('can read all jupyter newline types', () => {
        let jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        // CRLF
                        'var a = 1;\r\n',
                        'var b = a + 2;'
                    ]
                },
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        // CR
                        'var c = 3;\r',
                        'var d = c + 4;'
                    ]
                },
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        // LF
                        'var e = 5;\n',
                        'var f = e + 6;'
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

        let expected: NotebookFile = {
            cells: [
                {
                    language: 'csharp',
                    contents: [
                        'var a = 1;',
                        'var b = a + 2;'
                    ]
                },
                {
                    language: 'csharp',
                    contents: [
                        'var c = 3;',
                        'var d = c + 4;'
                    ]
                },
                {
                    language: 'csharp',
                    contents: [
                        'var e = 5;',
                        'var f = e + 6;'
                    ]
                }
            ]
        };
        let notebook = convertFromJupyter(jupyter);
        expect(notebook).to.deep.equal(expected);
    });

    it('magic commands are retained on jupyter open', () => {
        let jupyter: JupyterNotebook = {
            cells: [
                {
                    cell_type: 'code',
                    execution_count: 1,
                    metadata: {},
                    source: [
                        '#!time\r\n',
                        'var x = 1;'
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

        let expected: NotebookFile = {
            cells: [
                {
                    language: 'csharp',
                    contents: [
                        '#!time',
                        'var x = 1;'
                    ]
                }
            ]
        };
        let notebook = convertFromJupyter(jupyter);
        expect(notebook).to.deep.equal(expected);
    });
});

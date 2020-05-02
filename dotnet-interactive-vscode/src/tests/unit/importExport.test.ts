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
});

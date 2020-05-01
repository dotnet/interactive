import { expect } from 'chai';

import { convertToJupyter } from '../../interop/jupyter';
import { CellKind, CellOutputKind, CellDisplayOutput, NotebookDocument } from '../../interfaces/vscode';
import { JupyterNotebook } from '../../interfaces/jupyter';

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
});

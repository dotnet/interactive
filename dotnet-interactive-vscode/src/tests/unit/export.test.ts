import { expect } from 'chai';

import { convertToJupyter } from '../../interop/jupyter';
import { CellKind, CellOutputKind, CellDisplayOutput } from '../../interfaces/vscode';

describe('File export tests', () => {
    function genSimpleNotebook(language: string): any {
        let output: CellDisplayOutput = {
            outputKind: CellOutputKind.Rich,
            data: {
                'text/html': '2'
            }
        };
        let notebook = {
            cells: [
                {
                    cellKind: CellKind.Code,
                    source: '1+1',
                    outputs: [
                        output
                    ]
                }
            ],
            languages: [language]
        };
        return notebook;
    }

    it('Export C# notebook to Jupyter', () => {
        let notebook = genSimpleNotebook('csharp');
        let jupyter = convertToJupyter(notebook);
        expect(jupyter).to.deep.equal({
            cells: [
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
                        '1+1'
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
        });
    });

    it('Export F# notebook to Jupyter', () => {
        let notebook = genSimpleNotebook('fsharp');
        let jupyter = convertToJupyter(notebook);
        expect(jupyter).to.deep.equal({
            cells: [
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
                        '1+1'
                    ]
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
                    version: '4.5'
                }
            },
            nbformat: 4,
            nbformat_minor: 4
        });
    });
});

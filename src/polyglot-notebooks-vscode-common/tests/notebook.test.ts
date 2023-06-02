// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, use } from 'chai';
import * as fs from 'fs';
import * as path from 'path';

use(require('chai-fs'));

import { ClientMapper } from './../../src/vscode-common/clientMapper';
import { TestDotnetInteractiveChannel } from './testDotnetInteractiveChannel';
import {
    CodeSubmissionReceivedType,
    CommandFailedType,
    CommandSucceededType,
    CompleteCodeSubmissionReceivedType,
    Diagnostic,
    DiagnosticSeverity,
    DiagnosticsProducedType,
    DisplayedValueProducedType,
    DisplayedValueUpdatedType,
    ReturnValueProducedType,
    StandardOutputValueProducedType,
} from '../../src/vscode-common/polyglot-notebooks/contracts';
import { createChannelConfig, decodeNotebookCellOutputs, withFakeGlobalStorageLocation } from './utilities';
import { createUri } from '../../src/vscode-common/utilities';
import { backupNotebook, languageToCellKind } from '../../src/vscode-common/interactiveNotebook';
import * as vscodeLike from '../../src/vscode-common/interfaces/vscode-like';

describe('Notebook tests', () => {

    for (const language of ['csharp', 'fsharp'])
        it(`executes and returns expected value: ${language}`, async () => {
            const code = '1+1';
            const config = createChannelConfig(async (_notebookPath) =>
                new TestDotnetInteractiveChannel({
                    'SubmitCode': [
                        {
                            eventType: CodeSubmissionReceivedType,
                            event: {
                                code: code
                            }
                        },
                        {
                            eventType: CompleteCodeSubmissionReceivedType,
                            event: {
                                code: code
                            }
                        },
                        {
                            eventType: ReturnValueProducedType,
                            event: {
                                valueId: null,
                                formattedValues: [
                                    {
                                        mimeType: 'text/html',
                                        value: '2'
                                    }
                                ]
                            }
                        },
                        {
                            eventType: CommandSucceededType,
                            event: {}
                        }
                    ]
                }));
            const clientMapper = new ClientMapper(config);
            const client = await clientMapper.getOrAddClient(createUri('test/path'));
            const outputs: Array<vscodeLike.NotebookCellOutput> = [];
            await client.execute(code, language, output => outputs.push(output), _ => { });
            const decodedResults = decodeNotebookCellOutputs(outputs);
            expect(decodedResults).to.deep.equal([
                {
                    id: '1',
                    items: [
                        {
                            mime: 'text/html',
                            decodedData: '2',
                        }
                    ]
                }
            ]);
        });

    it('multiple stdout values cause the output to grow', async () => {
        const code = `
Console.WriteLine(1);
Console.WriteLine(2);
Guid.NewGuid().Display();
Console.WriteLine(3);
`;
        const config = createChannelConfig(async (_notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: StandardOutputValueProducedType,
                    event: {
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: '1\r\n'
                            }
                        ]
                    }
                },
                {
                    eventType: StandardOutputValueProducedType,
                    event: {
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: '2\r\n'
                            }
                        ]
                    }
                },
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/html',
                                value: '<div></div>'
                            }
                        ]
                    }
                },
                {
                    eventType: StandardOutputValueProducedType,
                    event: {
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: '3\r\n'
                            }
                        ]
                    }
                },
                {
                    eventType: CommandSucceededType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        const outputs: Array<vscodeLike.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', output => outputs.push(output), _ => { });
        const decodedResults = decodeNotebookCellOutputs(outputs);
        expect(decodedResults).to.deep.equal([
            {
                id: '1',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: '1\r\n',
                        stream: "stdout"
                    }
                ],
            },
            {
                id: '2',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: '2\r\n',
                        stream: "stdout"
                    }
                ]
            },
            {
                id: '3',
                items: [
                    {
                        mime: 'text/html',
                        decodedData: '<div></div>'
                    }
                ]
            },
            {
                id: '4',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: '3\r\n',
                        stream: "stdout"
                    }
                ]
            }
        ]);
    });

    it('returned json is properly parsed', async () => {
        const code = 'JObject.FromObject(new { a = 1, b = false })';
        const config = createChannelConfig(async (_notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: ReturnValueProducedType,
                    event: {
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'application/json',
                                value: '{"a":1,"b":false}' // encoded as a string, expected to be decoded when relayed back
                            }
                        ]
                    }
                },
                {
                    eventType: CommandSucceededType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        const outputs: Array<vscodeLike.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', output => outputs.push(output), _ => { });
        const decodedResults = decodeNotebookCellOutputs(outputs);
        expect(decodedResults).to.deep.equal([
            {
                id: '1',
                items: [
                    {
                        mime: 'application/json',
                        decodedData: {
                            a: 1,
                            b: false,
                        }
                    }
                ]
            }
        ]);
    });

    it('diagnostics are reported on CommandFailed', (done) => {
        const code = 'Console.WriteLin();';
        const config = createChannelConfig(async (_notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: DiagnosticsProducedType,
                    event: {
                        diagnostics: [
                            {
                                linePositionSpan: {
                                    start: {
                                        line: 0,
                                        character: 8
                                    },
                                    end: {
                                        line: 0,
                                        character: 15
                                    }
                                },
                                severity: DiagnosticSeverity.Error,
                                code: 'CS0117',
                                message: "'Console' does not contain a definition for 'WritLin'"
                            }
                        ]
                    }
                },
                {
                    eventType: CommandFailedType,
                    event: {
                        message: "CS0117: (0,8)-(0,15) 'Console' does not contain a definition for 'WritLin'"
                    }
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test/path')).then(client => {
            let diagnostics: Array<Diagnostic> = [];
            client.execute(code, 'csharp', _ => { }, diags => diagnostics = diags).then(result => {
                done(`expected execution to fail, but it passed with: ${result}`);
            }).catch(_err => {
                expect(diagnostics).to.deep.equal([
                    {
                        linePositionSpan: {
                            start: {
                                line: 0,
                                character: 8
                            },
                            end: {
                                line: 0,
                                character: 15
                            }
                        },
                        severity: DiagnosticSeverity.Error,
                        code: 'CS0117',
                        message: "'Console' does not contain a definition for 'WritLin'"
                    }
                ]);
                done();
            });
        });
    });

    it('diagnostics are reported on CommandSucceeded', async () => {
        const token = '123';
        const code = 'Console.WriteLine();';
        const config = createChannelConfig(async (_notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    }
                },
                {
                    eventType: DiagnosticsProducedType,
                    event: {
                        diagnostics: [
                            {
                                linePositionSpan: {
                                    start: {
                                        line: 0,
                                        character: 8
                                    },
                                    end: {
                                        line: 0,
                                        character: 16
                                    }
                                },
                                severity: DiagnosticSeverity.Warning,
                                code: 'CS4242',
                                message: "This is a fake diagnostic for testing."
                            }
                        ]
                    }
                },
                {
                    eventType: CommandSucceededType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        let diagnostics: Array<Diagnostic> = [];
        await client.execute(code, 'csharp', _ => { }, diags => diagnostics = diags);
        expect(diagnostics).to.deep.equal([
            {
                linePositionSpan: {
                    start: {
                        line: 0,
                        character: 8
                    },
                    end: {
                        line: 0,
                        character: 16
                    }
                },
                severity: DiagnosticSeverity.Warning,
                code: 'CS4242',
                message: "This is a fake diagnostic for testing."
            }
        ]);
    });

    it('diagnostics are reported when directly requested', async () => {

        const code = 'Console.WriteLine();';
        const config = createChannelConfig(async (_notebookPath) => new TestDotnetInteractiveChannel({
            'RequestDiagnostics': [
                {
                    eventType: DiagnosticsProducedType,
                    event: {
                        diagnostics: [
                            {
                                linePositionSpan: {
                                    start: {
                                        line: 0,
                                        character: 8
                                    },
                                    end: {
                                        line: 0,
                                        character: 16
                                    }
                                },
                                severity: DiagnosticSeverity.Warning,
                                code: 'CS4242',
                                message: "This is a fake diagnostic for testing."
                            }
                        ]
                    }
                },
                {
                    eventType: CommandSucceededType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        const diagnostics = await client.getDiagnostics('csharp', code);
        expect(diagnostics).to.deep.equal([
            {
                linePositionSpan: {
                    start: {
                        line: 0,
                        character: 8
                    },
                    end: {
                        line: 0,
                        character: 16
                    }
                },
                severity: DiagnosticSeverity.Warning,
                code: 'CS4242',
                message: "This is a fake diagnostic for testing."
            }
        ]);
    });

    it('notebook backup creates file: global storage exists', async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const rawData = new Uint8Array([1, 2, 3]);
            const backupLocation = path.join(globalStoragePath, Date.now().toString());
            const notebookBackup = await backupNotebook(rawData, backupLocation);
            const actualBuffer = fs.readFileSync(notebookBackup.id);
            const actualContents = Array.from(actualBuffer.values());
            expect(actualContents).to.deep.equal([1, 2, 3]);
        });
    });

    it("notebook backup creates file: global storage doesn't exist", async () => {
        await withFakeGlobalStorageLocation(false, async globalStoragePath => {
            const rawData = new Uint8Array([1, 2, 3]);
            const backupLocation = path.join(globalStoragePath, Date.now().toString());
            const notebookBackup = await backupNotebook(rawData, backupLocation);
            const actualBuffer = fs.readFileSync(notebookBackup.id);
            const actualContents = Array.from(actualBuffer.values());
            expect(actualContents).to.deep.equal([1, 2, 3]);
        });
    });

    it('notebook backup cleans up after itself', async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const rawData = new Uint8Array([1, 2, 3]);
            const backupLocation = path.join(globalStoragePath, Date.now().toString());
            const notebookBackup = await backupNotebook(rawData, backupLocation);
            expect(notebookBackup.id).to.be.file();
            notebookBackup.delete();
            expect(notebookBackup.id).to.not.be.a.path();
        });
    });

    //-------------------------------------------------------------------------
    //                                                               misc tests
    //-------------------------------------------------------------------------

    it('code cells are appropriately classified by language', () => {
        const codeLanguages = [
            'csharp',
            'fsharp',
            'html',
            'javascript',
            'pwsh'
        ];
        for (let language of codeLanguages) {
            expect(languageToCellKind(language)).to.equal(vscodeLike.NotebookCellKind.Code);
        }
    });

    it('markdown cells are appropriately classified', () => {
        const markdownLanguages = [
            'markdown'
        ];
        for (let language of markdownLanguages) {
            expect(languageToCellKind(language)).to.equal(vscodeLike.NotebookCellKind.Markup);
        }
    });
});

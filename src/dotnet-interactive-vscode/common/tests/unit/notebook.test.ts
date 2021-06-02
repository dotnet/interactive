// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, use } from 'chai';
import * as fs from 'fs';
import * as path from 'path';

use(require('chai-fs'));

import { ClientMapper } from './../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
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
} from '../../interfaces/contracts';
import { createKernelTransportConfig, decodeNotebookCellOutputs, withFakeGlobalStorageLocation } from './utilities';
import { createUri } from '../../utilities';
import { backupNotebook, languageToCellKind } from '../../interactiveNotebook';
import * as vscodeLike from '../../interfaces/vscode-like';

describe('Notebook tests', () => {
    for (const language of ['csharp', 'fsharp']) {
        it(`executes and returns expected value: ${language}`, async () => {
            const token = '123';
            const code = '1+1';
            const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
                'SubmitCode': [
                    {
                        eventType: CodeSubmissionReceivedType,
                        event: {
                            code: code
                        },
                        token
                    },
                    {
                        eventType: CompleteCodeSubmissionReceivedType,
                        event: {
                            code: code
                        },
                        token
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
                        },
                        token
                    },
                    {
                        eventType: CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            }));
            const clientMapper = new ClientMapper(config);
            const client = await clientMapper.getOrAddClient(createUri('test/path'));
            let result: Array<vscodeLike.NotebookCellOutput> = [];
            await client.execute(code, language, outputs => result = outputs, _ => { }, { token });
            const decodedResults = decodeNotebookCellOutputs(result);
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
    }

    it('multiple stdout values cause the output to grow', async () => {
        const token = '123';
        const code = `
Console.WriteLine(1);
Console.WriteLine(1);
Console.WriteLine(1);
`;
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
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
                    },
                    token
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
                    },
                    token
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
                    },
                    token
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        let result: Array<vscodeLike.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = decodeNotebookCellOutputs(result);
        expect(decodedResults).to.deep.equal([
            {
                id: '1',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: '1\r\n',
                    }
                ],
            },
            {
                id: '2',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: '2\r\n',
                    }
                ]
            },
            {
                id: '3',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: '3\r\n',
                    }
                ]
            }
        ]);
    });

    it('updated values are replaced instead of added', async () => {
        const token = '123';
        const code = '#r nuget:Newtonsoft.Json';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        valueId: 'newtonsoft.json',
                        formattedValues: [{
                            mimeType: "text/plain",
                            value: "Installing package Newtonsoft.Json..."
                        }]
                    },
                    token
                },
                {
                    eventType: DisplayedValueUpdatedType,
                    event: {
                        valueId: 'newtonsoft.json',
                        formattedValues: [
                            {
                                mimeType: "text/plain",
                                value: "Installed package Newtonsoft.Json version 1.2.3.4"
                            }]
                    },
                    token
                },
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        valueId: null,
                        formattedValues: [{
                            mimeType: "text/plain",
                            value: "sentinel"
                        }]
                    },
                    token
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        let result: Array<vscodeLike.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = decodeNotebookCellOutputs(result);
        expect(decodedResults).to.deep.equal([
            {
                id: '2',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: 'Installed package Newtonsoft.Json version 1.2.3.4',
                    }
                ]
            },
            {
                id: '3',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: 'sentinel',
                    }
                ]
            },
        ]);
    });

    it('returned json is properly parsed', async () => {
        const token = '123';
        const code = 'JObject.FromObject(new { a = 1, b = false })';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
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
                    },
                    token
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        let result: Array<vscodeLike.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = decodeNotebookCellOutputs(result);
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
        const token = '123';
        const code = 'Console.WriteLin();';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
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
                    },
                    token
                },
                {
                    eventType: CommandFailedType,
                    event: {
                        message: "CS0117: (0,8)-(0,15) 'Console' does not contain a definition for 'WritLin'"
                    },
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test/path')).then(client => {
            let diagnostics: Array<Diagnostic> = [];
            client.execute(code, 'csharp', _ => { }, diags => diagnostics = diags, { token }).then(result => {
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
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
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
                    },
                    token
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        let diagnostics: Array<Diagnostic> = [];
        await client.execute(code, 'csharp', _ => { }, diags => diagnostics = diags, { token });
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
        const token = '123';
        const code = 'Console.WriteLine();';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
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
                    },
                    token
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        const diagnostics = await client.getDiagnostics('csharp', code, token);
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

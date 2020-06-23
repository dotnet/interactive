// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from './../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { CellOutput, CellOutputKind } from '../../interfaces/vscode';
import { CodeSubmissionReceivedType, CommandSucceededType, CompleteCodeSubmissionReceivedType, DisplayedValueProducedType, DisplayedValueUpdatedType, ReturnValueProducedType, StandardOutputValueProducedType } from '../../contracts';

describe('Notebook tests', () => {
    for (let language of ['csharp', 'fsharp']) {
        it(`executes and returns expected value: ${language}`, async () => {
            let token = '123';
            let code = '1+1';
            let clientMapper = new ClientMapper(() => TestKernelTransport.create({
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
                            value: 2,
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
            let client = await clientMapper.getOrAddClient({ fsPath: 'test/path' });
            let result: Array<CellOutput> = [];
            await client.execute(code, language, outputs => result = outputs, token);
            expect(result).to.deep.equal([
                {
                    outputKind: CellOutputKind.Rich,
                    data: {
                        'text/html': '2'
                    }
                }
            ]);
        });
    }

    it('multiple stdout values cause the output to grow', async () => {
        let token = '123';
        let code = `
Console.WriteLine(1);
Console.WriteLine(1);
Console.WriteLine(1);
`;
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
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
                        value: '1\r\n',
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
                        value: '2\r\n',
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
                        value: '3\r\n',
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
        let client = await clientMapper.getOrAddClient({ fsPath: 'test/path' });
        let result: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, token);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': '1\r\n'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': '2\r\n'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': '3\r\n'
                }
            }
        ]);
    });

    it('updated values are replaced instead of added', async () => {
        let token = '123';
        let code = '#r nuget:Newtonsoft.Json';
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
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
                        value: 'Installing package Newtonsoft.Json...',
                        formattedValues: []
                    },
                    token
                },
                {
                    eventType: DisplayedValueUpdatedType,
                    event: {
                        valueId: 'newtonsoft.json',
                        value: 'Installed package Newtonsoft.Json version 1.2.3.4',
                        formattedValues: []
                    },
                    token
                },
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        valueId: null,
                        value: 'sentinel',
                        formattedValue: []
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
        let client = await clientMapper.getOrAddClient({ fsPath: 'test/path' });
        let result: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, token);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': 'Installed package Newtonsoft.Json version 1.2.3.4'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': 'sentinel'
                }
            },
        ]);
    });

    it('returned json is property parsed', async () => {
        let token = '123';
        let code = 'JObject.FromObject(new { a = 1, b = false })';
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
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
                        value: 2,
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
        let client = await clientMapper.getOrAddClient({ fsPath: 'test/path' });
        let result: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, token);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'application/json': {
                        a: 1,
                        b: false
                    }
                }
            }
        ]);
    });
});

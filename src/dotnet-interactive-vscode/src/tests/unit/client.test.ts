// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { CellOutput, CellOutputKind } from '../../interfaces/vscode';
import { CodeSubmissionReceivedType, CompleteCodeSubmissionReceivedType, CommandSucceededType, DisplayedValueProducedType, ReturnValueProducedType, UpdateDisplayedValueType, DisplayedValueUpdatedType } from '../../contracts';

describe('InteractiveClient tests', () => {
    it('command execution returns deferred events', async () => {
        let token = 'test-token';
        let code = '1 + 1';
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
            'SubmitCode': [
                {
                    // deferred event; unassociated with the original submission; has its own token
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: '',
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: 'deferred output'
                            }
                        ]
                    },
                    token: 'token-for-deferred-command-doesnt-match-any-other-token'
                },
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
        await client.execute(code, 'csharp', outputs => result = outputs, token);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': 'deferred output'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            }
        ]);
    });

    it('deferred events do not interfere with display update events', async () => {
        let token = 'test-token';
        let code = '1 + 1';
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
            'SubmitCode': [
                {
                    // deferred event; unassociated with the original submission; has its own token
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: '',
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: 'deferred output'
                            }
                        ]
                    },
                    token: 'token-for-deferred-command-doesnt-match-any-other-token'
                },
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: 1,
                        valueId: "displayId",
                        formattedValues: [
                            {
                                mimeType: 'text/html',
                                value: '1'
                            }
                        ]
                    },
                    token
                },
                {
                    eventType: DisplayedValueUpdatedType,
                    event: {
                        value: 2,
                        valueId: "displayId",
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
        await client.execute(code, 'csharp', outputs => result = outputs, token);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': 'deferred output'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            }
        ]);
    });

    it('interleaved deferred events do not interfere with display update events', async () => {
        let token = 'test-token';
        let code = '1 + 1';
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
            'SubmitCode': [
                {
                    // deferred event; unassociated with the original submission; has its own token
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: '',
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: 'deferred output 1'
                            }
                        ]
                    },
                    token: 'token-for-deferred-command-doesnt-match-any-other-token'
                },
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: 1,
                        valueId: "displayId",
                        formattedValues: [
                            {
                                mimeType: 'text/html',
                                value: '1'
                            }
                        ]
                    },
                    token
                },
                {
                    // deferred event; unassociated with the original submission; has its own token
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: '',
                        valueId: null,
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: 'deferred output 2'
                            }
                        ]
                    },
                    token: 'token-for-deferred-command-doesnt-match-any-other-token'
                },
                {
                    eventType: DisplayedValueUpdatedType,
                    event: {
                        value: 2,
                        valueId: "displayId",
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
        await client.execute(code, 'csharp', outputs => result = outputs, token);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': 'deferred output 1'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            },
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/plain': 'deferred output 2'
                }
            }
        ]);
    });

    it('display update events from separate submissions trigger the correct observer', async () => {
        let code = '1 + 1';
        let clientMapper = new ClientMapper(() => TestKernelTransport.create({
            'SubmitCode#1': [
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: 1,
                        valueId: "displayId",
                        formattedValues: [
                            {
                                mimeType: 'text/html',
                                value: '1'
                            }
                        ]
                    },
                    token: 'token 1'
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token: 'token 1'
                }
            ],
            'SubmitCode#2': [
                {
                    eventType: DisplayedValueProducedType,
                    event: {
                        value: 2,
                        valueId: "displayId",
                        formattedValues: [
                            {
                                mimeType: 'text/html',
                                value: '2'
                            }
                        ]
                    },
                    token: 'token 2'
                },
                {
                    eventType: CommandSucceededType,
                    event: {},
                    token: 'token 2'
                }
            ]
        }));
        let client = await clientMapper.getOrAddClient({ fsPath: 'test/path' });

        // execute first command
        let result1: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result1 = outputs, 'token 1');
        expect(result1).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '1'
                }
            }
        ]);

        // execute second command
        let result2: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result2 = outputs, 'token 2');
        expect(result2).to.deep.equal([]);

        // ensure first result array was updated
        expect(result1).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            }
        ]);
    });
});

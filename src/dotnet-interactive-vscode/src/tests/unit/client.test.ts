// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { CellOutput, CellOutputKind } from '../../interfaces/vscode';
import { CodeSubmissionReceivedType, CompleteCodeSubmissionReceivedType, CommandHandledType, DisplayedValueProducedType, ReturnValueProducedType, UpdateDisplayedValueType, DisplayedValueUpdatedType } from '../../contracts';

describe('InteractiveClient tests', () => {
    it('command execution returns deferred events', async () => {
        let token = 'test-token';
        let code = '1 + 1';
        let clientMapper = new ClientMapper(() => new TestKernelTransport({
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
                    eventType: CommandHandledType,
                    event: {},
                    token
                }
            ]
        }));
        let client = clientMapper.getOrAddClient({ fsPath: 'test/path' });
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
        let clientMapper = new ClientMapper(() => new TestKernelTransport({
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
                    eventType: CommandHandledType,
                    event: {},
                    token
                }
            ]
        }));
        let client = clientMapper.getOrAddClient({ fsPath: 'test/path' });
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
        let clientMapper = new ClientMapper(() => new TestKernelTransport({
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
                    eventType: CommandHandledType,
                    event: {},
                    token
                }
            ]
        }));
        let client = clientMapper.getOrAddClient({ fsPath: 'test/path' });
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
});

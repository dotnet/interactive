// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { CellOutput, CellOutputKind } from '../../interfaces/vscode';
import { CodeSubmissionReceivedType, CompleteCodeSubmissionReceivedType, CommandSucceededType, DisplayedValueProducedType, ReturnValueProducedType, DisplayedValueUpdatedType, CommandFailedType } from '../../contracts';

describe('InteractiveClient tests', () => {
    it('command execution returns deferred events', async () => {
        let token = 'test-token';
        let code = '1 + 1';
        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
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
                    token: 'deferredCommand::token-for-deferred-command-doesnt-match-any-other-token'
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
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, token);
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
        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
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
                    token: 'deferredCommand::123'
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
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, token);
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
        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
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
                    token: 'deferredCommand::123'
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
                    token: 'deferredCommand::456'
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
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, token);
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
        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
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
        await client.execute(code, 'csharp', outputs => result1 = outputs, _ => { }, 'token 1');
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
        await client.execute(code, 'csharp', outputs => result2 = outputs, _ => { }, 'token 2');
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

    it('CommandFailedEvent rejects the execution promise', (done) => {
        const token = 'token';
        const clientMapper = new ClientMapper(async (_notebookPath) => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CommandFailedType,
                    event: {},
                    token
                }
            ]
        }));
        clientMapper.getOrAddClient({ fsPath: 'test/path' }).then(client => {
            client.execute('bad-code-that-will-fail', 'csharp', _ => { }, _ => { }, token).then(result => {
                done(`expected execution to fail promise, but passed with: ${result}`);
            }).catch(_err => {
                done();
            });
        });
    });

    it('clientMapper can reassociate clients', (done) => {
        let transportCreated = false;
        const clientMapper = new ClientMapper(async (_notebookPath) => {
            if (transportCreated) {
                done('transport already created; this function should not have been called again');
            }

            transportCreated = true;
            return new TestKernelTransport({});
        });
        clientMapper.getOrAddClient({ fsPath: 'test-path.dib' }).then(_client => {
            clientMapper.reassociateClient({ fsPath: 'test-path.dib' }, { fsPath: 'updated-path.dib' });
            clientMapper.getOrAddClient({ fsPath: 'updated-path.dib' }).then(_reassociatedClient => {
                done();
            });
        });
    });
});

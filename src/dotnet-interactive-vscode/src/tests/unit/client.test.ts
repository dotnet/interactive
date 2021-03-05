// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from 'chai';
import * as chai_as_promised from 'chai-as-promised';
chai.use(chai_as_promised);
const expect = chai.expect;

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { CallbackTestKernelTransport } from './callbackTestKernelTransport';
import { CodeSubmissionReceivedType, CompleteCodeSubmissionReceivedType, CommandSucceededType, DisplayedValueProducedType, ReturnValueProducedType, DisplayedValueUpdatedType, CommandFailedType } from 'dotnet-interactive-vscode-interfaces/out/contracts';
import { debounce, wait } from '../../utilities';
import * as interfaces from 'dotnet-interactive-vscode-interfaces/out/notebook';

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
        let result: Array<interfaces.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        expect(result).to.deep.equal([
            {
                id: '1',
                outputs: [
                    {
                        mime: 'text/plain',
                        value: 'deferred output',
                    }
                ]
            },
            {
                id: '2',
                outputs: [
                    {
                        mime: 'text/html',
                        value: '2',
                    }
                ]
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
        let result: Array<interfaces.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        expect(result).to.deep.equal([
            {
                id: '1',
                outputs: [
                    {
                        mime: 'text/plain',
                        value: 'deferred output',
                    }
                ]
            },
            {
                id: '3',
                outputs: [
                    {
                        mime: 'text/html',
                        value: '2',
                    }
                ]
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
        let result: Array<interfaces.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        expect(result).to.deep.equal([
            {
                id: '1',
                outputs: [
                    {
                        mime: 'text/plain',
                        value: 'deferred output 1',
                    }
                ]
            },
            {
                id: '4',
                outputs: [
                    {
                        mime: 'text/html',
                        value: '2',
                    }
                ]
            },
            {
                id: '3',
                outputs: [
                    {
                        mime: 'text/plain',
                        value: 'deferred output 2',
                    }
                ]
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
        let result1: Array<interfaces.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result1 = outputs, _ => { }, { token: 'token 1' });
        expect(result1).to.deep.equal([
            {
                id: '1',
                outputs: [
                    {
                        mime: 'text/html',
                        value: '1',
                    }
                ]
            }
        ]);

        // execute second command
        let result2: Array<interfaces.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', outputs => result2 = outputs, _ => { }, { token: 'token 2' });
        expect(result2).to.deep.equal([]);

        // ensure first result array was updated
        expect(result1).to.deep.equal([
            {
                id: '2',
                outputs: [
                    {
                        mime: 'text/html',
                        value: '2',
                    }
                ]
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
            client.execute('bad-code-that-will-fail', 'csharp', _ => { }, _ => { }, { token }).then(result => {
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

    it('clientMapper reassociate does nothing for an untracked file', async () => {
        let transportCreated = false;
        const clientMapper = new ClientMapper(async (_notebookPath) => {
            if (transportCreated) {
                throw new Error('transport already created; this function should not have been called again');
            }

            transportCreated = true;
            return new TestKernelTransport({});
        });
        await clientMapper.getOrAddClient({ fsPath: 'test-path.dib' });
        clientMapper.reassociateClient({ fsPath: 'not-a-tracked-file.txt' }, { fsPath: 'also-not-a-tracked-file.txt' });
        const _existingClient = await clientMapper.getOrAddClient({ fsPath: 'test-path.dib' });
        expect(clientMapper.isDotNetClient({ fsPath: 'not-a-tracked-file.txt' })).to.be.false;
        expect(clientMapper.isDotNetClient({ fsPath: 'also-not-a-tracked-file.txt' })).to.be.false;
    });

    it('execution prevents diagnostics request forwarding', async () => {
        let token = 'test-token';

        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
            'SubmitCode': [

                {
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));

        let diagnosticsCallbackFired = false;
        debounce("id0", 500, () => {
            diagnosticsCallbackFired = true;
        });

        const client = await clientMapper.getOrAddClient({ fsPath: 'test-path.dib' });
        await client.execute("1+1", "csharp", (_outputs) => { }, (_diagnostics) => { }, { token: token, id: "id0" });
        await wait(1000);
        expect(diagnosticsCallbackFired).to.be.false;
    });

    it('exception in submit code properly rejects all promises', done => {
        const token = 'test-token';
        const clientMapper = new ClientMapper(async (_notebookPath) => new CallbackTestKernelTransport({
            'SubmitCode': () => {
                throw new Error('expected exception during submit');
            },
        }));
        clientMapper.getOrAddClient({ fsPath: 'test-path.dib' }).then(client => {
            expect(client.execute("1+1", "csharp", _outputs => { }, _diagnostics => { }, { token, id: '' })).eventually.rejectedWith('expected exception during submit').notify(done);
        });
    });

    it('exception in submit code properly generates error outputs', done => {
        const token = 'test-token';
        const clientMapper = new ClientMapper(async (_notebookPath) => new CallbackTestKernelTransport({
            'SubmitCode': () => {
                throw new Error('expected exception during submit');
            },
        }));
        let seenOutputs: Array<interfaces.NotebookCellOutput> = [];
        clientMapper.getOrAddClient({ fsPath: 'test-path.dib' }).then(client => {
            expect(client.execute("1+1", "csharp", outputs => { seenOutputs = outputs; }, _diagnostics => { }, { token, id: '' })).eventually.rejected.then(() => {
                try {
                    expect(seenOutputs).to.deep.equal([{
                        id: '1',
                        outputs: [{
                            mime: interfaces.ErrorOutputMimeType,
                            value: 'Error: expected exception during submit',
                        }]
                    }]);
                    done();
                } catch (e) {
                    done(e);
                }
            });
        });
    });

    it('exception creating kernel transport gracefully fails', done => {
        const clientMapper = new ClientMapper(async _notebookPath => {
            throw new Error('simulated error during transport creation');
        });
        expect(clientMapper.getOrAddClient({ fsPath: 'fake-notebook' })).eventually.rejectedWith('simulated error during transport creation').notify(done);
    });

});

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from 'chai';
import * as chai_as_promised from 'chai-as-promised';
chai.use(chai_as_promised);
const expect = chai.expect;

import { ClientMapper } from '../../src/vscode-common/clientMapper';
import { TestDotnetInteractiveChannel } from './testDotnetInteractiveChannel';
import { CallbackTestTestDotnetInteractiveChannel } from './callbackTestTestDotnetInteractiveChannel';
import { CodeSubmissionReceivedType, CompleteCodeSubmissionReceivedType, CommandSucceededType, DisplayedValueProducedType, ReturnValueProducedType, DisplayedValueUpdatedType, CommandFailedType, ErrorProducedType } from '../../src/vscode-common/polyglot-notebooks/contracts';
import { createUri, debounce, wait } from '../../src/vscode-common/utilities';
import * as vscodeLike from '../../src/vscode-common/interfaces/vscode-like';
import { createChannelConfig, decodeNotebookCellOutputs } from './utilities';

describe('InteractiveClient tests', () => {

    it.only('command execution returns deferred events', async () => {
        const code = '1 + 1';
        const config = createChannelConfig(async (notebookPath) => new TestDotnetInteractiveChannel({
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
                    }
                },
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
                        value: 2,
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
        await client.execute(code, 'csharp', output => outputs.push(output), _ => { });
        const decodedResults = decodeNotebookCellOutputs(outputs);
        expect(decodedResults).to.deep.equal([
            {
                id: '1',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: 'deferred output',
                    }
                ]
            },
            {
                id: '2',
                items: [
                    {
                        mime: 'text/html',
                        decodedData: '2',
                    }
                ]
            }
        ]);
    });

    it('display events with multiple mimeTypes', async () => {
        const code = '1 + 1';
        const config = createChannelConfig(async (notebookPath) => new TestDotnetInteractiveChannel({
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
                            },
                            {
                                mimeType: 'apllication/json',
                                value: '{}'
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

        // execute first command
        const outputs1: Array<vscodeLike.NotebookCellOutput> = [];
        await client.execute(code, 'csharp', output => outputs1.push(output), _ => { });
        let decodedResults1 = decodeNotebookCellOutputs(outputs1);
        expect(decodedResults1).to.deep.equal([
            {
                id: 'displayId',
                items: [
                    {
                        mime: 'text/html',
                        decodedData: '1',
                    }, {
                        mime: 'apllication/json',
                        decodedData: '{}'
                    }
                ]
            }
        ]);
    });

    it('ErrorProduced resolve the execution promise reporting failuer', async () => {
        const config = createChannelConfig(async (notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [
                {
                    eventType: ErrorProducedType,
                    event: { message: "failed internal command" }
                },
                {
                    eventType: CommandSucceededType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        const client = await clientMapper.getOrAddClient(createUri('test/path'));
        const result = await client.execute('1+1', 'csharp', _ => { }, _ => { });
        expect(result).to.be.equal(false);
    });

    it('CommandFailedEvent rejects the execution promise', (done) => {
        const config = createChannelConfig(async (notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [
                {
                    eventType: CommandFailedType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test/path')).then(client => {
            client.execute('bad-code-that-will-fail', 'csharp', _ => { }, _ => { }).then(result => {
                done(`expected execution to fail promise, but passed with: ${result}`);
            }).catch(_err => {
                done();
            });
        });
    });

    it('clientMapper can reassociate clients', (done) => {
        let channelCreated = false;
        const config = createChannelConfig(async (_notebookPath) => {
            if (channelCreated) {
                done('channel already created; this function should not have been called again');
            }

            channelCreated = true;
            return new TestDotnetInteractiveChannel({});
        });
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test-path.dib')).then(_client => {
            clientMapper.reassociateClient(createUri('test-path.dib'), createUri('updated-path.dib'));
            clientMapper.getOrAddClient(createUri('updated-path.dib')).then(_reassociatedClient => {
                done();
            });
        });
    });

    it('clientMapper reassociate does nothing for an untracked file', async () => {
        let channelCreated = false;
        const config = createChannelConfig(async (_notebookPath) => {
            if (channelCreated) {
                throw new Error('channel already created; this function should not have been called again');
            }

            channelCreated = true;
            return new TestDotnetInteractiveChannel({});
        });
        const clientMapper = new ClientMapper(config);
        await clientMapper.getOrAddClient(createUri('test-path.dib'));
        clientMapper.reassociateClient(createUri('not-a-tracked-file.txt'), createUri('also-not-a-tracked-file.txt'));
        const _existingClient = await clientMapper.getOrAddClient(createUri('test-path.dib'));
        expect(clientMapper.isDotNetClient(createUri('not-a-tracked-file.txt'))).to.be.false;
        expect(clientMapper.isDotNetClient(createUri('also-not-a-tracked-file.txt'))).to.be.false;
    });

    it('execution prevents diagnostics request forwarding', async () => {
        const config = createChannelConfig(async (notebookPath) => new TestDotnetInteractiveChannel({
            'SubmitCode': [

                {
                    eventType: CommandSucceededType,
                    event: {}
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        let diagnosticsCallbackFired = false;
        debounce("id0", 500, () => {
            diagnosticsCallbackFired = true;
        });

        const client = await clientMapper.getOrAddClient(createUri('test-path.dib'));
        await client.execute("1+1", "csharp", (_outputs) => { }, (_diagnostics) => { }, { id: "id0" });
        await wait(1000);
        expect(diagnosticsCallbackFired).to.be.false;
    });

    it('exception in submit code properly rejects all promises', async () => {
        const token = 'test-token';
        const config = createChannelConfig(async (_notebookPath) => new CallbackTestTestDotnetInteractiveChannel({
            'SubmitCode': () => {
                throw new Error('expected exception during submit');
            },
        }));

        const clientMapper = new ClientMapper(config);
        let client = await clientMapper.getOrAddClient(createUri('test-path.dib'));

        await expect(client.execute("1+1", "csharp", _outputs => { }, _diagnostics => { }))
            .eventually
            .rejectedWith('expected exception during submit');
    });

    it('exception in submit code properly generates error outputs', done => {
        const token = 'test-token';
        const config = createChannelConfig(async (_notebookPath) => new CallbackTestTestDotnetInteractiveChannel({
            'SubmitCode': () => {
                throw new Error('expected exception during submit');
            },
        }));
        const clientMapper = new ClientMapper(config);
        const seenOutputs: Array<vscodeLike.NotebookCellOutput> = [];
        clientMapper.getOrAddClient(createUri('test-path.dib')).then(client => {
            expect(client.execute("1+1", "csharp", output => seenOutputs.push(output), _diagnostics => { })).eventually.rejected.then(() => {
                try {
                    const decodedOutputs = decodeNotebookCellOutputs(seenOutputs);
                    expect(decodedOutputs).to.deep.equal([{
                        id: '1',
                        items: [{
                            mime: vscodeLike.ErrorOutputMimeType,
                            decodedData: {
                                name: 'Error',
                                message: 'expected exception during submit',
                            },
                        }]
                    }]);
                    done();
                } catch (e) {
                    done(e);
                }
            });
        });
    });

    it('exception creating kernel channel gracefully fails', done => {
        const config = createChannelConfig(async (_notebookPath) => {
            throw new Error('simulated error during channel creation');
        });
        const clientMapper = new ClientMapper(config);
        expect(clientMapper.getOrAddClient(createUri('fake-notebook'))).eventually.rejectedWith('simulated error during channel creation').notify(done);
    });

});

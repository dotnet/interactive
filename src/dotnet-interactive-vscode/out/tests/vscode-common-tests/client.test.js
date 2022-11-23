"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const chai = require("chai");
const chai_as_promised = require("chai-as-promised");
chai.use(chai_as_promised);
const expect = chai.expect;
const clientMapper_1 = require("../../src/vscode-common/clientMapper");
const testDotnetInteractiveChannel_1 = require("./testDotnetInteractiveChannel");
const callbackTestTestDotnetInteractiveChannel_1 = require("./callbackTestTestDotnetInteractiveChannel");
const contracts_1 = require("../../src/vscode-common/dotnet-interactive/contracts");
const utilities_1 = require("../../src/vscode-common/utilities");
const vscodeLike = require("../../src/vscode-common/interfaces/vscode-like");
const utilities_2 = require("./utilities");
describe('InteractiveClient tests', () => {
    it('command execution returns deferred events', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = 'test-token';
        const code = '1 + 1';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
                    {
                        // deferred event; unassociated with the original submission; has its own token
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.CodeSubmissionReceivedType,
                        event: {
                            code: code
                        },
                        token
                    },
                    {
                        eventType: contracts_1.CompleteCodeSubmissionReceivedType,
                        event: {
                            code: code
                        },
                        token
                    },
                    {
                        eventType: contracts_1.ReturnValueProducedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        let result = [];
        yield client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = (0, utilities_2.decodeNotebookCellOutputs)(result);
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
    }));
    it('deferred events do not interfere with display update events', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = 'test-token';
        const code = '1 + 1';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
                    {
                        // deferred event; unassociated with the original submission; has its own token
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.DisplayedValueUpdatedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        let result = [];
        yield client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = (0, utilities_2.decodeNotebookCellOutputs)(result);
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
                id: '3',
                items: [
                    {
                        mime: 'text/html',
                        decodedData: '2',
                    }
                ]
            }
        ]);
    }));
    it('interleaved deferred events do not interfere with display update events', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = 'test-token';
        const code = '1 + 1';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
                    {
                        // deferred event; unassociated with the original submission; has its own token
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.DisplayedValueUpdatedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        let result = [];
        yield client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = (0, utilities_2.decodeNotebookCellOutputs)(result);
        expect(decodedResults).to.deep.equal([
            {
                id: '1',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: 'deferred output 1',
                    }
                ]
            },
            {
                id: '4',
                items: [
                    {
                        mime: 'text/html',
                        decodedData: '2',
                    }
                ]
            },
            {
                id: '3',
                items: [
                    {
                        mime: 'text/plain',
                        decodedData: 'deferred output 2',
                    }
                ]
            }
        ]);
    }));
    it('display update events from separate submissions trigger the correct observer', () => __awaiter(void 0, void 0, void 0, function* () {
        const code = '1 + 1';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode#1': [
                    {
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token: 'token 1'
                    }
                ],
                'SubmitCode#2': [
                    {
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token: 'token 2'
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        // execute first command
        let result1 = [];
        yield client.execute(code, 'csharp', outputs => result1 = outputs, _ => { }, { token: 'token 1' });
        let decodedResults1 = (0, utilities_2.decodeNotebookCellOutputs)(result1);
        expect(decodedResults1).to.deep.equal([
            {
                id: '1',
                items: [
                    {
                        mime: 'text/html',
                        decodedData: '1',
                    }
                ]
            }
        ]);
        // execute second command
        let result2 = [];
        yield client.execute(code, 'csharp', outputs => result2 = outputs, _ => { }, { token: 'token 2' });
        expect(result2).to.deep.equal([]);
        // ensure first result array was updated
        decodedResults1 = (0, utilities_2.decodeNotebookCellOutputs)(result1);
        expect(decodedResults1).to.deep.equal([
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
    }));
    it('display events with multiple mimeTyoes', () => __awaiter(void 0, void 0, void 0, function* () {
        const code = '1 + 1';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode#1': [
                    {
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        },
                        token: 'token 1'
                    },
                    {
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token: 'token 1'
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        // execute first command
        let result1 = [];
        yield client.execute(code, 'csharp', outputs => result1 = outputs, _ => { }, { token: 'token 1' });
        let decodedResults1 = (0, utilities_2.decodeNotebookCellOutputs)(result1);
        expect(decodedResults1).to.deep.equal([
            {
                id: '1',
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
    }));
    it('ErrorProduced resolve the execution promise reporting failuer', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = 'token';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
                    {
                        eventType: contracts_1.ErrorProducedType,
                        event: { message: "failed internal command" },
                        token
                    },
                    {
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        const result = yield client.execute('1+1', 'csharp', _ => { }, _ => { }, { token });
        expect(result).to.be.equal(false);
    }));
    it('CommandFailedEvent rejects the execution promise', (done) => {
        const token = 'token';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
                    {
                        eventType: contracts_1.CommandFailedType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path')).then(client => {
            client.execute('bad-code-that-will-fail', 'csharp', _ => { }, _ => { }, { token }).then(result => {
                done(`expected execution to fail promise, but passed with: ${result}`);
            }).catch(_err => {
                done();
            });
        });
    });
    it('clientMapper can reassociate clients', (done) => {
        let channelCreated = false;
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            if (channelCreated) {
                done('channel already created; this function should not have been called again');
            }
            channelCreated = true;
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({});
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        clientMapper.getOrAddClient((0, utilities_1.createUri)('test-path.dib')).then(_client => {
            clientMapper.reassociateClient((0, utilities_1.createUri)('test-path.dib'), (0, utilities_1.createUri)('updated-path.dib'));
            clientMapper.getOrAddClient((0, utilities_1.createUri)('updated-path.dib')).then(_reassociatedClient => {
                done();
            });
        });
    });
    it('clientMapper reassociate does nothing for an untracked file', () => __awaiter(void 0, void 0, void 0, function* () {
        let channelCreated = false;
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            if (channelCreated) {
                throw new Error('channel already created; this function should not have been called again');
            }
            channelCreated = true;
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({});
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test-path.dib'));
        clientMapper.reassociateClient((0, utilities_1.createUri)('not-a-tracked-file.txt'), (0, utilities_1.createUri)('also-not-a-tracked-file.txt'));
        const _existingClient = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test-path.dib'));
        expect(clientMapper.isDotNetClient((0, utilities_1.createUri)('not-a-tracked-file.txt'))).to.be.false;
        expect(clientMapper.isDotNetClient((0, utilities_1.createUri)('also-not-a-tracked-file.txt'))).to.be.false;
    }));
    it('execution prevents diagnostics request forwarding', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = 'test-token';
        const config = (0, utilities_2.createChannelConfig)((notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
                    {
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        let diagnosticsCallbackFired = false;
        (0, utilities_1.debounce)("id0", 500, () => {
            diagnosticsCallbackFired = true;
        });
        const client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test-path.dib'));
        yield client.execute("1+1", "csharp", (_outputs) => { }, (_diagnostics) => { }, { token: token, id: "id0" });
        yield (0, utilities_1.wait)(1000);
        expect(diagnosticsCallbackFired).to.be.false;
    }));
    it('exception in submit code properly rejects all promises', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = 'test-token';
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new callbackTestTestDotnetInteractiveChannel_1.CallbackTestTestDotnetInteractiveChannel({
                'SubmitCode': () => {
                    throw new Error('expected exception during submit');
                },
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        let client = yield clientMapper.getOrAddClient((0, utilities_1.createUri)('test-path.dib'));
        yield expect(client.execute("1+1", "csharp", _outputs => { }, _diagnostics => { }, { token, id: '' }))
            .eventually
            .rejectedWith('expected exception during submit');
    }));
    it('exception in submit code properly generates error outputs', done => {
        const token = 'test-token';
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new callbackTestTestDotnetInteractiveChannel_1.CallbackTestTestDotnetInteractiveChannel({
                'SubmitCode': () => {
                    throw new Error('expected exception during submit');
                },
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        let seenOutputs = [];
        clientMapper.getOrAddClient((0, utilities_1.createUri)('test-path.dib')).then(client => {
            expect(client.execute("1+1", "csharp", outputs => { seenOutputs = outputs; }, _diagnostics => { }, { token, id: '' })).eventually.rejected.then(() => {
                try {
                    const decodedOutputs = (0, utilities_2.decodeNotebookCellOutputs)(seenOutputs);
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
                }
                catch (e) {
                    done(e);
                }
            });
        });
    });
    it('exception creating kernel channel gracefully fails', done => {
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            throw new Error('simulated error during channel creation');
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        expect(clientMapper.getOrAddClient((0, utilities_1.createUri)('fake-notebook'))).eventually.rejectedWith('simulated error during channel creation').notify(done);
    });
});
//# sourceMappingURL=client.test.js.map
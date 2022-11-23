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
const chai_1 = require("chai");
const fs = require("fs");
const path = require("path");
(0, chai_1.use)(require('chai-fs'));
const clientMapper_1 = require("./../../src/vscode-common/clientMapper");
const testDotnetInteractiveChannel_1 = require("./testDotnetInteractiveChannel");
const contracts_1 = require("../../src/vscode-common/dotnet-interactive/contracts");
const utilities_1 = require("./utilities");
const utilities_2 = require("../../src/vscode-common/utilities");
const interactiveNotebook_1 = require("../../src/vscode-common/interactiveNotebook");
const vscodeLike = require("../../src/vscode-common/interfaces/vscode-like");
describe('Notebook tests', () => {
    for (const language of ['csharp', 'fsharp']) {
        it(`executes and returns expected value: ${language}`, () => __awaiter(void 0, void 0, void 0, function* () {
            const token = '123';
            const code = '1+1';
            const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
                return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                    'SubmitCode': [
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
            const client = yield clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path'));
            let result = [];
            yield client.execute(code, language, outputs => result = outputs, _ => { }, { token });
            const decodedResults = (0, utilities_1.decodeNotebookCellOutputs)(result);
            (0, chai_1.expect)(decodedResults).to.deep.equal([
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
        }));
    }
    it('multiple stdout values cause the output to grow', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const code = `
Console.WriteLine(1);
Console.WriteLine(2);
Guid.NewGuid().Display();
Console.WriteLine(3);
`;
        const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
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
                        eventType: contracts_1.StandardOutputValueProducedType,
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
                        eventType: contracts_1.StandardOutputValueProducedType,
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
                        eventType: contracts_1.DisplayedValueProducedType,
                        event: {
                            valueId: null,
                            formattedValues: [
                                {
                                    mimeType: 'text/html',
                                    value: '<div></div>'
                                }
                            ]
                        },
                        token
                    },
                    {
                        eventType: contracts_1.StandardOutputValueProducedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path'));
        let result = [];
        yield client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = (0, utilities_1.decodeNotebookCellOutputs)(result);
        (0, chai_1.expect)(decodedResults).to.deep.equal([
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
    }));
    it('updated values are replaced instead of added', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const code = '#r nuget:Newtonsoft.Json';
        const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
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
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.DisplayedValueUpdatedType,
                        event: {
                            valueId: 'newtonsoft.json',
                            formattedValues: [
                                {
                                    mimeType: "text/plain",
                                    value: "Installed package Newtonsoft.Json version 1.2.3.4"
                                }
                            ]
                        },
                        token
                    },
                    {
                        eventType: contracts_1.DisplayedValueProducedType,
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path'));
        let result = [];
        yield client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = (0, utilities_1.decodeNotebookCellOutputs)(result);
        (0, chai_1.expect)(decodedResults).to.deep.equal([
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
    }));
    it('returned json is properly parsed', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const code = 'JObject.FromObject(new { a = 1, b = false })';
        const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
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
                        eventType: contracts_1.CommandSucceededType,
                        event: {},
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        const client = yield clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path'));
        let result = [];
        yield client.execute(code, 'csharp', outputs => result = outputs, _ => { }, { token });
        const decodedResults = (0, utilities_1.decodeNotebookCellOutputs)(result);
        (0, chai_1.expect)(decodedResults).to.deep.equal([
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
    }));
    it('diagnostics are reported on CommandFailed', (done) => {
        const token = '123';
        const code = 'Console.WriteLin();';
        const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
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
                        eventType: contracts_1.DiagnosticsProducedType,
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
                                    severity: contracts_1.DiagnosticSeverity.Error,
                                    code: 'CS0117',
                                    message: "'Console' does not contain a definition for 'WritLin'"
                                }
                            ]
                        },
                        token
                    },
                    {
                        eventType: contracts_1.CommandFailedType,
                        event: {
                            message: "CS0117: (0,8)-(0,15) 'Console' does not contain a definition for 'WritLin'"
                        },
                        token
                    }
                ]
            });
        }));
        const clientMapper = new clientMapper_1.ClientMapper(config);
        clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path')).then(client => {
            let diagnostics = [];
            client.execute(code, 'csharp', _ => { }, diags => diagnostics = diags, { token }).then(result => {
                done(`expected execution to fail, but it passed with: ${result}`);
            }).catch(_err => {
                (0, chai_1.expect)(diagnostics).to.deep.equal([
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
                        severity: contracts_1.DiagnosticSeverity.Error,
                        code: 'CS0117',
                        message: "'Console' does not contain a definition for 'WritLin'"
                    }
                ]);
                done();
            });
        });
    });
    it('diagnostics are reported on CommandSucceeded', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const code = 'Console.WriteLine();';
        const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'SubmitCode': [
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
                        eventType: contracts_1.DiagnosticsProducedType,
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
                                    severity: contracts_1.DiagnosticSeverity.Warning,
                                    code: 'CS4242',
                                    message: "This is a fake diagnostic for testing."
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
        const client = yield clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path'));
        let diagnostics = [];
        yield client.execute(code, 'csharp', _ => { }, diags => diagnostics = diags, { token });
        (0, chai_1.expect)(diagnostics).to.deep.equal([
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
                severity: contracts_1.DiagnosticSeverity.Warning,
                code: 'CS4242',
                message: "This is a fake diagnostic for testing."
            }
        ]);
    }));
    it('diagnostics are reported when directly requested', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const code = 'Console.WriteLine();';
        const config = (0, utilities_1.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'RequestDiagnostics': [
                    {
                        eventType: contracts_1.DiagnosticsProducedType,
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
                                    severity: contracts_1.DiagnosticSeverity.Warning,
                                    code: 'CS4242',
                                    message: "This is a fake diagnostic for testing."
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
        const client = yield clientMapper.getOrAddClient((0, utilities_2.createUri)('test/path'));
        const diagnostics = yield client.getDiagnostics('csharp', code, token);
        (0, chai_1.expect)(diagnostics).to.deep.equal([
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
                severity: contracts_1.DiagnosticSeverity.Warning,
                code: 'CS4242',
                message: "This is a fake diagnostic for testing."
            }
        ]);
    }));
    it('notebook backup creates file: global storage exists', () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_1.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const rawData = new Uint8Array([1, 2, 3]);
            const backupLocation = path.join(globalStoragePath, Date.now().toString());
            const notebookBackup = yield (0, interactiveNotebook_1.backupNotebook)(rawData, backupLocation);
            const actualBuffer = fs.readFileSync(notebookBackup.id);
            const actualContents = Array.from(actualBuffer.values());
            (0, chai_1.expect)(actualContents).to.deep.equal([1, 2, 3]);
        }));
    }));
    it("notebook backup creates file: global storage doesn't exist", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_1.withFakeGlobalStorageLocation)(false, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const rawData = new Uint8Array([1, 2, 3]);
            const backupLocation = path.join(globalStoragePath, Date.now().toString());
            const notebookBackup = yield (0, interactiveNotebook_1.backupNotebook)(rawData, backupLocation);
            const actualBuffer = fs.readFileSync(notebookBackup.id);
            const actualContents = Array.from(actualBuffer.values());
            (0, chai_1.expect)(actualContents).to.deep.equal([1, 2, 3]);
        }));
    }));
    it('notebook backup cleans up after itself', () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_1.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const rawData = new Uint8Array([1, 2, 3]);
            const backupLocation = path.join(globalStoragePath, Date.now().toString());
            const notebookBackup = yield (0, interactiveNotebook_1.backupNotebook)(rawData, backupLocation);
            (0, chai_1.expect)(notebookBackup.id).to.be.file();
            notebookBackup.delete();
            (0, chai_1.expect)(notebookBackup.id).to.not.be.a.path();
        }));
    }));
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
            (0, chai_1.expect)((0, interactiveNotebook_1.languageToCellKind)(language)).to.equal(vscodeLike.NotebookCellKind.Code);
        }
    });
    it('markdown cells are appropriately classified', () => {
        const markdownLanguages = [
            'markdown'
        ];
        for (let language of markdownLanguages) {
            (0, chai_1.expect)((0, interactiveNotebook_1.languageToCellKind)(language)).to.equal(vscodeLike.NotebookCellKind.Markup);
        }
    });
});
//# sourceMappingURL=notebook.test.js.map
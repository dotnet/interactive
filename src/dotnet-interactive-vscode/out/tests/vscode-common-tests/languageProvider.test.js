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
const clientMapper_1 = require("../../src/vscode-common/clientMapper");
const testDotnetInteractiveChannel_1 = require("./testDotnetInteractiveChannel");
const completion_1 = require("./../../src/vscode-common/languageServices/completion");
const hover_1 = require("./../../src/vscode-common/languageServices/hover");
const signatureHelp_1 = require("../../src/vscode-common/languageServices/signatureHelp");
const contracts_1 = require("../../src/vscode-common/dotnet-interactive/contracts");
const utilities_1 = require("../../src/vscode-common/utilities");
const utilities_2 = require("./utilities");
describe('LanguageProvider tests', () => {
    it('CompletionProvider', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'RequestCompletions': [
                    {
                        eventType: contracts_1.CompletionsProducedType,
                        event: {
                            linePositionSpan: null,
                            completions: [
                                {
                                    displayText: 'Sqrt',
                                    kind: 'Method',
                                    filterText: 'Sqrt',
                                    sortText: 'Sqrt',
                                    insertText: 'Sqrt',
                                    documentation: null
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
        const uri = (0, utilities_1.createUri)('test/path');
        clientMapper.getOrAddClient(uri);
        const code = 'Math.';
        const position = {
            line: 0,
            character: 5
        };
        // perform the completion request
        const completion = yield (0, completion_1.provideCompletion)(clientMapper, 'csharp', uri, code, position, 0, token);
        (0, chai_1.expect)(completion).to.deep.equal({
            linePositionSpan: null,
            completions: [
                {
                    displayText: 'Sqrt',
                    kind: 'Method',
                    filterText: 'Sqrt',
                    sortText: 'Sqrt',
                    insertText: 'Sqrt',
                    documentation: null
                }
            ]
        });
    }));
    it('HoverProvider', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'RequestHoverText': [
                    {
                        eventType: contracts_1.HoverTextProducedType,
                        event: {
                            content: [
                                {
                                    mimeType: 'text/markdown',
                                    value: 'readonly struct System.Int32'
                                }
                            ],
                            isMarkdown: true,
                            linePositionSpan: {
                                start: {
                                    line: 0,
                                    character: 8
                                },
                                end: {
                                    line: 0,
                                    character: 12
                                }
                            }
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
        clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        const code = 'var x = 1234;';
        const uri = (0, utilities_1.createUri)('test/path');
        const position = {
            line: 0,
            character: 10
        };
        // perform the hover request
        const hover = yield (0, hover_1.provideHover)(clientMapper, 'csharp', uri, code, position, 0, token);
        (0, chai_1.expect)(hover).to.deep.equal({
            contents: 'readonly struct System.Int32',
            isMarkdown: true,
            range: {
                start: {
                    line: 0,
                    character: 8
                },
                end: {
                    line: 0,
                    character: 12
                }
            }
        });
    }));
    it('SignatureHelpProvider', () => __awaiter(void 0, void 0, void 0, function* () {
        const token = '123';
        const config = (0, utilities_2.createChannelConfig)((_notebookPath) => __awaiter(void 0, void 0, void 0, function* () {
            return new testDotnetInteractiveChannel_1.TestDotnetInteractiveChannel({
                'RequestSignatureHelp': [
                    {
                        eventType: contracts_1.SignatureHelpProducedType,
                        event: {
                            activeSignature: 0,
                            activeParameter: 0,
                            signatures: [
                                {
                                    label: 'void Console.WriteLine(bool value)',
                                    documentation: {
                                        mimeType: 'text/markdown',
                                        value: ''
                                    },
                                    parameters: [
                                        {
                                            label: 'value',
                                            documentation: {
                                                mimeType: 'text/markdown',
                                                value: 'value'
                                            }
                                        }
                                    ]
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
        clientMapper.getOrAddClient((0, utilities_1.createUri)('test/path'));
        const code = 'Console.WriteLine(true';
        const uri = (0, utilities_1.createUri)('test/path');
        const position = {
            line: 0,
            character: 22
        };
        // perform the sig help request
        const sigHelp = yield (0, signatureHelp_1.provideSignatureHelp)(clientMapper, 'csharp', uri, code, position, 0, token);
        (0, chai_1.expect)(sigHelp).to.deep.equal({
            activeParameter: 0,
            activeSignature: 0,
            signatures: [
                {
                    documentation: {
                        mimeType: 'text/markdown',
                        value: ''
                    },
                    label: 'void Console.WriteLine(bool value)',
                    parameters: [
                        {
                            documentation: {
                                mimeType: 'text/markdown',
                                value: 'value'
                            },
                            label: 'value'
                        }
                    ]
                }
            ]
        });
    }));
});
//# sourceMappingURL=languageProvider.test.js.map
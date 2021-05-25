// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { provideCompletion } from './../../languageServices/completion';
import { provideHover } from './../../languageServices/hover';
import { provideSignatureHelp } from '../../languageServices/signatureHelp';
import { CommandSucceededType, CompletionsProducedType, HoverTextProducedType, SignatureHelpProducedType } from '../../interfaces/contracts';
import { createUri } from '../../utilities';
import { createKernelTransportConfig } from './utilities';

describe('LanguageProvider tests', () => {
    it('CompletionProvider', async () => {
        const token = '123';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'RequestCompletions': [
                {
                    eventType: CompletionsProducedType,
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
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test/path'));

        const code = 'Math.';
        const document = {
            uri: createUri('test/path'),
            getText: () => code
        };
        const position = {
            line: 0,
            character: 5
        };

        // perform the completion request
        const completion = await provideCompletion(clientMapper, 'csharp', document, position, 0, token);
        expect(completion).to.deep.equal({
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
    });

    it('HoverProvider', async () => {
        const token = '123';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'RequestHoverText': [
                {
                    eventType: HoverTextProducedType,
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
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test/path'));

        const code = 'var x = 1234;';
        const document = {
            uri: createUri('test/path'),
            getText: () => code,
        };
        const position = {
            line: 0,
            character: 10
        };

        // perform the hover request
        const hover = await provideHover(clientMapper, 'csharp', document, position, 0, token);
        expect(hover).to.deep.equal({
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
    });

    it('SignatureHelpProvider', async () => {
        const token = '123';
        const config = createKernelTransportConfig(async (_notebookPath) => new TestKernelTransport({
            'RequestSignatureHelp': [
                {
                    eventType: SignatureHelpProducedType,
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
                    eventType: CommandSucceededType,
                    event: {},
                    token
                }
            ]
        }));
        const clientMapper = new ClientMapper(config);
        clientMapper.getOrAddClient(createUri('test/path'));

        const code = 'Console.WriteLine(true';
        const document = {
            uri: createUri('test/path'),
            getText: () => code,
        };
        const position = {
            line: 0,
            character: 22
        };

        // perform the sig help request
        const sigHelp = await provideSignatureHelp(clientMapper, 'csharp', document, position, 0, token);
        expect(sigHelp).to.deep.equal({
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
    });
});

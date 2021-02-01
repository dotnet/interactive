// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { provideCompletion } from './../../languageServices/completion';
import { provideHover } from './../../languageServices/hover';
import { provideSignatureHelp } from '../../languageServices/signatureHelp';
import { CommandSucceededType, CompletionsProducedType, HoverTextProducedType, SignatureHelpProducedType } from '../../contracts';

describe('LanguageProvider tests', () => {
    it('CompletionProvider', async () => {
        let token = '123';
        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
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
        clientMapper.getOrAddClient({ fsPath: 'test/path' });

        let code = 'Math.';
        let document = {
            uri: { fsPath: 'test/path' },
            getText: () => code
        };
        let position = {
            line: 0,
            character: 5
        };

        // perform the completion request
        let completion = await provideCompletion(clientMapper, 'csharp', document, position, token);
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
        let token = '123';
        let clientMapper = new ClientMapper(async (notebookPath) => new TestKernelTransport({
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
        clientMapper.getOrAddClient({ fsPath: 'test/path' });

        let code = 'var x = 1234;';
        let document = {
            uri: { fsPath: 'test/path' },
            getText: () => code,
        };
        let position = {
            line: 0,
            character: 10
        };

        // perform the hover request
        let hover = await provideHover(clientMapper, 'csharp', document, position, token);
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
        let token = '123';
        let clientMapper = new ClientMapper(async (_notebookPath) => new TestKernelTransport({
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
        clientMapper.getOrAddClient({ fsPath: 'test/path' });

        let code = 'Console.WriteLine(true';
        let document = {
            uri: { fsPath: 'test/path' },
            getText: () => code,
        };
        let position = {
            line: 0,
            character: 22
        };

        // perform the sig help request
        let sigHelp = await provideSignatureHelp(clientMapper, 'csharp', document, position, token);
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

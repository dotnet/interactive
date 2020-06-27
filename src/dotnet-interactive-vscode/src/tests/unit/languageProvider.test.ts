// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { provideCompletion } from './../../languageServices/completion';
import { provideHover } from './../../languageServices/hover';
import { CommandSucceededType, CompletionsProducedType } from '../../contracts';

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
                    eventType: 'HoverTextProduced',
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
                    eventType: 'CommandSucceeded',
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
});

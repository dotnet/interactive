import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestClientTransport } from './testClientTransport';
import { Hover } from './../../languageServices/hover';
import { provideCompletion } from './../../languageServices/completion';

describe('LanguageProvider tests', () => {
    it('CompletionProvider', async () => {
        let clientMapper = new ClientMapper(() => new TestClientTransport({
            'RequestCompletion': [
                {
                    eventType: 'CompletionRequestCompleted',
                    event: {
                        completionList: [
                            {
                                displayText: 'Sqrt',
                                kind: 'Method',
                                filterText: 'Sqrt',
                                sortText: 'Sqrt',
                                insertText: 'Sqrt',
                                documentation: null
                            }
                        ]
                    }
                },
                {
                    eventType: 'CommandHandled',
                    event: {}
                }
            ]
        }));
        clientMapper.getOrAddClient({ path: 'test/path' });

        let code = 'Math.';
        let document = {
            uri: { path: 'test/path' },
            getText: () => code
        };
        let position = {
            line: 0,
            character: 5
        };

        // perform the completion request
        let completion = await provideCompletion(clientMapper, 'csharp', document, position);
        expect(completion).to.deep.equal([
            {
                displayText: 'Sqrt',
                kind: 'Method',
                filterText: 'Sqrt',
                sortText: 'Sqrt',
                insertText: 'Sqrt',
                documentation: null
            }
        ]);
    });

    it('HoverProvider', async () => {
        let clientMapper = new ClientMapper(() => new TestClientTransport({
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
                    }
                },
                {
                    eventType: 'CommandHandled',
                    event: {},
                }
            ]
        }));
        clientMapper.getOrAddClient({ path: 'test/path' });

        let code = 'var x = 1234;';
        let document = {
            uri: { path: 'test/path' },
            getText: () => code,
        };
        let position = {
            line: 0,
            character: 10
        };

        // perform the hover request
        let hover = await Hover.provideHover(clientMapper, 'csharp', document, position);
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

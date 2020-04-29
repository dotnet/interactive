import { expect } from 'chai';

import { ClientMapper } from '../../clientMapper';
import { TestClientAdapter } from './testClientAdapter';
import { Hover } from './../../languageServices/hover';
import { provideCompletion } from './../../languageServices/completion';

describe('LanguageProvider tests', () => {
    it('CompletionProvider', async () => {
        let clientMapper = new ClientMapper(targetKernelName => new TestClientAdapter(targetKernelName, {
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
        clientMapper.addClient('csharp', { path: 'test/path' });

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
        let completion = await provideCompletion(clientMapper, document, position);
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
        let clientMapper = new ClientMapper(targetKernelName => new TestClientAdapter(targetKernelName, {
            'RequestHoverText': [
                {
                    eventType: 'HoverMarkdownProduced',
                    event: {
                        content: 'readonly struct System.Int32',
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
        clientMapper.addClient('csharp', { path: 'test/path' });

        let code = 'data:text/plain;base64,dmFyIHggPSAxMjM0Ow=='; // var x = 1234;
        let document = {
            uri: { path: 'test/path' },
            getText: () => code,
        };
        let position = {
            line: 0,
            character: 10
        };

        // perform the hover request
        let hover = await Hover.provideHover(clientMapper, document, position);
        expect(hover.contents).to.equal('readonly struct System.Int32');
        expect(hover.range?.start.line).to.equal(0);
        expect(hover.range?.start.character).to.equal(8);
        expect(hover.range?.end.line).to.equal(0);
        expect(hover.range?.end.character).to.equal(12);
    });
});

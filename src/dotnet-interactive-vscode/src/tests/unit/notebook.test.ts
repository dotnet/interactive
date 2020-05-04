import { expect } from 'chai';

import { ClientMapper } from './../../clientMapper';
import { execute } from '../../interactiveNotebook';
import { TestClientTransport } from './testClientTransport';
import { CellOutputKind } from '../../interfaces/vscode';

describe('Notebook tests', () => {
    for (let language of ['csharp', 'fsharp']) {
        it(`executes and returns expected value: ${language}`, async () => {
            let code = '1+1';
            let clientMapper = new ClientMapper(() => new TestClientTransport({
                'SubmitCode': [
                    {
                        eventType: 'CodeSubmissionReceived',
                        event: {
                            code: code
                        }
                    },
                    {
                        eventType: 'CompleteCodeSubmissionReceived',
                        event: {
                            code: code
                        },
                    },
                    {
                        eventType: 'ReturnValueProduced',
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
                        eventType: 'CommandHandled',
                        event: {}
                    }
                ]
            }));
            let client = clientMapper.getOrAddClient({ path: 'test/path' });
            let outputs = await execute(language, code, client);
            expect(outputs).to.deep.equal([
                {
                    outputKind: CellOutputKind.Rich,
                    data: {
                        'text/html': '2'
                    }
                }
            ]);
        });
    }

    it('multiple stdout values cause the output to grow', async () => {
        let code = `
Console.WriteLine(1);
Console.WriteLine(1);
Console.WriteLine(1);
`;
        let clientMapper = new ClientMapper(() => new TestClientTransport({
            'SubmitCode': [
                {
                    eventType: 'CodeSubmissionReceived',
                    event: {
                        code: code
                    }
                },
                {
                    eventType: 'CompleteCodeSubmissionReceived',
                    event: {
                        code: code
                    },
                },
                {
                    eventType: 'StandardOutputValueProduced',
                    event: {
                        valueId: null,
                        value: '1\r\n',
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: '1\r\n'
                            }
                        ]
                    }
                },
                {
                    eventType: 'StandardOutputValueProduced',
                    event: {
                        valueId: null,
                        value: '2\r\n',
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: '2\r\n'
                            }
                        ]
                    }
                },
                {
                    eventType: 'StandardOutputValueProduced',
                    event: {
                        valueId: null,
                        value: '3\r\n',
                        formattedValues: [
                            {
                                mimeType: 'text/plain',
                                value: '3\r\n'
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
        let client = clientMapper.getOrAddClient({ path: 'test/path' });
        let outputs = await execute('csharp', code, client);
        expect(outputs).to.deep.equal([
            {
                outputKind: CellOutputKind.Text,
                text: '1\r\n'
            },
            {
                outputKind: CellOutputKind.Text,
                text: '2\r\n'
            },
            {
                outputKind: CellOutputKind.Text,
                text: '3\r\n'
            }
        ]);
    });
});

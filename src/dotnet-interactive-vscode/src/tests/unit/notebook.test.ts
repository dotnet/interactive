import { expect } from 'chai';

import { ClientMapper } from './../../clientMapper';
import { TestKernelTransport } from './testKernelTransport';
import { CellOutput, CellOutputKind } from '../../interfaces/vscode';
import { CodeSubmissionReceivedType, CommandHandledType, CompleteCodeSubmissionReceivedType, ReturnValueProducedType, StandardOutputValueProducedType } from '../../contracts';

describe('Notebook tests', () => {
    for (let language of ['csharp', 'fsharp']) {
        it(`executes and returns expected value: ${language}`, async () => {
            let token = '123';
            let code = '1+1';
            let clientMapper = new ClientMapper(() => new TestKernelTransport({
                'SubmitCode': [
                    {
                        eventType: CodeSubmissionReceivedType,
                        event: {
                            code: code
                        },
                        token
                    },
                    {
                        eventType: CompleteCodeSubmissionReceivedType,
                        event: {
                            code: code
                        },
                        token
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
                        },
                        token
                    },
                    {
                        eventType: CommandHandledType,
                        event: {},
                        token
                    }
                ]
            }));
            let client = clientMapper.getOrAddClient({ path: 'test/path' });
            await client.execute(code, language, cellOutput => {
                expect(cellOutput).to.deep.equal({
                        outputKind: CellOutputKind.Rich,
                        data: {
                            'text/html': '2'
                        }
                    }
                );
            }, token);
        });
    }

    it('multiple stdout values cause the output to grow', async () => {
        let token = '123';
        let code = `
Console.WriteLine(1);
Console.WriteLine(1);
Console.WriteLine(1);
`;
        let clientMapper = new ClientMapper(() => new TestKernelTransport({
            'SubmitCode': [
                {
                    eventType: CodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: CompleteCodeSubmissionReceivedType,
                    event: {
                        code: code
                    },
                    token
                },
                {
                    eventType: StandardOutputValueProducedType,
                    event: {
                        valueId: null,
                        value: '1\r\n',
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
                    eventType: StandardOutputValueProducedType,
                    event: {
                        valueId: null,
                        value: '2\r\n',
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
                    eventType: StandardOutputValueProducedType,
                    event: {
                        valueId: null,
                        value: '3\r\n',
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
                    eventType: CommandHandledType,
                    event: {},
                    token
                }
            ]
        }));
        let client = clientMapper.getOrAddClient({ path: 'test/path' });
        let outputs: Array<CellOutput> = [];
        await client.execute(code, 'csharp', cellOutput => {
            outputs.push(cellOutput);
            if (outputs.length === 3) {
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
            }
        }, token);
    });
});

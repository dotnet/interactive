import { expect } from 'chai';

import { ClientMapper } from './../../clientMapper';
import { CellOutputKind, InteractiveNotebook } from '../../interactiveNotebook';
import { TestClientAdapter } from './testClientAdapter';

describe('Notebook tests', () => {
    for (let language of ['csharp', 'fsharp']) {
        it(`executes and returns expected value: ${language}`, async () => {
            let code = '1+1';
            let clientMapper = new ClientMapper(targetKernelName => new TestClientAdapter(targetKernelName, {
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
            let client = clientMapper.addClient('csharp', { path: 'test/path' });
            let outputs = await InteractiveNotebook.execute(code, client);
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
});

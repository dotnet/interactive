import { expect } from 'chai';

import { StdioKernelTransport } from '../../../stdioKernelTransport';
import { ClientMapper } from '../../../clientMapper';
import { CellOutput, CellOutputKind } from '../../../interfaces/vscode';
import { RecordingChannel } from '../../RecordingOutputChannel';


suite('Extension Test Suite', () => {
    test('Execute against real kernel', async () => {
        let processStart = {
            command: 'dotnet',
            args: [
                'interactive',
                'stdio',
                '--http-port-range',
                '1000-3000'
            ],
            workingDirectory: __dirname
        };
        let clientMapper = new ClientMapper(() => new StdioKernelTransport(processStart, new RecordingChannel()));
        let client = clientMapper.getOrAddClient({ path: 'some/path' });
        let code = '1+1';
        let result: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs);
        expect(result).to.deep.equal([
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            }
        ]);
    }).timeout(10000);
});

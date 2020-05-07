import { expect } from 'chai';

import { StdioKernelTransport } from '../../../stdioKernelTransport';
import { ClientMapper } from '../../../clientMapper';
import { CellOutputKind } from '../../../interfaces/vscode';

suite('Extension Test Suite', () => {
    test('Execute against real kernel', async () => {
        let clientMapper = new ClientMapper(() => new StdioKernelTransport());
        let client = clientMapper.getOrAddClient({ path: 'some/path' });
        let code = '1+1';
        await client.execute('csharp', code, cellOutput => {
            expect(cellOutput).to.deep.equal({
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            });
        });
    }).timeout(10000);
});

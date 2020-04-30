import { expect } from 'chai';

import { StdioClientAdapter } from '../../../stdioClientAdapter';
import { ClientMapper } from '../../../clientMapper';
import { CellOutputKind, InteractiveNotebook } from '../../../interactiveNotebook';

suite('Extension Test Suite', () => {
    test('Execute against real kernel', async () => {
        let clientMapper = new ClientMapper(targetKernelName => new StdioClientAdapter(targetKernelName));
        let client = clientMapper.addClient('csharp', { path: 'some/path' });
        let code = '1+1';
        let outputs = await InteractiveNotebook.execute(code, client);
            expect(outputs).to.deep.equal([
                {
                    outputKind: CellOutputKind.Rich,
                    data: {
                        'text/html': '2'
                    }
                }
            ]);
    }).timeout(10000);
});

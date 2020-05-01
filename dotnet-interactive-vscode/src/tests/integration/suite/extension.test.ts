import { expect } from 'chai';

import { StdioClientAdapter } from '../../../stdioClientAdapter';
import { ClientMapper } from '../../../clientMapper';
import { execute } from '../../../interactiveNotebook';
import { CellOutputKind } from '../../../interfaces/vscode';

suite('Extension Test Suite', () => {
    test('Execute against real kernel', async () => {
        let clientMapper = new ClientMapper(() => new StdioClientAdapter());
        let client = clientMapper.addClient({ path: 'some/path' });
        let code = '1+1';
        let outputs = await execute('csharp', code, client);
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

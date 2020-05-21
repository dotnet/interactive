// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { StdioKernelTransport } from '../../../stdioKernelTransport';
import { ClientMapper } from '../../../clientMapper';
import { CellOutput, CellOutputKind } from '../../../interfaces/vscode';
import { RecordingChannel } from '../../RecordingOutputChannel';

describe('Extension Test Suite', () => {
    it('Execute against real kernel', async () => {
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
        let clientMapper = new ClientMapper(notebookPath => new StdioKernelTransport(processStart, notebookPath, new RecordingChannel()));
        let client = clientMapper.getOrAddClient({ fsPath: 'some/path' });
        let code = '1+1';
        let result: Array<CellOutput> = [];
        await client.execute(code, 'csharp', outputs => result = outputs);
        expect(result).to.deep.include(
            {
                outputKind: CellOutputKind.Rich,
                data: {
                    'text/html': '2'
                }
            }
        );
    }).timeout(20000);
});

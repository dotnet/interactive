// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../../interfaces/contracts';
import * as fs from 'fs';
import * as path from 'path';
import * as tmp from 'tmp';
import * as vscodeLike from '../../interfaces/vscode-like';
import { createOutput } from '../../utilities';

export function withFakeGlobalStorageLocation(createLocation: boolean, callback: { (globalStoragePath: string): Promise<void> }) {
    return new Promise<void>((resolve, reject) => {
        tmp.dir({ unsafeCleanup: true }, (err, dir) => {
            if (err) {
                reject();
                throw err;
            }

            // VS Code doesn't guarantee that the global storage path is present, so we have to go one directory deeper
            let globalStoragePath = path.join(dir, 'globalStoragePath');
            if (createLocation) {
                fs.mkdirSync(globalStoragePath);
            }

            callback(globalStoragePath).then(() => {
                resolve();
            });
        });
    });
}

export function createKernelTransportConfig(kernelTransportCreator: (notebookUri: vscodeLike.Uri) => Promise<contracts.KernelTransport>) {
    function defaultKernelTransportCreator(notebookUri: vscodeLike.Uri): Promise<contracts.KernelTransport> {
        throw new Error('Each test needs to override this.');
    }

    const encoder = new TextEncoder();

    const defaultClientMapperConfig = {
        kernelTransportCreator: defaultKernelTransportCreator,
        createErrorOutput: (message: string, outputId?: string) => {
            const errorItem = {
                mime: 'application/vnd.code.notebook.error',
                value: encoder.encode(JSON.stringify({
                    name: 'Error',
                    message,
                })),
            };
            const cellOutput = createOutput([errorItem], outputId);
            return cellOutput;
        },
        diagnosticChannel: undefined,
    };

    return {
        ...defaultClientMapperConfig,
        kernelTransportCreator
    };
}

export function decodeNotebookCellOutputs(outputs: vscodeLike.NotebookCellOutput[]): any[] {
    const decoder = new TextDecoder('utf-8');
    return outputs.map(o => ({
        ...o, outputs: o.outputs.map(oi => {
            let result = {
                ...oi,
                decodedValue: JSON.parse(decoder.decode(<Uint8Array>oi.value))
            };
            delete result.value;
            return result;
        })
    }));
}

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
            const errorItem: vscodeLike.NotebookCellOutputItem = {
                mime: 'application/vnd.code.notebook.error',
                data: encoder.encode(JSON.stringify({
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

export function decodeToString(data: Uint8Array): string {
    const decoder = new TextDecoder('utf-8');
    const decoded = decoder.decode(data);
    return decoded;
}

export function decodeNotebookCellOutputs(outputs: vscodeLike.NotebookCellOutput[]): any[] {
    const jsonLikeMimes = new Set<string>();
    jsonLikeMimes.add('application/json');
    jsonLikeMimes.add(vscodeLike.ErrorOutputMimeType);
    return outputs.map(o => ({
        ...o, items: o.items.map(oi => {
            const decoded = decodeToString(oi.data);
            let result = <any>{
                ...oi,
                decodedData: jsonLikeMimes.has(oi.mime) ? JSON.parse(decoded) : decoded,
            };
            delete result.data;
            return result;
        })
    }));
}

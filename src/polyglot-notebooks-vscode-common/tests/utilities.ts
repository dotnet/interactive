// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from '../../src/vscode-common/polyglot-notebooks/commandsAndEvents';
import * as fs from 'fs';
import * as path from 'path';
import * as tmp from 'tmp';
import * as vscodeLike from '../../src/vscode-common/interfaces/vscode-like';
import { createOutput } from '../../src/vscode-common/utilities';
import { DotnetInteractiveChannel } from '../../src/vscode-common/DotnetInteractiveChannel';
import { ClientMapperConfiguration } from '../../src/vscode-common/clientMapper';

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

export function createChannelConfig(channelCreator: (notebookUri: vscodeLike.Uri) => Promise<DotnetInteractiveChannel>): ClientMapperConfiguration {
    function defaultChannelCreator(notebookUri: vscodeLike.Uri): Promise<DotnetInteractiveChannel> {
        throw new Error('Each test needs to override this.');
    }

    const encoder = new TextEncoder();

    function configureKernel() {
        // noop
    }

    const defaultClientMapperConfig = {
        channelCreator: defaultChannelCreator,
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
        configureKernel,
    };

    return {
        ...defaultClientMapperConfig,
        channelCreator: async (uri: vscodeLike.Uri) => {
            const channel = await channelCreator(uri);
            return { channel, kernelReady: {} as commandsAndEvents.KernelReady }
        },
    };
}

export function decodeToString(data: Uint8Array): string {
    const decoder = new TextDecoder('utf-8');
    const decoded = decoder.decode(data);
    return decoded;
}

export function decodeNotebookCellOutputs(outputs: vscodeLike.NotebookCellOutput[]): any[] {
    const jsonLikeMimeTypes = new Set<string>();
    jsonLikeMimeTypes.add('application/json');
    jsonLikeMimeTypes.add(vscodeLike.ErrorOutputMimeType);
    return outputs.map(o => ({
        ...o, items: o.items.map(oi => {
            const decoded = decodeToString(oi.data);
            let result = {
                ...oi,
                decodedData: jsonLikeMimeTypes.has(oi.mime) ? JSON.parse(decoded) : decoded,
            } as any;
            delete result.data;
            return result;
        })
    }));
}

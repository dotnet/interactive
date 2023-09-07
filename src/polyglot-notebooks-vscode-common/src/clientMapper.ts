// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ErrorOutputCreator, InteractiveClient } from "./interactiveClient";
import { ReportChannel, Uri } from "./interfaces/vscode-like";
import { CompositeKernel } from './polyglot-notebooks/compositeKernel';
import { KernelCommandAndEventChannel } from "./DotnetInteractiveChannel";
import { KernelReady, Logger } from "./polyglot-notebooks";

export interface ClientMapperConfiguration {
    channelCreator: (notebookUri: Uri) => Promise<{ channel: KernelCommandAndEventChannel, kernelReady: KernelReady }>,
    createErrorOutput: ErrorOutputCreator,
    diagnosticChannel?: ReportChannel,
    configureKernel: (compositeKernel: CompositeKernel, notebookUri: Uri) => void,
}

export class ClientMapper {
    private clientMap: Map<string, Promise<InteractiveClient>> = new Map();
    private clientCreationCallbacks: ((uri: Uri, client: InteractiveClient) => void)[] = [];
    private clientDisposalCallbacks: ((uri: Uri, client: InteractiveClient) => void)[] = [];

    constructor(readonly config: ClientMapperConfiguration) {
    }

    private writeDiagnosticMessage(message: string) {
        if (this.config.diagnosticChannel) {
            this.config.diagnosticChannel.appendLine(message);
        }
    }

    static keyFromUri(uri: Uri): string {
        const key = uri.toString();
        if (key.startsWith("vscode-notebook-cell")) {
            throw new Error("vscode-notebook-cell is not supported");
        }
        return key;
    }

    tryGetClient(uri: Uri): Promise<InteractiveClient | undefined> {
        return new Promise(resolve => {
            const key = ClientMapper.keyFromUri(uri);
            const clientPromise = this.clientMap.get(key);
            if (clientPromise) {
                clientPromise.then(resolve);
            } else {
                resolve(undefined);
            }
        });
    }

    getOrAddClient(uri: Uri): Promise<InteractiveClient> {
        const key = ClientMapper.keyFromUri(uri);
        let clientPromise = this.clientMap.get(key);
        if (clientPromise === undefined) {
            this.writeDiagnosticMessage(`creating client for '${key}'`);
            clientPromise = new Promise<InteractiveClient>(async (resolve, reject) => {
                try {
                    const { channel, kernelReady } = await this.config.channelCreator(uri);
                    const config = {
                        channel: channel,
                        createErrorOutput: this.config.createErrorOutput,
                        kernelInfos: kernelReady?.kernelInfos || []
                    };
                    const client = new InteractiveClient(config);
                    this.config.configureKernel(client.kernel, uri);

                    for (const callback of this.clientCreationCallbacks) {
                        try {
                            callback(uri, client);
                        } catch (e) {
                            Logger.default.error(`Error executing client creation callback for ${uri.fsPath}: ${e}`);
                        }
                    }

                    resolve(client);
                } catch (err) {
                    reject(err);
                }
            });
            this.clientMap.set(key, clientPromise);
        }

        return clientPromise;
    }

    onClientCreate(callBack: (uri: Uri, client: InteractiveClient) => void) {
        this.clientCreationCallbacks.push(callBack);
    }

    onClientDispose(callback: (uri: Uri, client: InteractiveClient) => void) {
        this.clientDisposalCallbacks.push(callback);
    }

    reassociateClient(oldUri: Uri, newUri: Uri) {
        const oldKey = ClientMapper.keyFromUri(oldUri);
        const newKey = ClientMapper.keyFromUri(newUri);
        if (oldKey === newKey) {
            // no change
            return;
        }

        const client = this.clientMap.get(oldKey);
        if (!client) {
            // no old client found, nothing to do
            return;
        }

        this.clientMap.set(newKey, client);
        this.clientMap.delete(oldKey);
    }

    closeClient(uri: Uri, disposeClient: boolean = true) {
        const key = ClientMapper.keyFromUri(uri);
        const clientPromise = this.clientMap.get(key);
        if (clientPromise) {
            this.writeDiagnosticMessage(`closing client for '${key}', disposing = ${disposeClient}`);
            this.clientMap.delete(key);
            if (disposeClient) {
                clientPromise.then(client => {
                    for (const callback of this.clientDisposalCallbacks) {
                        try {
                            callback(uri, client);
                        } catch (e) {
                            Logger.default.error(`Error executing client disposal callback for ${uri.fsPath}: ${e}`);
                        }
                    }

                    client.dispose();
                });
            };
        }
    }

    isDotNetClient(uri: Uri): boolean {
        const key = ClientMapper.keyFromUri(uri);
        return this.clientMap.has(key);
    }
}

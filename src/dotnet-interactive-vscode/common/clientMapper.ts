// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelTransport } from './interfaces/contracts';
import { InteractiveClient } from "./interactiveClient";
import { ReportChannel, Uri } from "./interfaces/vscode-like";

export class ClientMapper {
    private clientMap: Map<string, Promise<InteractiveClient>> = new Map();
    private clientCreationCallbackMap: Map<string, (client: InteractiveClient) => Promise<void>> = new Map();

    constructor(readonly kernelTransportCreator: (notebookUri: Uri) => Promise<KernelTransport>, readonly diagnosticChannel?: ReportChannel) {
    }

    private writeDiagnosticMessage(message: string) {
        if (this.diagnosticChannel) {
            this.diagnosticChannel.appendLine(message);
        }
    }

    static keyFromUri(uri: Uri): string {
        return uri.toString();
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
                    const transport = await this.kernelTransportCreator(uri);
                    const client = new InteractiveClient(transport);

                    let onCreate = this.clientCreationCallbackMap.get(key);
                    if (onCreate) {
                        await onCreate(client);
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

    onClientCreate(uri: Uri, callBack: (client: InteractiveClient) => Promise<void>) {
        let key = ClientMapper.keyFromUri(uri);
        this.clientCreationCallbackMap.set(key, callBack);
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

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelTransport } from "./contracts";
import { InteractiveClient } from "./interactiveClient";
import { Uri } from "./interfaces/vscode";

export class ClientMapper {
    private clientMap: Map<string, Promise<InteractiveClient>> = new Map();
    private clientCreationCallbackMap: Map<string, (client: InteractiveClient) => Promise<void>> = new Map();

    constructor(readonly kernelTransportCreator: (notebookPath: string) => Promise<KernelTransport>) {
    }

    static keyFromUri(uri: Uri): string {
        return uri.fsPath;
    }

    getOrAddClient(uri: Uri): Promise<InteractiveClient> {
        let key = ClientMapper.keyFromUri(uri);
        let clientPromise = this.clientMap.get(key);
        if (clientPromise === undefined) {
            clientPromise = new Promise<InteractiveClient>(async resolve => {
                const transport = await this.kernelTransportCreator(uri.fsPath);
                const client = new InteractiveClient(transport);

                let onCreate = this.clientCreationCallbackMap.get(key);
                if (onCreate) {
                    await onCreate(client);
                }

                resolve(client);
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

    closeClient(uri: Uri) {
        let key = ClientMapper.keyFromUri(uri);
        let clientPromise = this.clientMap.get(key);
        if (clientPromise) {
            this.clientMap.delete(key);
            clientPromise.then(client => {
                client.dispose();
            });
        }
    }

    isDotNetClient(uri: Uri): boolean {
        return this.clientMap.has(uri.fsPath);
    }
}

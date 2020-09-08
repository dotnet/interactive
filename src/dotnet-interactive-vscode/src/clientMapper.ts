// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelTransport } from "./contracts";
import { InteractiveClient } from "./interactiveClient";
import { Uri } from "./interfaces/vscode";

export class ClientMapper {
    private clientMap: Map<string, InteractiveClient> = new Map();

    constructor(readonly kernelTransportCreator: (notebookPath: string) => Promise<KernelTransport>) {
    }

    static keyFromUri(uri: Uri): string {
        return uri.fsPath;
    }

    async getOrAddClient(uri: Uri): Promise<InteractiveClient> {
        let key = ClientMapper.keyFromUri(uri);
        let client = this.clientMap.get(key);
        if (client === undefined) {
            const transport = await this.kernelTransportCreator(uri.fsPath);
            client = new InteractiveClient(transport);
            this.clientMap.set(key, client);
        }

        return client;
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
        let client = this.clientMap.get(key);
        if (client) {
            client.dispose();
            this.clientMap.delete(key);
        }
    }

    isDotNetClient(uri: Uri): boolean {
        return this.clientMap.has(uri.fsPath);
    }
}

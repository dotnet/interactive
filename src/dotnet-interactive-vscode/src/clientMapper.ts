// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InteractiveClient } from "./interactiveClient";
import { Uri } from "./interfaces/vscode";
import { KernelTransportCreationResult } from "./interfaces";

export class ClientMapper {
    private clientMap: Map<string, InteractiveClient> = new Map();

    constructor(readonly kernelTransportCreator: (notebookPath: string) => KernelTransportCreationResult) {
    }

    static keyFromUri(uri: Uri): string {
        return uri.fsPath;
    }

    async getOrAddClient(uri: Uri): Promise<InteractiveClient> {
        let key = ClientMapper.keyFromUri(uri);
        let client = this.clientMap.get(key);
        if (client === undefined) {
            const creationResult = this.kernelTransportCreator(uri.fsPath);
            client = new InteractiveClient(creationResult.transport);
            this.clientMap.set(key, client);
            await creationResult.initialization;
        }

        return client;
    }

    closeClient(uri: Uri) {
        let key = ClientMapper.keyFromUri(uri);
        let client = this.clientMap.get(key);
        if (client) {
            client.dispose();
            this.clientMap.delete(key);
        }
    }

    closeAllClients() {
        for (let fsPath of this.clientMap.keys()) {
            this.closeClient({ fsPath });
        }
    }
}

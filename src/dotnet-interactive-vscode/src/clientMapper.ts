// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelTransport } from "./contracts";
import { InteractiveClient } from "./interactiveClient";
import { Uri } from "./interfaces/vscode";

export class ClientMapper {
    private clientMap: Map<string, InteractiveClient> = new Map();

    constructor(readonly kernelTransportCreator: {(notebookPath: string): KernelTransport}) {
    }

    static keyFromUri(uri: Uri): string {
        return uri.fsPath;
    }

    getOrAddClient(uri: Uri): InteractiveClient {
        let key = ClientMapper.keyFromUri(uri);
        let client = this.clientMap.get(key);
        if (client === undefined) {
            client = new InteractiveClient(this.kernelTransportCreator(uri.fsPath));
            this.clientMap.set(key, client);
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

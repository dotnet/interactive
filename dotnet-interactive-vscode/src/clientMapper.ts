import * as vscode from 'vscode';
import { InteractiveClient } from "./interactiveClient";

export class ClientMapper {
    private clientMap: Map<string, InteractiveClient> = new Map();

    private static keyFromUri(uri: vscode.Uri): string {
        // TODO: `path` property can look like:
        // TODO:   /c:/path/to/notebook/file, cell X
        // TODO: need access to `vscode.CellUri` to properly parse
        // TODO: until then, this'll have to do
        let cellIdx = uri.path.lastIndexOf(', cell ');
        if (cellIdx < 0) {
            return uri.path;
        } else {
            return uri.path.substr(0, cellIdx);
        }
    }

    addClient(targetKernelName: string, uri: vscode.Uri): InteractiveClient {
        let client = new InteractiveClient(targetKernelName);
        this.clientMap.set(ClientMapper.keyFromUri(uri), client);
        return client;
    }

    getClient(uri: vscode.Uri): InteractiveClient {
        let client = this.clientMap.get(ClientMapper.keyFromUri(uri));
        if (client === undefined) {
            throw `Unable to find interactive client for uri '${uri}'`;
        }

        return client;
    }
}

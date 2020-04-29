import { InteractiveClient } from "./interactiveClient";
import { ClientAdapterBase } from './clientAdapterBase';

interface HasPath {
    path: string;
}

export class ClientMapper {
    private clientMap: Map<string, InteractiveClient> = new Map();

    constructor(readonly clientAdapterCreator: {(targetKernelName: string): ClientAdapterBase}) {
    }

    static keyFromUri(uri: HasPath): string {
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

    addClient(targetKernelName: string, uri: HasPath) {
        let client = new InteractiveClient(this.clientAdapterCreator(targetKernelName));
        this.clientMap.set(ClientMapper.keyFromUri(uri), client);
    }

    getClient(uri: HasPath): InteractiveClient | undefined {
        return this.clientMap.get(ClientMapper.keyFromUri(uri));
    }
}

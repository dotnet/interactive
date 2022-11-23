"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.ClientMapper = void 0;
const interactiveClient_1 = require("./interactiveClient");
class ClientMapper {
    constructor(config) {
        this.config = config;
        this.clientMap = new Map();
        this.clientCreationCallbacks = [];
    }
    writeDiagnosticMessage(message) {
        if (this.config.diagnosticChannel) {
            this.config.diagnosticChannel.appendLine(message);
        }
    }
    static keyFromUri(uri) {
        const key = uri.toString();
        if (key.startsWith("vscode-notebook-cell")) {
            throw new Error("vscode-notebook-cell is not supported");
        }
        return key;
    }
    tryGetClient(uri) {
        return new Promise(resolve => {
            const key = ClientMapper.keyFromUri(uri);
            const clientPromise = this.clientMap.get(key);
            if (clientPromise) {
                clientPromise.then(resolve);
            }
            else {
                resolve(undefined);
            }
        });
    }
    getOrAddClient(uri) {
        const key = ClientMapper.keyFromUri(uri);
        let clientPromise = this.clientMap.get(key);
        if (clientPromise === undefined) {
            this.writeDiagnosticMessage(`creating client for '${key}'`);
            clientPromise = new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
                try {
                    const channel = yield this.config.channelCreator(uri);
                    const config = {
                        channel: channel,
                        createErrorOutput: this.config.createErrorOutput,
                    };
                    const client = new interactiveClient_1.InteractiveClient(config);
                    this.config.configureKernel(client.kernel, uri);
                    for (const callback of this.clientCreationCallbacks) {
                        callback(uri, client);
                    }
                    resolve(client);
                }
                catch (err) {
                    reject(err);
                }
            }));
            this.clientMap.set(key, clientPromise);
        }
        return clientPromise;
    }
    onClientCreate(callBack) {
        this.clientCreationCallbacks.push(callBack);
    }
    reassociateClient(oldUri, newUri) {
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
    closeClient(uri, disposeClient = true) {
        const key = ClientMapper.keyFromUri(uri);
        const clientPromise = this.clientMap.get(key);
        if (clientPromise) {
            this.writeDiagnosticMessage(`closing client for '${key}', disposing = ${disposeClient}`);
            this.clientMap.delete(key);
            if (disposeClient) {
                clientPromise.then(client => {
                    client.dispose();
                });
            }
            ;
        }
    }
    isDotNetClient(uri) {
        const key = ClientMapper.keyFromUri(uri);
        return this.clientMap.has(key);
    }
}
exports.ClientMapper = ClientMapper;
//# sourceMappingURL=clientMapper.js.map
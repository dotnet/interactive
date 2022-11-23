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
exports.MessageClient = void 0;
const commonUtilities = require("./utilities");
// A `MessageClient` wraps a `LineAdapter` by writing JSON objects with an `id` field and returns the corresponding JSON line with the same `id` field.
class MessageClient {
    constructor(lineAdapter) {
        this.lineAdapter = lineAdapter;
        this.requestListeners = new Map();
        this.lineAdapter.subscribeToLines((line) => {
            try {
                const message = commonUtilities.parse(line);
                if (typeof message.id === 'string') {
                    const responseId = message.id;
                    const responseCallback = this.requestListeners.get(responseId);
                    if (responseCallback) {
                        responseCallback(message);
                    }
                }
            }
            catch (_) {
            }
        });
    }
    sendMessageAndGetResponse(message) {
        return new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
            this.requestListeners.set(message.id, (response) => {
                this.requestListeners.delete(message.id);
                resolve(response);
            });
            try {
                const body = commonUtilities.stringify(message);
                this.lineAdapter.writeLine(body);
            }
            catch (e) {
                reject(e);
            }
        }));
    }
}
exports.MessageClient = MessageClient;
//# sourceMappingURL=messageClient.js.map
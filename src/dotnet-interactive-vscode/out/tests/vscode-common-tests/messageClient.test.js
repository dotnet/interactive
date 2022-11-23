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
const chai_1 = require("chai");
const messageClient_1 = require("../../src/vscode-common/messageClient");
class KeyValuePairLineAdapter {
    constructor(idToResponseMap) {
        this.idToResponseMap = idToResponseMap;
        this.callbacks = [];
    }
    subscribeToLines(callback) {
        this.callbacks.push(callback);
    }
    writeLine(line) {
        try {
            const json = JSON.parse(line);
            const responses = this.idToResponseMap[json.id];
            responses.forEach(response => this.callbacks.forEach(callback => callback(response)));
        }
        catch (_) {
        }
    }
}
describe('MessageClient tests', () => {
    it(`only the response with the matching id is returned`, () => __awaiter(void 0, void 0, void 0, function* () {
        const messageClient = new messageClient_1.MessageClient(new KeyValuePairLineAdapter({
            '1': [
                '{"id":"0","reply":"this is the wrong reply"}',
                '{"id":"1","reply":"this is the correct reply"}',
                '{"id":"2","reply":"this is also the wrong reply"}',
            ]
        }));
        const reply = yield messageClient.sendMessageAndGetResponse({ id: '1' });
        (0, chai_1.expect)(reply).to.deep.equal({
            id: '1',
            reply: 'this is the correct reply',
        });
    }));
    it(`responses that don't parse as JSON are skipped`, () => __awaiter(void 0, void 0, void 0, function* () {
        const messageClient = new messageClient_1.MessageClient(new KeyValuePairLineAdapter({
            '1': [
                '{"id":"1","reply":"this is the wrong reply and does not parse as JSON because it is missing the end curly"',
                `this doesn't even look like JSON`,
                '{"id":"1","reply":"this is the correct reply"}',
            ]
        }));
        const reply = yield messageClient.sendMessageAndGetResponse({ id: '1' });
        (0, chai_1.expect)(reply).to.deep.equal({
            id: '1',
            reply: 'this is the correct reply',
        });
    }));
});
//# sourceMappingURL=messageClient.test.js.map
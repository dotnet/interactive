// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';

import { MessageClient } from '../../src/vscode-common/messageClient';

class KeyValuePairLineAdapter {

    private callbacks: ((line: string) => void)[] = [];

    constructor(private readonly idToResponseMap: { [id: string]: string[] }) {
    }

    subscribeToLines(callback: (line: string) => void) {
        this.callbacks.push(callback);
    }

    writeLine(line: string) {
        try {
            const json = JSON.parse(line);
            const responses = this.idToResponseMap[json.id];
            responses.forEach(response => this.callbacks.forEach(callback => callback(response)));
        } catch (_) {
        }
    }
}

describe('MessageClient tests', () => {

    it(`only the response with the matching id is returned`, async () => {
        const messageClient = new MessageClient(new KeyValuePairLineAdapter({
            '1': [
                '{"id":"0","reply":"this is the wrong reply"}',
                '{"id":"1","reply":"this is the correct reply"}',
                '{"id":"2","reply":"this is also the wrong reply"}',
            ]
        }));
        const reply = await messageClient.sendMessageAndGetResponse({ id: '1' });
        expect(reply).to.deep.equal({
            id: '1',
            reply: 'this is the correct reply',
        });
    });

    it(`responses that don't parse as JSON are skipped`, async () => {
        const messageClient = new MessageClient(new KeyValuePairLineAdapter({
            '1': [
                '{"id":"1","reply":"this is the wrong reply and does not parse as JSON because it is missing the end curly"',
                `this doesn't even look like JSON`,
                '{"id":"1","reply":"this is the correct reply"}',
            ]
        }));
        const reply = await messageClient.sendMessageAndGetResponse({ id: '1' });
        expect(reply).to.deep.equal({
            id: '1',
            reply: 'this is the correct reply',
        });
    });

});

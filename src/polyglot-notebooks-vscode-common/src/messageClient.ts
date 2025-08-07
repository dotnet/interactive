// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commonUtilities from './utilities';
import { LineAdapter } from './childProcessLineAdapter';

// A `MessageClient` wraps a `LineAdapter` by writing JSON objects with an `id` field and returns the corresponding JSON line with the same `id` field.
export class MessageClient {
    private requestListeners: Map<string, (response: any) => void> = new Map();

    constructor(private readonly lineAdapter: LineAdapter) {
        this.lineAdapter.subscribeToLines((line: string) => {
            try {
                const message = commonUtilities.parse(line);
                if (typeof message.id === 'string') {
                    const responseId = message.id as string;
                    const responseCallback = this.requestListeners.get(responseId);
                    if (responseCallback) {
                        responseCallback(message);
                    }
                }
            } catch (_) {
            }
        });
    }

    sendMessageAndGetResponse(message: { id: string, [key: string]: any }): Promise<any> {
        return new Promise<any>(async (resolve, reject) => {
            this.requestListeners.set(message.id, (response: any) => {
                this.requestListeners.delete(message.id);
                resolve(response);
            });

            try {
                const body = commonUtilities.stringify(message);
                this.lineAdapter.writeLine(body);
            } catch (e) {
                reject(e);
            }
        });
    }
}

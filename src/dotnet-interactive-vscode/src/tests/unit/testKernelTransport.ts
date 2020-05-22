// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelCommand, KernelCommandType, KernelEventType, KernelEventEnvelopeObserver, DisposableSubscription } from "../../contracts";

// Replays all events given to it
export class TestKernelTransport {
    private theObserver: KernelEventEnvelopeObserver | undefined;

    constructor(readonly fakedEventEnvelopes: { [key: string]: {eventType: KernelEventType, event: any, token: string}[] }) {
    }

    static create(fakedEventEnvelopes: { [key: string]: {eventType: KernelEventType, event: any, token: string}[] }): Promise<TestKernelTransport> {
        return new Promise<TestKernelTransport>((resolve, reject) => {
            let testTransport = new TestKernelTransport(fakedEventEnvelopes);
            resolve(testTransport);
        });
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        this.theObserver = observer;
        return {
            dispose: () => {}
        };
    }

    async submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {
        let eventEnvelopesToReturn = this.fakedEventEnvelopes[commandType];
        if (this.theObserver) {
            for (let envelope of eventEnvelopesToReturn) {
                this.theObserver({
                    eventType: envelope.eventType,
                    event: envelope.event,
                    command: {
                        token: envelope.token,
                        commandType,
                        command
                    }
                });
            }
        }
    }

    dispose() {
        // noop
    }
}

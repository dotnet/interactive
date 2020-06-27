// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelCommand, KernelCommandType, KernelEventType, KernelEventEnvelopeObserver, DisposableSubscription, KernelEvent } from "../../contracts";

// Replays all events given to it
export class TestKernelTransport {
    private theObserver: KernelEventEnvelopeObserver | undefined;
    private fakedCommandCounter: Map<string, number> = new Map<string, number>();

    constructor(readonly fakedEventEnvelopes: { [key: string]: {eventType: KernelEventType, event: KernelEvent, token: string}[] }) {
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        this.theObserver = observer;
        return {
            dispose: () => {}
        };
    }

    async submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {
        // find bare fake command events
        let eventEnvelopesToReturn = this.fakedEventEnvelopes[commandType];
        if (!eventEnvelopesToReturn) {
            // check for numbered variants
            let counter = this.fakedCommandCounter.get(commandType);
            if (!counter) {
                // first encounter
                counter = 1;
            }

            // and increment for next time
            this.fakedCommandCounter.set(commandType, counter + 1);

            eventEnvelopesToReturn = this.fakedEventEnvelopes[`${commandType}#${counter}`];
            if (!eventEnvelopesToReturn) {
                // couldn't find numbered event names
                throw new Error(`Unable to find events for command '${commandType}'.`);
            }
        }

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

    waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    dispose() {
        // noop
    }
}

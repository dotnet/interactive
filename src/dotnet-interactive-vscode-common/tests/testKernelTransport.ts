// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from '../../src/vscode-common/dotnet-interactive/contracts';
import { KernelConnector } from '../../src/vscode-common/KernelConnector';

// Replays all events given to it
export class TestKernelTransport implements KernelConnector {
    private theObserver: contracts.KernelEventEnvelopeObserver | undefined;
    private fakedCommandCounter: Map<string, number> = new Map<string, number>();

    constructor(readonly fakedEventEnvelopes: { [key: string]: { eventType: contracts.KernelEventType, event: contracts.KernelEvent, token: string }[] }) {
    }

    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): contracts.DisposableSubscription {
        this.theObserver = observer;
        return {
            dispose: () => { }
        };
    }

    setCommandHandler(handler: contracts.KernelCommandEnvelopeHandler) {

    }

    async submitCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        // find bare fake command events
        let eventEnvelopesToReturn = this.fakedEventEnvelopes[commandEnvelope.commandType];
        if (!eventEnvelopesToReturn) {
            // check for numbered variants
            let counter = this.fakedCommandCounter.get(commandEnvelope.commandType);
            if (!counter) {
                // first encounter
                counter = 1;
            }

            // and increment for next time
            this.fakedCommandCounter.set(commandEnvelope.commandType, counter + 1);

            eventEnvelopesToReturn = this.fakedEventEnvelopes[`${commandEnvelope.commandType}#${counter}`];
            if (!eventEnvelopesToReturn) {
                // couldn't find numbered event names
                throw new Error(`Unable to find events for command '${commandEnvelope.commandType}'.`);
            }
        }

        if (this.theObserver) {
            for (let envelope of eventEnvelopesToReturn) {
                this.theObserver({
                    eventType: envelope.eventType,
                    event: envelope.event,
                    command: {
                        ...commandEnvelope,
                        token: envelope.token
                    }
                });
            }
        }
    }

    publishKernelEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void> {
        throw new Error("Stdio channel doesn't currently support a back channel");
    }

    waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    dispose() {
        // noop
    }
}

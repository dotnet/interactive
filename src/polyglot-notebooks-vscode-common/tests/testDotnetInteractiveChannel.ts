// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, KernelCommandOrEventEnvelope, isKernelCommandEnvelope, KernelCommandAndEventReceiver, KernelCommandAndEventSender } from '../../src/vscode-common/polyglot-notebooks';
import * as contracts from '../../src/vscode-common/polyglot-notebooks/commandsAndEvents';
import { DotnetInteractiveChannel } from '../../src/vscode-common/DotnetInteractiveChannel';
import * as rxjs from 'rxjs';

// Replays all events given to it
export class TestDotnetInteractiveChannel implements DotnetInteractiveChannel {
    private fakedCommandCounter: Map<string, number> = new Map<string, number>();
    private _senderSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;
    private _receiverSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;
    constructor(readonly fakedEventEnvelopes: { [key: string]: { eventType: contracts.KernelEventType, event: contracts.KernelEvent, token: string }[] }) {
        this._senderSubject = new rxjs.Subject<KernelCommandOrEventEnvelope>();
        this._receiverSubject = new rxjs.Subject<KernelCommandOrEventEnvelope>();

        this.sender = KernelCommandAndEventSender.FromObserver(this._senderSubject);
        this.receiver = KernelCommandAndEventReceiver.FromObservable(this._receiverSubject);

        this._senderSubject.subscribe({
            next: (envelope) => {
                if (isKernelCommandEnvelope(envelope)) {
                    this.submitCommand(envelope);
                }
            }
        });

    }
    sender: IKernelCommandAndEventSender;
    receiver: IKernelCommandAndEventReceiver;


    private submitCommand(commandEnvelope: contracts.KernelCommandEnvelope) {
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

        for (let envelope of eventEnvelopesToReturn) {
            this._receiverSubject.next({
                eventType: envelope.eventType,
                event: envelope.event,
                command: {
                    ...commandEnvelope,
                    token: envelope.token
                }
            });
        }
    }


    waitForReady(): Promise<contracts.KernelReady> {
        return Promise.resolve({ kernelInfos: [] });
    }

    dispose() {
        // noop
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, KernelCommandOrEventEnvelope, isKernelCommandEnvelope, KernelCommandAndEventReceiver, KernelCommandAndEventSender } from '../../src/vscode-common/polyglot-notebooks';
import * as commandsAndEvents from '../../src/vscode-common/polyglot-notebooks/commandsAndEvents';
import { DotnetInteractiveChannel } from '../../src/vscode-common/DotnetInteractiveChannel';
import * as rxjs from 'rxjs';

// Replays all events given to it
export class TestDotnetInteractiveChannel implements DotnetInteractiveChannel {
    private fakedCommandCounter: Map<string, number> = new Map<string, number>();
    private _senderSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;
    private _receiverSubject: rxjs.Subject<KernelCommandOrEventEnvelope>;
    constructor(readonly fakedEventEnvelopes: { [key: string]: { eventType: commandsAndEvents.KernelEventType, event: commandsAndEvents.KernelEvent }[] }) {
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


    private submitCommand(commandEnvelope: commandsAndEvents.KernelCommandEnvelope) {

        const commandClone = commandEnvelope.clone();
        // find bare fake command events
        let eventEnvelopesToReturn = this.fakedEventEnvelopes[commandClone.commandType];
        if (!eventEnvelopesToReturn) {
            // check for numbered variants
            let counter = this.fakedCommandCounter.get(commandClone.commandType);
            if (!counter) {
                // first encounter
                counter = 1;
            }

            // and increment for next time
            this.fakedCommandCounter.set(commandClone.commandType, counter + 1);

            eventEnvelopesToReturn = this.fakedEventEnvelopes[`${commandClone.commandType}#${counter}`];
            if (!eventEnvelopesToReturn) {
                // couldn't find numbered event names
                throw new Error(`Unable to find events for command '${commandClone.commandType}'.`);
            }
        }

        for (let envelope of eventEnvelopesToReturn) {
            const event = new commandsAndEvents.KernelEventEnvelope(envelope.eventType, envelope.event, commandClone);
            this._receiverSubject.next(event);
        }
    }


    waitForReady(): Promise<commandsAndEvents.KernelReady> {
        return Promise.resolve({ kernelInfos: [] });
    }

    dispose() {
        // noop
    }
}

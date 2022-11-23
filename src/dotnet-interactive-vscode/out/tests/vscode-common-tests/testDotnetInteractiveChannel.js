"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.TestDotnetInteractiveChannel = void 0;
const dotnet_interactive_1 = require("../../src/vscode-common/dotnet-interactive");
const rxjs = require("rxjs");
// Replays all events given to it
class TestDotnetInteractiveChannel {
    constructor(fakedEventEnvelopes) {
        this.fakedEventEnvelopes = fakedEventEnvelopes;
        this.fakedCommandCounter = new Map();
        this._senderSubject = new rxjs.Subject();
        this._receiverSubject = new rxjs.Subject();
        this.sender = dotnet_interactive_1.KernelCommandAndEventSender.FromObserver(this._senderSubject);
        this.receiver = dotnet_interactive_1.KernelCommandAndEventReceiver.FromObservable(this._receiverSubject);
        this._senderSubject.subscribe({
            next: (envelope) => {
                if ((0, dotnet_interactive_1.isKernelCommandEnvelope)(envelope)) {
                    this.submitCommand(envelope);
                }
            }
        });
    }
    submitCommand(commandEnvelope) {
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
                command: Object.assign(Object.assign({}, commandEnvelope), { token: envelope.token })
            });
        }
    }
    waitForReady() {
        return Promise.resolve();
    }
    dispose() {
        // noop
    }
}
exports.TestDotnetInteractiveChannel = TestDotnetInteractiveChannel;
//# sourceMappingURL=testDotnetInteractiveChannel.js.map
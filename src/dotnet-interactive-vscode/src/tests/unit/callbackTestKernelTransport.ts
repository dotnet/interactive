// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelCommand, KernelCommandType, KernelEventEnvelopeObserver, DisposableSubscription, KernelCommandEnvelopeObserver, KernelEventEnvelope, KernelTransport } from 'dotnet-interactive-vscode-interfaces/out/contracts';

// executes the given callback for the specified commands
export class CallbackTestKernelTransport implements KernelTransport {
    private theObserver: KernelEventEnvelopeObserver | undefined;

    constructor(readonly fakedCommandCallbacks: { [key: string]: () => KernelEventEnvelope }) {
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        this.theObserver = observer;
        return {
            dispose: () => { }
        };
    }

    subscribeToCommands(observer: KernelCommandEnvelopeObserver): DisposableSubscription {
        // Currently, the back channel for client-side kernels is only implemented by the SignalR
        // transport, so tests in this project don't call this.
        throw new Error("Stdio channel doesn't currently support a back channel");
    }

    async submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {
        return new Promise((resolve, reject) => {
            const commandCallback = this.fakedCommandCallbacks[commandType];
            if (!commandCallback) {
                reject(`No callback specified for command '${commandType}'`);
                return;
            }

            const eventEnvelope = commandCallback();
            if (this.theObserver) {
                this.theObserver(eventEnvelope);
            }

            resolve();
        });
    }

    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void> {
        throw new Error("Stdio channel doesn't currently support a back channel");
    }

    waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    dispose() {
        // noop
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import * as connection from "./connection";
import * as logger from "./logger";
import * as disposable from "./disposables";
import * as promiseCompletionSource from "./promiseCompletionSource";


export class GenericChannel implements connection.KernelCommandAndEventChannel {

    private stillRunning: promiseCompletionSource.PromiseCompletionSource<number>;
    private commandHandler: contracts.KernelCommandEnvelopeHandler = () => Promise.resolve();
    private eventSubscribers: Array<contracts.KernelEventEnvelopeObserver> = [];

    constructor(private readonly messageSender: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void>, private readonly messageReceiver: () => Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>) {

        this.stillRunning = new promiseCompletionSource.PromiseCompletionSource<number>();
    }

    dispose(): void {
        this.stop();
    }

    async run(): Promise<void> {
        while (true) {
            let message = await Promise.race([this.messageReceiver(), this.stillRunning.promise]);
            if (typeof message === 'number') {
                return;
            }
            if (connection.isKernelCommandEnvelope(message)) {
                this.commandHandler(message);
            } else if (connection.isKernelEventEnvelope(message)) {
                for (let i = this.eventSubscribers.length - 1; i >= 0; i--) {
                    this.eventSubscribers[i](message);
                }
            }
        }
    }

    stop() {
        this.stillRunning.resolve(-1);
    }


    submitCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        return this.messageSender(commandEnvelope);
    }

    publishKernelEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void> {
        return this.messageSender(eventEnvelope);
    }

    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): disposable.DisposableSubscription {
        this.eventSubscribers.push(observer);
        return {
            dispose: () => {
                const i = this.eventSubscribers.indexOf(observer);
                if (i >= 0) {
                    this.eventSubscribers.splice(i, 1);
                }
            }
        };
    }

    setCommandHandler(handler: contracts.KernelCommandEnvelopeHandler) {
        this.commandHandler = handler;
    }
}

export class CommandAndEventReceiver {
    private _waitingOnMessages: promiseCompletionSource.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> | null = null;
    private readonly _envelopeQueue: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];

    public delegate(commandOrEvent: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) {
        if (this._waitingOnMessages) {
            let capturedMessageWaiter = this._waitingOnMessages;
            this._waitingOnMessages = null;

            capturedMessageWaiter.resolve(commandOrEvent);
        } else {

            this._envelopeQueue.push(commandOrEvent);
        }
    }

    public read(): Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> {
        let envelope = this._envelopeQueue.shift();
        if (envelope) {
            return Promise.resolve<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(envelope);
        }
        else {
            logger.Logger.default.info(`channel building promise awaiter`);
            this._waitingOnMessages = new promiseCompletionSource.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>();
            return this._waitingOnMessages.promise;
        }
    }
}

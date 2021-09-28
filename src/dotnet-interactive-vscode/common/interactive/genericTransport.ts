// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "../interfaces/contracts";
import * as utilities from "../interfaces/utilities";
import { Logger } from "../logger";

export function isPromiseCompletionSource<T>(obj: any): obj is PromiseCompletionSource<T> {
    return obj.promise
        && obj.resolve
        && obj.reject;
}

export class PromiseCompletionSource<T> {
    private _resolve: (value: T) => void = () => { };
    private _reject: (reason: any) => void = () => { };
    readonly promise: Promise<T>;

    constructor() {
        this.promise = new Promise<T>((resolve, reject) => {
            this._resolve = resolve;
            this._reject = reject;
        });
    }

    resolve(value: T) {
        this._resolve(value);
    }

    reject(reason: any) {
        this._reject(reason);
    }
}

export class GenericTransport implements contracts.Transport {

    private stillRunning: PromiseCompletionSource<number>;
    private commandHandler: KernelCommandEnvelopeHandler = () => Promise.resolve();
    private eventSubscribers: Array<contracts.KernelEventEnvelopeObserver> = [];

    constructor(private readonly messageSender: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void>, private readonly messageReceiver: () => Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>) {

        this.stillRunning = new PromiseCompletionSource<number>();
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
            if (utilities.isKernelCommandEnvelope(message)) {
                this.commandHandler(message);
            } else if (utilities.isKernelEventEnvelope(message)) {
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

    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): contracts.DisposableSubscription {
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

    setCommandHandler(handler: KernelCommandEnvelopeHandler) {
        this.commandHandler = handler;
    }
}

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
}

export class CommandAndEventReceiver {
    private _waitingOnMessages: PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> | null;
    private readonly _envelopeQueue: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];

    public publish(commandOrEvent: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) {
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
            Logger.default.info(`transport building promise awaiter`);
            this._waitingOnMessages = new PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>();
            return this._waitingOnMessages.promise;
        }
    }
}

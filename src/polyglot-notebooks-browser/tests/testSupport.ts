// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import * as rxjs from "rxjs";
import { DotnetInteractiveClient, KernelClientContainer } from "../src/polyglot-notebooks-interfaces";
import * as connection from "../src/polyglot-notebooks/connection";
import * as commandsAndEvents from "../src/polyglot-notebooks/commandsAndEvents";


export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return client as any as KernelClientContainer;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export class MockKernelCommandAndEventChannel {
    private static _counter = 0;
    public commandsSent: Array<commandsAndEvents.KernelCommandEnvelope>;
    public eventsPublished: Array<commandsAndEvents.KernelEventEnvelope>;
    private eventObservers: { [key: string]: commandsAndEvents.KernelEventEnvelopeObserver } = {};
    private commandHandlers: { [key: string]: commandsAndEvents.KernelCommandEnvelopeHandler } = {};

    public sender: connection.IKernelCommandAndEventSender;
    public receiver: connection.IKernelCommandAndEventReceiver;
    private _senderSubject: rxjs.Subject<connection.KernelCommandOrEventEnvelope>;
    private _receiverSubject: rxjs.Subject<connection.KernelCommandOrEventEnvelope>;

    constructor() {

        this.commandsSent = new Array<commandsAndEvents.KernelCommandEnvelope>();
        this.eventsPublished = new Array<commandsAndEvents.KernelEventEnvelope>();

        this._senderSubject = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        this._receiverSubject = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

        this._senderSubject.subscribe({
            next: (envelope) => {
                if (connection.isKernelCommandEnvelope(envelope)) {
                    this.commandsSent.push(envelope);
                } else if (connection.isKernelEventEnvelope(envelope)) {
                    this.eventsPublished.push(envelope);
                }
            }
        });

        this.sender = connection.KernelCommandAndEventSender.FromObserver(this._senderSubject);
        this.receiver = connection.KernelCommandAndEventReceiver.FromObservable(this._receiverSubject);
    }
    private static createToken() {
        return `token-${MockKernelCommandAndEventChannel._counter++}`
    }
    public subscribeToKernelEvents(observer: commandsAndEvents.KernelEventEnvelopeObserver) {
        let key = MockKernelCommandAndEventChannel.createToken();
        this.eventObservers[key] = observer;

        return {
            dispose: () => {
                delete this.eventObservers[key];
            }
        };
    }

    public setCommandHandler(handler: commandsAndEvents.KernelCommandEnvelopeHandler) {
        let key = MockKernelCommandAndEventChannel.createToken();
        this.commandHandlers[key] = handler;
    }

    public fakeIncomingSubmitCommand(envelope: commandsAndEvents.KernelCommandEnvelope) {
        this._receiverSubject.next(envelope);
    }

    publishKernelEvent(eventEnvelope: commandsAndEvents.KernelEventEnvelope): Promise<void> {
        this.eventsPublished.push(eventEnvelope);
        return Promise.resolve();
    }

    public submitCommand(commandEnvelope: commandsAndEvents.KernelCommandEnvelope): Promise<void> {

        this.commandsSent.push(commandEnvelope);
        return Promise.resolve();
    }

    public waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    public dispose() {
    }
}

export function createMockChannel(rootUrl: string): Promise<{
    sender: connection.IKernelCommandAndEventSender,
    receiver: connection.IKernelCommandAndEventReceiver;
}> {
    return Promise.resolve(new MockKernelCommandAndEventChannel());
}

export function findEvent<T>(kernelEventEnvelopes: commandsAndEvents.KernelEventEnvelope[], eventType: commandsAndEvents.KernelEventType): T | undefined {
    return findEventEnvelope(kernelEventEnvelopes, eventType)?.event as T;
}

export function findEventFromKernel<T>(kernelEventEnvelopes: commandsAndEvents.KernelEventEnvelope[], eventType: commandsAndEvents.KernelEventType, kernelName: string): T | undefined {
    return findEventEnvelopeFromKernel(kernelEventEnvelopes, eventType, kernelName)?.event as T;
}

export function findEventEnvelope(kernelEventEnvelopes: commandsAndEvents.KernelEventEnvelope[], eventType: commandsAndEvents.KernelEventType): commandsAndEvents.KernelEventEnvelope | undefined {
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType);
}

export function findEventEnvelopeFromKernel(kernelEventEnvelopes: commandsAndEvents.KernelEventEnvelope[], eventType: commandsAndEvents.KernelEventType, kernelName: string): commandsAndEvents.KernelEventEnvelope | undefined {
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType && eventEnvelope.command!.command.targetKernelName === kernelName);
}

export function delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
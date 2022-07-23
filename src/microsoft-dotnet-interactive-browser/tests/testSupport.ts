// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import * as rxjs from "rxjs";
import { DotnetInteractiveClient, KernelClientContainer } from "../src/dotnet-interactive-interfaces";
import * as connection from "../src/dotnet-interactive/connection";
import * as contracts from "../src/dotnet-interactive/contracts";
import { TokenGenerator } from "../src/dotnet-interactive/tokenGenerator";


export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export class MockKernelCommandAndEventChannel {

    public commandsSent: Array<contracts.KernelCommandEnvelope>;
    public eventsPublished: Array<contracts.KernelEventEnvelope>;
    private tokenGenerator = new TokenGenerator();
    private eventObservers: { [key: string]: contracts.KernelEventEnvelopeObserver } = {};
    private commandHandlers: { [key: string]: contracts.KernelCommandEnvelopeHandler } = {};

    public sender: connection.IKernelCommandAndEventSender;
    public receiver: connection.IKernelCommandAndEventReceiver;
    private _senderSubject: rxjs.Subject<connection.KernelCommandOrEventEnvelope>;
    private _receiverSubject: rxjs.Subject<connection.KernelCommandOrEventEnvelope>;

    constructor() {

        this.commandsSent = new Array<contracts.KernelCommandEnvelope>();
        this.eventsPublished = new Array<contracts.KernelEventEnvelope>();

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

    public subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver) {
        let key = this.tokenGenerator.GetNewToken();
        this.eventObservers[key] = observer;

        return {
            dispose: () => {
                delete this.eventObservers[key];
            }
        };
    }

    public setCommandHandler(handler: contracts.KernelCommandEnvelopeHandler) {
        let key = this.tokenGenerator.GetNewToken();
        this.commandHandlers[key] = handler;
    }

    public fakeIncomingSubmitCommand(envelope: contracts.KernelCommandEnvelope) {
        this._receiverSubject.next(envelope);
    }

    publishKernelEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void> {
        this.eventsPublished.push(eventEnvelope);
        return Promise.resolve();
    }

    public submitCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {

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

export function findEvent<T>(kernelEventEnvelopes: contracts.KernelEventEnvelope[], eventType: contracts.KernelEventType): T | undefined {
    return findEventEnvelope(kernelEventEnvelopes, eventType)?.event as T;
}

export function findEventFromKernel<T>(kernelEventEnvelopes: contracts.KernelEventEnvelope[], eventType: contracts.KernelEventType, kernelName: string): T | undefined {
    return findEventEnvelopeFromKernel(kernelEventEnvelopes, eventType, kernelName)?.event as T;
}

export function findEventEnvelope(kernelEventEnvelopes: contracts.KernelEventEnvelope[], eventType: contracts.KernelEventType): contracts.KernelEventEnvelope | undefined {
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType);
}

export function findEventEnvelopeFromKernel(kernelEventEnvelopes: contracts.KernelEventEnvelope[], eventType: contracts.KernelEventType, kernelName: string): contracts.KernelEventEnvelope | undefined {
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType && eventEnvelope.command!.command.targetKernelName === kernelName);
}

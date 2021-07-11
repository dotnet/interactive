// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import { DotnetInteractiveClient, KernelClientContainer } from "../src/dotnet-interactive/dotnet-interactive-interfaces";
import { KernelTransport, KernelCommandEnvelope, KernelEventEnvelopeObserver, KernelCommand, KernelCommandType, KernelCommandEnvelopeHandler, KernelEventEnvelope, DisposableSubscription, KernelEventType } from "../src/common/interfaces/contracts";
import { TokenGenerator } from "../src/common/interactive/tokenGenerator";
import { Kernel } from "../src/common/interactive/kernel";


export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export class MockKernelTransport implements KernelTransport {

    public codeSubmissions: Array<KernelCommandEnvelope>;
    public publishedEvents: Array<KernelEventEnvelope>;
    private tokenGenerator = new TokenGenerator();
    private eventObservers: { [key: string]: KernelEventEnvelopeObserver } = {};
    private commandHandlers: { [key: string]: KernelCommandEnvelopeHandler } = {};

    constructor() {

        this.codeSubmissions = new Array<KernelCommandEnvelope>();
        this.publishedEvents = new Array<KernelEventEnvelope>();
    }

    public subscribeToKernelEvents(observer: KernelEventEnvelopeObserver) {
        let key = this.tokenGenerator.GetNewToken();
        this.eventObservers[key] = observer;

        return {
            dispose: () => {
                delete this.eventObservers[key];
            }
        };
    }

    public setCommandHandler(handler: KernelCommandEnvelopeHandler) {
        let key = this.tokenGenerator.GetNewToken();
        this.commandHandlers[key] = handler;
    }

    public fakeIncomingSubmitCommand(envelope: KernelCommandEnvelope) {
        for (let key of Object.keys(this.commandHandlers)) {
            let observer = this.commandHandlers[key];
            observer(envelope);
        }
    }

    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void> {
        this.publishedEvents.push(eventEnvelope);
        return Promise.resolve();
    }

    public submitCommand(commandEnvelope: KernelCommandEnvelope): Promise<void> {

        this.codeSubmissions.push(commandEnvelope);
        return Promise.resolve();
    }

    public waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    public dispose() {
    }
}

export function createMockKernelTransport(rootUrl: string): Promise<KernelTransport> {
    return Promise.resolve(new MockKernelTransport());
}

export function findEvent<T>(kernelEventEnvelopes: KernelEventEnvelope[], eventType: KernelEventType): T | undefined {
    return findEventEnvelope(kernelEventEnvelopes, eventType)?.event as T;
}

export function findEventFromKernel<T>(kernelEventEnvelopes: KernelEventEnvelope[], eventType: KernelEventType, kernelName: string): T | undefined {
    return findEventEnvelopeFromKernel(kernelEventEnvelopes, eventType, kernelName)?.event as T;
}

export function findEventEnvelope(kernelEventEnvelopes: KernelEventEnvelope[], eventType: KernelEventType): KernelEventEnvelope | undefined {
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType);
}

export function findEventEnvelopeFromKernel(kernelEventEnvelopes: KernelEventEnvelope[], eventType: KernelEventType, kernelName: string): KernelEventEnvelope | undefined {
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType && eventEnvelope.command.command.targetKernelName === kernelName);
}
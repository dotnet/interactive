// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import { DotnetInteractiveClient, KernelClientContainer } from "../src/dotnet-interactive/dotnet-interactive-interfaces";
import * as contracts from "../src/common/interfaces/contracts";
import { TokenGenerator } from "../src/common/interactive/tokenGenerator";
import { Kernel } from "../src/common/interactive/kernel";
import { CommandAndEventReceiver, GenericTransport } from "../src/common/interactive/genericTransport";


export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export class MockKernelTransport implements contracts.KernelTransport {

    public codeSubmissions: Array<contracts.KernelCommandEnvelope>;
    public publishedEvents: Array<contracts.KernelEventEnvelope>;
    private tokenGenerator = new TokenGenerator();
    private eventObservers: { [key: string]: contracts.KernelEventEnvelopeObserver } = {};
    private commandHandlers: { [key: string]: contracts.KernelCommandEnvelopeHandler } = {};

    constructor() {

        this.codeSubmissions = new Array<contracts.KernelCommandEnvelope>();
        this.publishedEvents = new Array<contracts.KernelEventEnvelope>();
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
        for (let key of Object.keys(this.commandHandlers)) {
            let observer = this.commandHandlers[key];
            observer(envelope);
        }
    }

    publishKernelEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void> {
        this.publishedEvents.push(eventEnvelope);
        return Promise.resolve();
    }

    public submitCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {

        this.codeSubmissions.push(commandEnvelope);
        return Promise.resolve();
    }

    public waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    public dispose() {
    }
}

export function createMockKernelTransport(rootUrl: string): Promise<contracts.KernelTransport> {
    return Promise.resolve(new MockKernelTransport());
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
    return kernelEventEnvelopes.find(eventEnvelope => eventEnvelope.eventType === eventType && eventEnvelope.command.command.targetKernelName === kernelName);
}

export function createInMemoryTransport(eventProducer?: (commandEnvelope: contracts.KernelCommandEnvelope) => contracts.KernelEventEnvelope[]): { transport: GenericTransport, sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[], writeToTransport: (data: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)) => void } {
    let sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    if (!eventProducer) {
        eventProducer = (ce) => {
            return [{ eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: ce }];
        }
    }

    const receiver = new CommandAndEventReceiver();
    let sender: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems.push(item);
        let events = eventProducer(<contracts.KernelCommandEnvelope>item)
        for (let event of events) {
            receiver.delegate(event);
        }
        return Promise.resolve();
    }
    let transport = new GenericTransport(
        sender,
        () => {
            return receiver.read();
        }
    );
    return {
        transport,
        sentItems,
        writeToTransport: (data) => {
            receiver.delegate(data);
        }
    };
}

export function createInMemoryChannel(): { channels: { transport: GenericTransport, sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] }[] } {
    const sentItems1: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    const sentItems2: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    const receiver1 = new CommandAndEventReceiver();
    const receiver2 = new CommandAndEventReceiver();

    const sender1: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems1.push(item);
        receiver2.delegate(item);
        return Promise.resolve();
    }

    const sender2: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems2.push(item);
        receiver1.delegate(item);
        return Promise.resolve();
    }

    const transport1 = new GenericTransport(
        sender1,
        () => {
            return receiver1.read();
        }
    );

    const transport2 = new GenericTransport(
        sender2,
        () => {
            return receiver2.read();
        }
    );

    return {
        channels: [
            {
                transport: transport1,
                sentItems: sentItems1,
            },
            {
                transport: transport2,
                sentItems: sentItems2,
            }
        ]
    };
}
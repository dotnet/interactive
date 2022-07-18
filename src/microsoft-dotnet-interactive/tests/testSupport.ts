// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "../src/contracts";
import * as connection from "../src/connection";
import * as genericChannel from "../src/genericChannel";
import * as rxjs from "rxjs";

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

export function createInMemoryChannel(eventProducer?: (commandEnvelope: contracts.KernelCommandEnvelope) => contracts.KernelEventEnvelope[]): { channel: genericChannel.GenericChannel, sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[], writeToTransport: (data: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)) => void } {
    let sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    if (!eventProducer) {
        eventProducer = (ce) => {
            return [{ eventType: contracts.CommandSucceededType, event: <contracts.CommandSucceeded>{}, command: ce }];
        };
    }

    const receiver = new genericChannel.CommandAndEventReceiver();
    let sender: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems.push(item);
        let events = eventProducer!(<contracts.KernelCommandEnvelope>item);
        for (let event of events) {
            receiver.delegate(event);
        }
        return Promise.resolve();
    };
    let channel = new genericChannel.GenericChannel(
        sender,
        () => {
            return receiver.read();
        }
    );
    return {
        channel: channel,
        sentItems,
        writeToTransport: (data) => {
            receiver.delegate(data);
        }
    };
}

export function createInMemoryChannels2(eventProducer?: (commandEnvelope: contracts.KernelCommandEnvelope) => contracts.KernelEventEnvelope[]): {
    local: { sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver },
    remote: { sender: connection.IKernelCommandAndEventSender, receiver: connection.IKernelCommandAndEventReceiver }
} {


    let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
    let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

    let channels = {
        local: {
            receiver: connection.KernelCommandAndEventReceiver2.FromObservable(remoteToLocal),
            sender: connection.KernelCommandAndEventSender2.FromObserver(localToRemote)
        },
        remote: {
            receiver: connection.KernelCommandAndEventReceiver2.FromObservable(localToRemote),
            sender: connection.KernelCommandAndEventSender2.FromObserver(remoteToLocal)
        }
    };

    return channels;
}

export function createInMemoryChannels(): { channels: { channel: genericChannel.GenericChannel, sentItems: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] }[] } {
    const sentItems1: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    const sentItems2: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];
    const receiver1 = new genericChannel.CommandAndEventReceiver();
    const receiver2 = new genericChannel.CommandAndEventReceiver();

    const sender1: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems1.push(item);
        receiver2.delegate(item);
        return Promise.resolve();
    };

    const sender2: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void> = (item) => {
        sentItems2.push(item);
        receiver1.delegate(item);
        return Promise.resolve();
    };

    const channel1 = new genericChannel.GenericChannel(
        sender1,
        () => {
            return receiver1.read();
        }
    );

    const channel2 = new genericChannel.GenericChannel(
        sender2,
        () => {
            return receiver2.read();
        }
    );

    return {
        channels: [
            {
                channel: channel1,
                sentItems: sentItems1,
            },
            {
                channel: channel2,
                sentItems: sentItems2,
            }
        ]
    };
}
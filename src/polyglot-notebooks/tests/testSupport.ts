// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "../src/commandsAndEvents";
import * as connection from "../src/connection";
import * as rxjs from "rxjs";

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

export function createInMemoryChannels(): {
    local: {
        sender: connection.IKernelCommandAndEventSender,
        receiver: connection.IKernelCommandAndEventReceiver,
        messagesSent: connection.KernelCommandOrEventEnvelope[]
    },
    remote: {
        sender: connection.IKernelCommandAndEventSender,
        receiver: connection.IKernelCommandAndEventReceiver,
        messagesSent: connection.KernelCommandOrEventEnvelope[]
    }
} {


    let localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
    let remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

    let remoteToLocalMessages: connection.KernelCommandOrEventEnvelope[] = [];
    let localToRemoteMessages: connection.KernelCommandOrEventEnvelope[] = [];

    localToRemote.subscribe({
        next: (e) => {
            localToRemoteMessages;//?
            localToRemoteMessages.push(e);
        }
    });

    remoteToLocal.subscribe({
        next: (e) => {
            remoteToLocalMessages//?;
            remoteToLocalMessages.push(e);
        }
    });

    let channels = {
        local: {
            receiver: connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal),
            sender: connection.KernelCommandAndEventSender.FromObserver(localToRemote),
            messagesSent: localToRemoteMessages,
        },
        remote: {
            receiver: connection.KernelCommandAndEventReceiver.FromObservable(localToRemote),
            sender: connection.KernelCommandAndEventSender.FromObserver(remoteToLocal),
            messagesSent: remoteToLocalMessages,
        }
    };

    return channels;
}

export function clearTokenAndId(envelope: connection.KernelCommandOrEventEnvelopeModel) {
    if (connection.isKernelEventEnvelopeModel(envelope)) {
        delete envelope.command?.token;

    } else if (connection.isKernelCommandEnvelopeModel(envelope)) {
        delete envelope.token;
    }

    return envelope;
}
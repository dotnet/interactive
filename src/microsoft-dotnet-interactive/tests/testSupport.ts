// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "../src/contracts";
import * as connection from "../src/connection";
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

export function clearTokenAndId(envelope: connection.KernelCommandOrEventEnvelope): connection.KernelCommandOrEventEnvelope {
    if (connection.isKernelEventEnvelope(envelope)) {
        let clone: contracts.KernelEventEnvelope = { ...envelope };
        if (clone.command) {
            clone.command = <contracts.KernelCommandEnvelope>clearTokenAndId(clone.command);
        }
        return clone;
    } else if (connection.isKernelCommandEnvelope(envelope)) {
        let clone = { ...envelope };
        clone.token = "commandToken";
        clone.id = "commandId";
        return clone;
    }

    throw new Error("Unknown envelope type");
}
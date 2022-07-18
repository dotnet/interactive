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

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "./commandsAndEvents";
export * from "./contracts";

export interface DocumentKernelInfoCollection {
    defaultKernelName: string;
    items: commandsAndEvents.DocumentKernelInfo[];
}

export interface KernelEventEnvelope {
    eventType: commandsAndEvents.KernelEventType;
    event: commandsAndEvents.KernelEvent;
    command?: KernelCommandEnvelope;
    routingSlip?: string[];
}

export interface KernelCommandEnvelope {
    token?: string;
    id?: string;
    commandType: commandsAndEvents.KernelCommandType;
    command: commandsAndEvents.KernelCommand;
    routingSlip?: string[];
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: KernelCommandEnvelope): Promise<void>;
}

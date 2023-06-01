// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
export * from "./contracts";

export interface DocumentKernelInfoCollection {
    defaultKernelName: string;
    items: contracts.DocumentKernelInfo[];
}

export interface KernelEventEnvelope {
    eventType: contracts.KernelEventType;
    event: contracts.KernelEvent;
    command?: KernelCommandEnvelope;
    routingSlip?: string[];
}

export interface KernelCommandEnvelope {
    token?: string;
    id?: string;
    commandType: contracts.KernelCommandType;
    command: contracts.KernelCommand;
    routingSlip?: string[];
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: KernelCommandEnvelope): Promise<void>;
}

export class KernelCommandEnvelope2 {
    constructor(
        public commandType: contracts.KernelCommandType,
        public command: contracts.KernelCommand) {
    }
}

export class KernelEventEnvelope2 {
    constructor(
        public eventType: contracts.KernelEventType,
        public event: contracts.KernelEvent,
        public command?: KernelCommandEnvelope2) {
    }
}
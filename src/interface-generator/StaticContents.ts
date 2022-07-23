﻿export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    command?: KernelCommandEnvelope;
    routingSlip?: string[];
}

export interface KernelCommandEnvelope {
    token?: string;
    id?: string;
    commandType: KernelCommandType;
    command: KernelCommand;
    routingSlip?: string[];
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: KernelCommandEnvelope): Promise<void>;
}
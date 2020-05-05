export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    cause?: KernelCommandEnvelope;
}

export interface KernelCommandEnvelope {
    token?: string;
    commandType: KernelCommandType;
    command: KernelCommand;
}

export interface KernelEventEvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface DisposableSubscription {
    dispose(): void;
}

export interface KernelTransport {
    subscribeToKernelEvents(observer: KernelEventEvelopeObserver): DisposableSubscription;
    submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void>;
}

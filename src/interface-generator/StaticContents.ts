export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    command?: KernelCommandEnvelope;
}

export interface KernelCommandEnvelope {
    token?: string;
    commandType: KernelCommandType;
    command: KernelCommand;
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface KernelCommandEnvelopeObserver {
    (eventEnvelope: KernelCommandEnvelope): void;
}

export interface Disposable {
    dispose(): void;
}

export interface DisposableSubscription extends Disposable {
}

export interface KernelTransport extends Disposable {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    subscribeToCommands(observer: KernelCommandEnvelopeObserver): DisposableSubscription;
    submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void>;
    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void>;
    waitForReady(): Promise<void>;
}

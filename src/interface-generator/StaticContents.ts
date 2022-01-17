export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    command?: KernelCommandEnvelope;
}

export interface KernelCommandEnvelope {
    token?: string;
    id?: string;
    commandType: KernelCommandType;
    command: KernelCommand;
    originUri?: string;
    destinationUri?: string;
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: KernelCommandEnvelope): Promise<void>;
}

export interface Disposable {
    dispose(): void;
}

export interface DisposableSubscription extends Disposable {
}

export interface KernelCommandAndEventSender {
    submitCommand(commandEnvelope: KernelCommandEnvelope): Promise<void>;
    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void>;
}

export interface KernelCommandAndEventReceiver {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    setCommandHandler(handler: KernelCommandEnvelopeHandler): void;
}

export interface KernelCommandAndEventChannel extends KernelCommandAndEventSender, KernelCommandAndEventReceiver, Disposable {
}


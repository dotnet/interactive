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

export interface KernelCommandEnvelopeObserver {
    (commandEnvelope: KernelCommandEnvelope): void;
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface Disposable {
    dispose(): void;
}

export interface DisposableSubscription extends Disposable {
}

export interface MessageEnvelope {
    label: string;
    payload: any;
}

export interface MessageObserver<T> {
    (message: T): void;
}

export interface LabelledMessageObserver<T> {
    (label: string, message: T): void;
}

export interface MessageTransport extends Disposable {
    subscribeToMessagesWithLabelPrefix<T extends object>(label: string, observer: LabelledMessageObserver<T>): DisposableSubscription;
    subscribeToMessagesWithLabel<T extends object>(label: string, observer: MessageObserver<T>): DisposableSubscription;
    sendMessage<T>(label: string, message: T): Promise<void>;
    waitForReady(): Promise<void>;
}

export interface KernelTransport extends Disposable {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void>;
    subscribeToCommands(observer: KernelCommandEnvelopeObserver): DisposableSubscription;
    submitKernelEvent(event: KernelEvent, eventType: KernelEventType, associatedCommand?: { command: KernelCommand, commandType: KernelCommandType, token: string }): Promise<void>;
    waitForReady(): Promise<void>;
}

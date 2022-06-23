import * as contracts from "./contracts";
export declare function isPromiseCompletionSource<T>(obj: any): obj is PromiseCompletionSource<T>;
export declare class PromiseCompletionSource<T> {
    private _resolve;
    private _reject;
    readonly promise: Promise<T>;
    constructor();
    resolve(value: T): void;
    reject(reason: any): void;
}
export declare class GenericChannel implements contracts.KernelCommandAndEventChannel {
    private readonly messageSender;
    private readonly messageReceiver;
    private stillRunning;
    private commandHandler;
    private eventSubscribers;
    constructor(messageSender: (message: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope) => Promise<void>, messageReceiver: () => Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>);
    dispose(): void;
    run(): Promise<void>;
    stop(): void;
    submitCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
    publishKernelEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void>;
    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): contracts.DisposableSubscription;
    setCommandHandler(handler: contracts.KernelCommandEnvelopeHandler): void;
}
export declare class CommandAndEventReceiver {
    private _waitingOnMessages;
    private readonly _envelopeQueue;
    delegate(commandOrEvent: contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope): void;
    read(): Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>;
}

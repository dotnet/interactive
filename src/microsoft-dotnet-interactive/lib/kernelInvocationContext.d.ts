import { KernelCommandEnvelope, KernelCommand, KernelEventEnvelope, Disposable } from "./contracts";
import { IKernelEventObserver, Kernel } from "./kernel";
export declare class KernelInvocationContext implements Disposable {
    get promise(): void | PromiseLike<void>;
    private static _current;
    private readonly _commandEnvelope;
    private readonly _childCommands;
    private readonly _tokenGenerator;
    private readonly _eventObservers;
    private _isComplete;
    handlingKernel: Kernel | null;
    private completionSource;
    static establish(kernelCommandInvocation: KernelCommandEnvelope): KernelInvocationContext;
    static get current(): KernelInvocationContext | null;
    get command(): KernelCommand;
    get commandEnvelope(): KernelCommandEnvelope;
    constructor(kernelCommandInvocation: KernelCommandEnvelope);
    subscribeToKernelEvents(observer: IKernelEventObserver): {
        dispose: () => void;
    };
    complete(command: KernelCommandEnvelope): void;
    fail(message?: string): void;
    publish(kernelEvent: KernelEventEnvelope): void;
    private internalPublish;
    isParentOfCommand(commandEnvelope: KernelCommandEnvelope): boolean;
    dispose(): void;
}
export declare function areCommandsTheSame(envelope1: KernelCommandEnvelope, envelope2: KernelCommandEnvelope): boolean;

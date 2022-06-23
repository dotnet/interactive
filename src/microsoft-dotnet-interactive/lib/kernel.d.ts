import { KernelInvocationContext } from "./kernelInvocationContext";
import * as contracts from "./contracts";
import { CompositeKernel } from "./compositeKernel";
export interface IKernelCommandInvocation {
    commandEnvelope: contracts.KernelCommandEnvelope;
    context: KernelInvocationContext;
}
export interface IKernelCommandHandler {
    commandType: string;
    handle: (commandInvocation: IKernelCommandInvocation) => Promise<void>;
}
export interface IKernelEventObserver {
    (kernelEvent: contracts.KernelEventEnvelope): void;
}
export declare class Kernel {
    readonly name: string;
    private _kernelInfo;
    private _commandHandlers;
    private readonly _eventObservers;
    private readonly _tokenGenerator;
    rootKernel: Kernel;
    parentKernel: CompositeKernel | null;
    private _scheduler?;
    get kernelInfo(): contracts.KernelInfo;
    constructor(name: string, languageName?: string, languageVersion?: string);
    protected handleRequestKernelInfo(invocation: IKernelCommandInvocation): Promise<void>;
    private getScheduler;
    private ensureCommandTokenAndId;
    static get current(): Kernel | null;
    static get root(): Kernel | null;
    send(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
    private executeCommand;
    getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined;
    handleCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): contracts.DisposableSubscription;
    protected canHandle(commandEnvelope: contracts.KernelCommandEnvelope): boolean;
    supportsCommand(commandType: contracts.KernelCommandType): boolean;
    registerCommandHandler(handler: IKernelCommandHandler): void;
    getHandlingKernel(commandEnvelope: contracts.KernelCommandEnvelope): Kernel | undefined;
    protected publishEvent(kernelEvent: contracts.KernelEventEnvelope): void;
}
export declare function submitCommandAndGetResult<TEvent extends contracts.KernelEvent>(kernel: Kernel, commandEnvelope: contracts.KernelCommandEnvelope, expectedEventType: contracts.KernelEventType): Promise<TEvent>;

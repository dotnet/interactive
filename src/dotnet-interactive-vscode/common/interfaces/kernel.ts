// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Disposable, DisposableSubscription, KernelCommand, KernelCommandEnvelope, KernelEvent, KernelEventEnvelopeObserver } from "./contracts";

export interface IKernelCommandInvocation {
    command: KernelCommand;
    context: IKernelInvocationContext;
}

export interface IKernelCommandHandler {
    commandType: string;
    handle: (commandInvocation: IKernelCommandInvocation) => Promise<void>;
}

export interface IKernelEventObserver {
    (kernelEvent: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }): void;
}

export interface IKernelInvocationContext extends Disposable {
    subscribeToKernelEvents(observer: IKernelEventObserver): DisposableSubscription;
    complete(command: KernelCommand): void;
    fail(message?: string): void
    publish(kernelEvent: { event: KernelEvent, eventType: string, command: KernelCommand, commandType: string }): void;
    command: KernelCommand;
}

export interface IKernel {
    readonly name: string;
    send(commandEnvelope: KernelCommandEnvelope): Promise<void>;
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    registerCommandHandler(handler: IKernelCommandHandler): void;
}
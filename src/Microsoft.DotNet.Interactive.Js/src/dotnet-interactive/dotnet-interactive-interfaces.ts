// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelEventEnvelopeObserver, DisposableSubscription, KernelCommandEnvelopeObserver, KernelCommand, KernelEvent, KernelCommandEnvelope, KernelEventEnvelope, Disposable } from "./contracts";


export interface VariableRequest {
    [kernelName: string]: Array<any>;
}

export interface VariableResponse {
    [kernelName: string]: {
        [variableName: string]: any
    }
}

export interface KernelClient {
    getVariable(variableName: string): Promise<any>;
    submitCode(code: string): Promise<string>;
    submitCommand(commandType: string, command?: any): Promise<string>;
}

export interface KernelInvocationContext extends Disposable {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    complete(commandEnvelope: KernelCommandEnvelope): void;
    fail(message?: string): void
    publish(eventEnvelope: KernelEventEnvelope): void;
    command: KernelCommandEnvelope;
}

// Implemented by the client-side kernel.
export interface Kernel {
    send(command: KernelCommandEnvelope): Promise<void>;
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    registerCommandHandler(commandType: string, observer: (envelope: KernelCommandEnvelope, context: KernelInvocationContext) => Promise<void>): void;
}

export interface DotnetInteractiveClient {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    registerCommandHandler(commandType: string, handler: (envelope: KernelCommandEnvelope) => Promise<void>): void;
    getVariable(kernelName: string, variableName: string): Promise<any>;
    getVariables(variableRequest: VariableRequest): Promise<VariableResponse>;
    getResource(resource: string): Promise<Response>;
    getResourceUrl(resource: string): string;
    getExtensionResource(extensionName: string, resource: string): Promise<Response>;
    getExtensionResourceUrl(extensionName: string, resource: string): string;
    loadKernels(): Promise<void>;
    submitCode(code: string, targetKernelName?: string): Promise<string>;
    submitCommand(commandType: string, command?: any, targetKernelName?: string): Promise<string>;
    configureRequire(config: any): any;
}

export interface KernelClientContainer {
    [key: string]: KernelClient;
}

export class DotnetInteractiveScopeContainer {
    [key: string]: DotnetInteractiveScope
}

export class DotnetInteractiveScope {
    [key: string]: any
}

export interface ClientFetch {
    (input: RequestInfo, init?: RequestInit): Promise<Response>
}
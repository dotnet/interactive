import { KernelEventEnvelope } from "./events";

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
export interface KernelEventEvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface DisposableSubscription {
    dispose(): void;
}

export interface KernelEventEnvelopeStream{
    subscribe(observer:KernelEventEvelopeObserver): DisposableSubscription;
}


export interface DotnetInteractiveClient {
    subscribeToKernelEvents(observer: KernelEventEvelopeObserver): DisposableSubscription;
    getVariable(kernelName: string, variableName: string): Promise<any>;
    getVariables(variableRequest: VariableRequest): Promise<VariableResponse>;
    getResource(resource: string): Promise<Response>;
    getResourceUrl(resource: string): string;
    loadKernels(): Promise<void>;
    submitCode(code: string, targetKernelName?: string): Promise<string>;
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
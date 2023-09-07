// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from "./polyglot-notebooks/commandsAndEvents";
import { DisposableSubscription } from "./polyglot-notebooks/disposables";
import { IKernelCommandHandler } from "./polyglot-notebooks/kernel";

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

// Implemented by the client-side kernel.
export interface DotnetInteractiveClient {
    subscribeToKernelEvents(observer: commandsAndEvents.KernelEventEnvelopeObserver): DisposableSubscription;
    registerCommandHandler(handler: IKernelCommandHandler): void;
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

    getConsole(commandToken: string): any;
    markExecutionComplete(commandToken: string): Promise<void>;
    failCommand(err: any, commandToken: string): void;
    waitForAllEventsToPublish(commandToken: string): Promise<void>;
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
    (input: RequestInfo, init?: RequestInit | undefined): Promise<Response>
}

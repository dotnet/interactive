// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelClient, VariableRequest, VariableResponse, DotnetInteractiveClient, ClientFetch} from "../dotnet-interactive/dotnet-interactive-interfaces";
import { Kernel, IKernelCommandHandler } from "../common/interactive/kernel";
import { TokenGenerator } from "../common/interactive/tokenGenerator";
import { signalTransportFactory } from "../dotnet-interactive/signalr-client";
import { CommandFailed, CommandFailedType, KernelTransport, KernelEventEnvelope, KernelEventEnvelopeObserver, DisposableSubscription, SubmitCode, SubmitCodeType, DisplayedValueProduced, DisplayedValueProducedType } from "../common/interfaces/contracts";
import { createDefaultClientFetch } from "./clientFetch";
import { clientSideKernelFactory } from "../dotnet-interactive/kernel-factory";

export interface KernelClientImplParameteres {
    clientFetch: (input: RequestInfo, init: RequestInit) => Promise<Response>;
    rootUrl: string;
    kernelTransport: KernelTransport,
    clientSideKernel: Kernel,
    configureRequire: (config: any) => any
}

class ClientEventQueueManager {
    private static eventPromiseQueues: Map<string, Array<Promise<void>>> = new Map();

    static addEventToClientQueue(clientFetch: ClientFetch, commandToken: string, eventEnvelope: KernelEventEnvelope) {
        let promiseQueue = this.eventPromiseQueues.get(commandToken);
        if (!promiseQueue) {
            promiseQueue = [];
            this.eventPromiseQueues.set(commandToken, promiseQueue);
        }

        const newPromise = clientFetch("publishEvent", {
            method: 'POST',
            cache: 'no-cache',
            mode: 'cors',
            body: JSON.stringify({ commandToken, eventEnvelope }),
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(() => { });
        promiseQueue.push(newPromise);
    }

    static async waitForAllEventsToPublish(commandToken: string): Promise<void> {
        const promiseQueue = this.eventPromiseQueues.get(commandToken);
        if (!promiseQueue) {
            return;
        }

        await Promise.all(promiseQueue);
    }
}

class InteractiveConsoleWrapper {
    private globalConsole: Console;

    constructor(private clientFetch: ClientFetch, private commandToken: string) {
        this.globalConsole = console;
    }

    public error(...args: any[]) {
        this.redirectAndEnqueue(this.globalConsole.error, ...args);
    }

    public info(...args: any[]) {
        this.redirectAndEnqueue(this.globalConsole.info, ...args);
    }

    public log(...args: any[]) {
        this.redirectAndEnqueue(this.globalConsole.log, ...args);
    }

    private redirectAndEnqueue(target: (...args: any[]) => void, ...args: any[]) {
        target(...args);
        this.enqueueArgsAsEvents(...args);
    }

    private enqueueArgsAsEvents(...args: any[]) {
        for (const arg of args) {
            let mimeType: string;
            let value: string;
            if (typeof arg !== 'object' && !Array.isArray(arg)) {
                mimeType = 'text/plain';
                value = arg.toString();
            } else {
                mimeType = 'application/json';
                value = JSON.stringify(arg);
            }

            const displayedValue: DisplayedValueProduced = {
                formattedValues: [
                    {
                        mimeType,
                        value,
                    }
                ]
            };
            const eventEnvelope: KernelEventEnvelope = {
                eventType: DisplayedValueProducedType,
                event: displayedValue,
            };

            ClientEventQueueManager.addEventToClientQueue(this.clientFetch, this.commandToken, eventEnvelope);
        }
    }
}

export class KernelClientImpl implements DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;
    private _kernelTransport: KernelTransport;
    private _clientSideKernel: Kernel;
    private _tokenGenerator: TokenGenerator;
    private _configureRequire: (confing: any) => any;

    constructor(parameters: KernelClientImplParameteres) {
        this._clientFetch = parameters.clientFetch;
        this._rootUrl = parameters.rootUrl;
        this._kernelTransport = parameters.kernelTransport;
        this._tokenGenerator = new TokenGenerator();
        this._configureRequire = parameters.configureRequire;
        this._clientSideKernel = parameters.clientSideKernel;
    }

    public configureRequire(config: any) {
        return this._configureRequire(config);
    }

    public subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        let subscription = this._kernelTransport.subscribeToKernelEvents(observer);
        return subscription;
    }

    public registerCommandHandler(handler: IKernelCommandHandler): void {
        this._clientSideKernel.registerCommandHandler(handler);
    }

    public async getVariable(kernelName: string, variableName: string): Promise<any> {
        let response = await this._clientFetch(`variables/${kernelName}/${variableName}`,
            {
                method: 'GET',
                cache: 'no-cache',
                mode: 'cors'
            });

        let variable = await response.json();
        return variable;
    }

    public async getVariables(variableRequest: VariableRequest): Promise<VariableResponse> {
        let response = await this._clientFetch("variables", {
            method: 'POST',
            cache: 'no-cache',
            mode: 'cors',
            body: JSON.stringify(variableRequest),
            headers: {
                'Content-Type': 'application/json'
            }
        });

        let variableBundle = await response.json();
        return variableBundle;
    }

    public getResource(resource: string): Promise<Response> {
        return this._clientFetch(`resources/${resource}`);
    }

    public getResourceUrl(resource: string): string {
        return `${this._rootUrl}resources/${resource}`;
    }

    public getExtensionResource(extensionName: string, resource: string): Promise<Response> {
        return this._clientFetch(`extension/${extensionName}/resources/${resource}`);
    }

    public getExtensionResourceUrl(extensionName: string, resource: string): string {
        return `${this._rootUrl}extensions/${extensionName}/resources/${resource}`;
    }

    public async loadKernels(): Promise<void> {
        let kernels = await this._clientFetch("kernels",
            {
                method: "GET",
                cache: 'no-cache',
                mode: 'cors'
            });

        let kernelNames = await kernels.json();
        if (Array.isArray(kernelNames)) {
            for (let i = 0; i < kernelNames.length; i++) {
                let kernelName: string = kernelNames[i];
                let kernelClient: KernelClient = {
                    getVariable: (variableName: string): Promise<any> => {
                        return this.getVariable(kernelName, variableName);
                    },

                    submitCode: (code: string): Promise<string> => {
                        return this.submitCode(code, kernelName);
                    },

                    submitCommand: (commandType: string, command?: any): Promise<string> => {
                        return this.submitCommand(commandType, command, kernelName);
                    }
                };

                (<any>this)[kernelName] = kernelClient;
            }
        }
    }

    public async submitCode(code: string, targetKernelName: string = null): Promise<string> {
        let token: string = this._tokenGenerator.GetNewToken();
        let command: SubmitCode = {
            code: code,
            targetKernelName: targetKernelName
        }

        await this._kernelTransport.submitCommand(command, SubmitCodeType, token);
        return token;
    }

    public async submitCommand(commandType: string, command?: any, targetKernelName?: string): Promise<string> {
        let token: string = this._tokenGenerator.GetNewToken();

        if (!command) {
            command = {};
        }

        if (targetKernelName) {
            command.targetKernelName = targetKernelName;
        }

        await this._kernelTransport.submitCommand(command, <any>commandType, token);
        return token;
    }

    public getConsole(commandToken: string): any {
        const wrappedConsole = new InteractiveConsoleWrapper(this._clientFetch, commandToken);
        return wrappedConsole;
    }

    public markExecutionComplete(commandToken: string): Promise<void> {
        return this._clientFetch("markExecutionComplete", {
            method: 'POST',
            cache: 'no-cache',
            mode: 'cors',
            body: JSON.stringify({ commandToken }),
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(() => { });
    }

    public failCommand(err: any, commandToken: string) {
        const failedEvent: CommandFailed = {
            message: `${err}`
        };
        const eventEnvelope: KernelEventEnvelope = {
            eventType: CommandFailedType,
            event: failedEvent,
        };
        ClientEventQueueManager.addEventToClientQueue(this._clientFetch, commandToken, eventEnvelope);
    }

    public waitForAllEventsToPublish(commandToken: string): Promise<void> {
        return ClientEventQueueManager.waitForAllEventsToPublish(commandToken);
    }
}

export type DotnetInteractiveClientConfiguration = {
    address: string,
    clientFetch?: ClientFetch,
    kernelTransportFactory?: (rootUrl: string) => Promise<KernelTransport>,
    clientSideKernelFactory?: (kernelTransport: KernelTransport) => Promise<Kernel>
};

function isConfiguration(config: any): config is DotnetInteractiveClientConfiguration {
    return typeof config !== "string";
}

export async function createDotnetInteractiveClient(configuration: string | DotnetInteractiveClientConfiguration): Promise<DotnetInteractiveClient> {
    let rootUrl = "";
    let clientFetch: ClientFetch = null;
    let kernelTransportFactory: (rootUrl: string) => Promise<KernelTransport> = null;
    let kernelFactory: (kernelTransport: KernelTransport) => Promise<Kernel> = null;

    if (isConfiguration(configuration)) {
        rootUrl = configuration.address;
        clientFetch = configuration.clientFetch;
        kernelTransportFactory = configuration.kernelTransportFactory;
        kernelFactory = configuration.clientSideKernelFactory;
    } else {
        rootUrl = configuration;
    }

    if (!rootUrl.endsWith("/")) {
        rootUrl = `${rootUrl}/`;
    }

    if (!clientFetch) {
        clientFetch = createDefaultClientFetch(rootUrl);
    }

    if (!kernelTransportFactory) {
        kernelTransportFactory = signalTransportFactory;
    }

    if (!kernelFactory) {
        kernelFactory = clientSideKernelFactory;
    }

    let transport = await kernelTransportFactory(rootUrl);
    let clientSideKernel = await kernelFactory(transport);
    let client = new KernelClientImpl({
        clientFetch: clientFetch,
        rootUrl,
        kernelTransport: transport,
        clientSideKernel,
        configureRequire: (config: any) => {
            return (<any>require).config(config) || require;
        }
    });

    await client.loadKernels();

    return client;
}

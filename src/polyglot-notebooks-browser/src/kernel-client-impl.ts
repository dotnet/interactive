// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractiveInterfaces from "./polyglot-notebooks-interfaces";
import { Kernel, IKernelCommandHandler } from "./polyglot-notebooks/kernel";
import { TokenGenerator } from "./polyglot-notebooks/tokenGenerator";
import { signalTransportFactory } from "./signalr-client";
import * as commandsAndEvents from "./polyglot-notebooks/commandsAndEvents";
import { createDefaultClientFetch } from "./clientFetch";
import { clientSideKernelFactory } from "./kernel-factory";
import { DisposableSubscription, IKernelCommandAndEventReceiver, IKernelCommandAndEventSender, isKernelEventEnvelope } from "./polyglot-notebooks";

export interface KernelClientImplParameteres {
    clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    rootUrl: string;
    channel: {
        sender: IKernelCommandAndEventSender,
        receiver: IKernelCommandAndEventReceiver;
    },
    clientSideKernel: Kernel,
    configureRequire: (config: any) => any
}

class ClientEventQueueManager {
    private static eventPromiseQueues: Map<string, Array<Promise<void>>> = new Map();

    static addEventToClientQueue(clientFetch: dotnetInteractiveInterfaces.ClientFetch, commandToken: string, eventEnvelope: commandsAndEvents.KernelEventEnvelope) {
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

    constructor(private clientFetch: dotnetInteractiveInterfaces.ClientFetch, private commandToken: string) {
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

            const displayedValue: commandsAndEvents.DisplayedValueProduced = {
                formattedValues: [
                    {
                        mimeType,
                        value,
                    }
                ]
            };
            const eventEnvelope: commandsAndEvents.KernelEventEnvelope = {
                eventType: commandsAndEvents.DisplayedValueProducedType,
                event: displayedValue,
            };

            ClientEventQueueManager.addEventToClientQueue(this.clientFetch, this.commandToken, eventEnvelope);
        }
    }
}

export class KernelClientImpl implements dotnetInteractiveInterfaces.DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;
    private _kernelChannel: {
        sender: IKernelCommandAndEventSender,
        receiver: IKernelCommandAndEventReceiver;
    };
    private _clientSideKernel: Kernel;
    private _tokenGenerator: TokenGenerator;
    private _configureRequire: (confing: any) => any;

    constructor(parameters: KernelClientImplParameteres) {
        this._clientFetch = parameters.clientFetch;
        this._rootUrl = parameters.rootUrl;
        this._kernelChannel = parameters.channel;
        this._tokenGenerator = new TokenGenerator();
        this._configureRequire = parameters.configureRequire;
        this._clientSideKernel = parameters.clientSideKernel;
    }

    public configureRequire(config: any) {
        return this._configureRequire(config);
    }

    public subscribeToKernelEvents(observer: commandsAndEvents.KernelEventEnvelopeObserver): DisposableSubscription {
        let subscription = this._kernelChannel.receiver.subscribe({
            next: (envelope) => {
                if (isKernelEventEnvelope(envelope)) {
                    observer(envelope);
                }
            }
        });
        return { dispose: () => subscription.unsubscribe() };
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

    public async getVariables(variableRequest: dotnetInteractiveInterfaces.VariableRequest): Promise<dotnetInteractiveInterfaces.VariableResponse> {
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
                let kernelClient: dotnetInteractiveInterfaces.KernelClient = {
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

    public async submitCode(code: string, targetKernelName?: string): Promise<string> {
        let token: string = this._tokenGenerator.createToken();
        let command: commandsAndEvents.SubmitCode = {
            code: code,
            targetKernelName: targetKernelName
        }

        await this._kernelChannel.sender.send({ command, commandType: commandsAndEvents.SubmitCodeType, token });
        return token;
    }

    public async submitCommand(commandType: string, command?: any, targetKernelName?: string): Promise<string> {
        let token: string = this._tokenGenerator.createToken();

        if (!command) {
            command = {};
        }

        if (targetKernelName) {
            command.targetKernelName = targetKernelName;
        }

        await this._kernelChannel.sender.send({ command, commandType: <any>commandType, token });
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
        const failedEvent: commandsAndEvents.CommandFailed = {
            message: `${err}`
        };
        const eventEnvelope: commandsAndEvents.KernelEventEnvelope = {
            eventType: commandsAndEvents.CommandFailedType,
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
    clientFetch?: dotnetInteractiveInterfaces.ClientFetch,
    channelFactory?: (rootUrl: string) => Promise<{
        sender: IKernelCommandAndEventSender,
        receiver: IKernelCommandAndEventReceiver;
    }>,
    clientSideKernelFactory?: (kernelTransport: {
        sender: IKernelCommandAndEventSender,
        receiver: IKernelCommandAndEventReceiver;
    }) => Promise<Kernel>
};

function isConfiguration(config: any): config is DotnetInteractiveClientConfiguration {
    return typeof config !== "string";
}

export async function createDotnetInteractiveClient(configuration: string | DotnetInteractiveClientConfiguration): Promise<dotnetInteractiveInterfaces.DotnetInteractiveClient> {
    let rootUrl = "";
    let clientFetch: dotnetInteractiveInterfaces.ClientFetch | undefined;
    let channelFactory: ((rootUrl: string) => Promise<{
        sender: IKernelCommandAndEventSender,
        receiver: IKernelCommandAndEventReceiver;
    }>) | undefined;
    let kernelFactory: ((kernelTransport: {
        sender: IKernelCommandAndEventSender,
        receiver: IKernelCommandAndEventReceiver;
    }) => Promise<Kernel>) | undefined;

    if (isConfiguration(configuration)) {
        rootUrl = configuration.address;
        clientFetch = configuration.clientFetch;
        channelFactory = configuration.channelFactory;
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

    if (!channelFactory) {
        channelFactory = signalTransportFactory;
    }

    if (!kernelFactory) {
        kernelFactory = clientSideKernelFactory;
    }

    let transport = await channelFactory(rootUrl);
    let clientSideKernel = await kernelFactory(transport);
    let client = new KernelClientImpl({
        clientFetch: clientFetch,
        rootUrl,
        channel: transport,
        clientSideKernel,
        configureRequire: (config: any) => {
            return (<any>require).config(config) || require;
        }
    });

    await client.loadKernels();

    return client;
}

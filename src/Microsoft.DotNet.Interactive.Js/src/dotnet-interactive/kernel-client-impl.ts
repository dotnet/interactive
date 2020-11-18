// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelClient, VariableRequest, VariableResponse, DotnetInteractiveClient, ClientFetch, KernelCommandObserver } from "./dotnet-interactive-interfaces";
import { TokenGenerator } from "./tokenGenerator";
import { signalTransportFactory } from "./signalr-client";
import { KernelTransport, KernelEventEnvelopeObserver, DisposableSubscription, SubmitCode, SubmitCodeType, KernelCommand } from "./contracts";
import { createDefaultClientFetch } from "./clientFetch";

import { kernelTransportFromMessageTransport } from "./kernelTransport";
import { send } from "process";


export interface KernelClientImplParameteres {
    clientFetch: (input: RequestInfo, init: RequestInit) => Promise<Response>;
    rootUrl: string;
    kernelTransport: KernelTransport,
    configureRequire: (config: any) => any
}
export class KernelClientImpl implements DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;
    private _kernelTransport: KernelTransport;
    private _tokenGenerator: TokenGenerator;
    private _configureRequire: (confing: any) => any;
    constructor(parameters: KernelClientImplParameteres) {
        this._clientFetch = parameters.clientFetch;
        this._rootUrl = parameters.rootUrl;
        this._kernelTransport = parameters.kernelTransport;
        this._tokenGenerator = new TokenGenerator();
        this._configureRequire = parameters.configureRequire;
    }
    public configureRequire(config: any) {
        return this._configureRequire(config);
    }

    public subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        let subscription = this._kernelTransport.subscribeToKernelEvents(observer);
        return subscription;
    }

    public subscribeToCommands<TCommand extends KernelCommand>(commandType: string, observer: KernelCommandObserver<TCommand>): DisposableSubscription {
        // Note: for now, this is the only place we do anything with commands, so this is a bit of a quick
        // hack. The vision was more around having a proper client-side kernel, which would mean we'd
        // need to ensure in-order processing of commands, queuing up any that arrive when processing is
        // already in progress.
        return this._kernelTransport.subscribeToCommands(commandEnvelope => {
            if (commandEnvelope.commandType === commandType) {
                observer(<TCommand>commandEnvelope.command);
            }
        });
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
}

export type DotnetInteractiveClientConfiguration = {
    address: string,
    clientFetch?: ClientFetch,
    kernelTransportFactory?: (rootUrl: string) => Promise<KernelTransport>
};

function isConfiguration(config: any): config is DotnetInteractiveClientConfiguration {
    return typeof config !== "string";
}

export async function createDotnetInteractiveClient(configuration: string | DotnetInteractiveClientConfiguration): Promise<DotnetInteractiveClient> {
    let rootUrl = "";
    let clientFetch: ClientFetch = null;
    let kernelTransportFactory: (rootUrl: string) => Promise<KernelTransport> = null;

    if (isConfiguration(configuration)) {
        rootUrl = configuration.address;
        clientFetch = configuration.clientFetch;
        kernelTransportFactory = configuration.kernelTransportFactory;
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
        kernelTransportFactory = async (rootUrl) => kernelTransportFromMessageTransport(await signalTransportFactory(rootUrl));
    }

    let transport = await kernelTransportFactory(rootUrl);
    let client = new KernelClientImpl({
        clientFetch: clientFetch,
        rootUrl,
        kernelTransport: transport,
        configureRequire: (config: any) => {
            return (<any>require).config(config) || require;
        }
    });

    await client.loadKernels();

    return client;
}
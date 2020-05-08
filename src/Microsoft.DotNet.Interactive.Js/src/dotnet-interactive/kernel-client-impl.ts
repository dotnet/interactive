// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelClient, VariableRequest, VariableResponse, DotnetInteractiveClient, ClientFetch } from "./dotnet-interactive-interfaces";
import { TokenGenerator } from "./tokenGenerator";
import { signalTransportFactory } from "./signalr-client";
import { KernelTransport, KernelEventEvelopeObserver, DisposableSubscription, SubmitCode, SubmitCodeType } from "./contracts";
import { createDefaultClientFetch } from "./clientFetch";

export interface KernelClientImplParameteres {
    clientFetch: (input: RequestInfo, init: RequestInit) => Promise<Response>;
    rootUrl: string;
    kernelTransport: KernelTransport
}
export class KernelClientImpl implements DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;
    private _kernelTransport: KernelTransport;
    private _tokenGenerator: TokenGenerator;

    constructor(parameters: KernelClientImplParameteres) {
        this._clientFetch = parameters.clientFetch;
        this._rootUrl = parameters.rootUrl;
        this._kernelTransport = parameters.kernelTransport;
        this._tokenGenerator = new TokenGenerator();
    }

    public subscribeToKernelEvents(observer: KernelEventEvelopeObserver): DisposableSubscription {
        let subscription = this._kernelTransport.subscribeToKernelEvents(observer);
        return subscription;
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
        let resourceUrl: string = `${this._rootUrl}resources/${resource}`;
        return resourceUrl;
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
        kernelTransportFactory = signalTransportFactory;
    }

    let transport = await kernelTransportFactory(rootUrl);
    let client = new KernelClientImpl({
        clientFetch: clientFetch,
        rootUrl,
        kernelTransport: transport
    });

    await client.loadKernels();

    return client;
}
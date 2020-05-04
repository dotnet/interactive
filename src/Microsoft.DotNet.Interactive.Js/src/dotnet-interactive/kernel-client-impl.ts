// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { KernelClient, VariableRequest, VariableResponse, DotnetInteractiveClient, ClientFetch, KernelEventEvelopeObserver, DisposableSubscription, KernelEventEnvelopeStream } from "./dotnet-interactive-interfaces";
import { SubmitCode } from "./commands";

export class KernelClientImpl implements DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;
    private _kernelEventStream: KernelEventEnvelopeStream;

    constructor({ clientFetch, rootUrl, kernelEventStream }: { clientFetch: (input: RequestInfo, init: RequestInit) => Promise<Response>; rootUrl: string; kernelEventStream: KernelEventEnvelopeStream }) {
        this._clientFetch = clientFetch;
        this._rootUrl = rootUrl;
        this._kernelEventStream = kernelEventStream;
    }

    public subscribeToKernelEvents(observer: KernelEventEvelopeObserver): DisposableSubscription {
        let subscription = this._kernelEventStream.subscribe(observer);
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
                    }
                };

                (<any>this)[kernelName] = kernelClient;
            }
        }
    }

    public async submitCode(code: string, targetKernelName: string = null): Promise<string> {
        let token: string = null;

        let command: SubmitCode = {
            code: code,
        };

        if (targetKernelName) {
            command.targetKernelName = targetKernelName
        }

        let response = await this._clientFetch("submitCode", {
            method: 'POST',
            cache: 'no-cache',
            mode: 'cors',
            body: JSON.stringify(command),
            headers: {
                'Content-Type': 'application/json'
            }
        });

        let etag = response.headers.get("ETag");
        if (etag) {
            token = etag;
        }
        return token;
    }
}

export type DotnetInteractiveClientConfiguration = {
    address: string,
    clientFetch?: ClientFetch,
    kernelEventStreamFactory?: (rootUrl: string) => Promise<KernelEventEnvelopeStream>
};

function isConfiguration(config: any): config is DotnetInteractiveClientConfiguration {
    return typeof config !== "string";
}
export async function createDotnetInteractiveClient(configuration: string | DotnetInteractiveClientConfiguration): Promise<DotnetInteractiveClient> {

    let rootUrl = "";
    let clientFetch: ClientFetch = null;
    let kernelEventStreamFactory: (rootUrl: string) => Promise<KernelEventEnvelopeStream> = null;

    if (isConfiguration(configuration)) {
        rootUrl = configuration.address;
        clientFetch = configuration.clientFetch;
        kernelEventStreamFactory = configuration.kernelEventStreamFactory;
    }

    if (!rootUrl.endsWith("/")) {
        rootUrl = `${rootUrl}/`;
    }

    async function defaultClientFetch(input: string, requestInit: RequestInit = null): Promise<Response> {
        let address = input;

        if (!address.startsWith("http")) {
            address = `${rootUrl}${address}`;
        }

        let response = await fetch(address, requestInit);
        return response;
    }  

    if (!clientFetch) {
        clientFetch = defaultClientFetch;
    }


    if (!kernelEventStreamFactory) {
        kernelEventStreamFactory = kernelEventStreamFactory;
    }

    let eventStream = await kernelEventStreamFactory(rootUrl);
    let client = new KernelClientImpl({
        clientFetch: clientFetch,
        rootUrl,
        kernelEventStream: eventStream
    });

    await client.loadKernels();

    return client;
}
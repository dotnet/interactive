import { KernelClient, VariableRequest, VariableResponse, DotnetInteractiveClient, ClientFetch } from "./dotnet-interactive-interfaces";

export class KernelClientImpl implements DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;

    constructor(clientFetch: (input: RequestInfo, init: RequestInit) => Promise<Response>, rootUrl: string) {
        this._clientFetch = clientFetch;
        this._rootUrl = rootUrl;

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
}

export async function createDotnetInteractiveClient(address: string, clientFetch: ClientFetch = null): Promise<DotnetInteractiveClient> {

    let rootUrl = address;
    if (!address.endsWith("/")) {
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

    let cf: ClientFetch = clientFetch;

    if (!clientFetch) {
        cf = defaultClientFetch;
    }

    let client = new KernelClientImpl(cf, rootUrl);

    await client.loadKernels();

    return client;
}
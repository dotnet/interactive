import { KernelClient, VariableRequest, VariableResponse, DotnetInteractiveClient } from "./dotnet-interactive-interfaces";

export class KernelClientImpl implements DotnetInteractiveClient {

    private _clientFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private _rootUrl: string;

    constructor(clientFetch: (input: RequestInfo, init: RequestInit) => Promise<Response>, rootUrl: string) {
        this._clientFetch = clientFetch;
        this._rootUrl = rootUrl;

    }
    
    async getVariable(kernelName: string, variableName: string): Promise<any> {
        let response = await this._clientFetch(`variables/${kernelName}/${variableName}`,
            {
                method: 'GET',
                cache: 'no-cache',
                mode: 'cors'
            });
        let variable = await response.json();
        return variable;

    }
    async getVariables(variableRequest: VariableRequest): Promise<VariableResponse> {
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

    getResource(resource: string): Promise<Response> {
        return this._clientFetch(`resources/${resource}`);
    }

    getResourceUrl(resource: string): string {
        let resourceUrl: string = `${this._rootUrl}resources/${resource}`;
        return resourceUrl;
    }

    async loadKernels(): Promise<void> {
        let kernels = await this._clientFetch("kernels");
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
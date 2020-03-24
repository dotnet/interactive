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
    GetVariable(variableName: string): any;
}

export interface DotnetInteractiveClient {
    GetVariable(kernelName: string, variableName: string): Promise<any>
    GetVariables(variableRequest: VariableRequest): Promise<VariableResponse>;
}

export class DotnetInteractiveScopeContainer {
    [key:string] : DotnetInteractiveScope
}

export class DotnetInteractiveScope{
    [key:string]: any
}

export function init(global: any) {
    global.getDotnetInteractiveScope = (key: string) =>{
        if (typeof(global.interactiveScopes) === undefined){
            global.interactiveScopes = new DotnetInteractiveScopeContainer();
        }

        if(typeof(global.interactiveScopes[key]) === undefined){
            global.interactiveScopes[key] = new DotnetInteractiveScope();
        }

        return global.interactiveScopes[key];
    }

    global.createDotnetInteractiveClient = async (address: string) : Promise<DotnetInteractiveClient> => {
        let root = new URL(address);
        async function clientFetch(input : RequestInfo, init : RequestInit) : Promise<Response>{
           let response = await fetch(input, init);
           return response;
        }
        let client : DotnetInteractiveClient = {
            GetVariable: async (kernelName: string, variableName: string): Promise<any> => {
                return null;

            },
            GetVariables: async (variableRequest: VariableRequest): Promise<VariableResponse> =>{
                return null;
            }
        } 
        return client;
    }
    
}
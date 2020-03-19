// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface KernelClient {    
    GetVariable(variableName: string): any ;   
    GetStuff() : number;
}

export function createClient(): KernelClient{
    return {
        
        GetVariable : (variableName: string): any => {
            return 1;            
        },
        GetStuff: (): number => {
            return 1;            
        }
    };
}
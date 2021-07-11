// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function createDefaultClientFetch(rootUrl: string){
    async function defaultClientFetch(input: string, requestInit: RequestInit = null): Promise<Response> {
        let address = input;
    
        if (!address.startsWith("http")) {
            address = `${rootUrl}${address}`;
        }
    
        let response = await fetch(address, requestInit);
        return response;
    }

    return defaultClientFetch;
}
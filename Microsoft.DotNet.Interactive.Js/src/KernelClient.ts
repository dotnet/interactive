// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DotnetInteractiveScopeContainer, DotnetInteractiveScope, DotnetInteractiveClient, ClientFetch } from "./dotnet-interactive-interfaces";
import { KernelClientImpl } from "./KernelClientImpl";

export function init(global: any) {
    global.getDotnetInteractiveScope = (key: string) => {
        if (typeof (global.interactiveScopes) === undefined) {
            global.interactiveScopes = new DotnetInteractiveScopeContainer();
        }

        if (typeof (global.interactiveScopes[key]) === undefined) {
            global.interactiveScopes[key] = new DotnetInteractiveScope();
        }

        return global.interactiveScopes[key];
    }

    global.createDotnetInteractiveClient = async (address: string, clientFetch : ClientFetch = null): Promise<DotnetInteractiveClient> => {

        let rootUrl = address;
        if (!address.endsWith("/")) {
            rootUrl = `${rootUrl}/`;
        }

        async function defaukltClientFetch(input: RequestInfo, init: RequestInit = null): Promise<Response> {
            let address = "";

            if (input instanceof Request) {
                address = input.url;
            } else {
                address = input;
            }
            if (!address.startsWith("http")) {
                address = `${rootUrl}${address}`;
            }

            let response = await fetch(address, init);
            return response;
        }

        let cf:ClientFetch = clientFetch;

        if(typeof(clientFetch) === undefined){
            cf = defaukltClientFetch;
        }
        
        let client = new KernelClientImpl(cf, rootUrl);
        return client;
    }
}
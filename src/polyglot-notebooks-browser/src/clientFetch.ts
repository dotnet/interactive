// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientFetch } from ".";

export function createDefaultClientFetch(rootUrl: string): ClientFetch {

    function isRequestInfo(x: RequestInfo): x is Request {
        return x.hasOwnProperty("url");
    }
    async function defaultClientFetch(input: RequestInfo, requestInit?: RequestInit): Promise<Response> {
        let address: string = isRequestInfo(input) ? input.url : input;

        if (!address.startsWith("http")) {
            address = `${rootUrl}${address}`;
        }

        let response = await fetch(address, requestInit);
        return response;
    }

    return defaultClientFetch;
}


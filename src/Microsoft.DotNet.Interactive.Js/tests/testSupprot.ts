// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import { DotnetInteractiveClient, KernelClientContainer, KernelEventEnvelopeStream, KernelEventEvelopeObserver } from "../src/dotnet-interactive/dotnet-interactive-interfaces";

export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export function createMockKernelEventStream(rootUrl: string): Promise<KernelEventEnvelopeStream> {
    let mock: KernelEventEnvelopeStream = {
        subscribe: (observer: KernelEventEvelopeObserver) => {
            return {
                dispose: (): void => {

                }
            }
        }
    }

    return Promise.resolve(mock);
}
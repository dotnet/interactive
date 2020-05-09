// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import { DotnetInteractiveClient, KernelClientContainer } from "../src/dotnet-interactive/dotnet-interactive-interfaces";
import { KernelTransport, KernelCommandEnvelope, KernelEventEnvelopeObserver, KernelCommand, KernelCommandType } from "../src/dotnet-interactive/contracts";


export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export class MockKernelTransport implements KernelTransport {

    public codeSubmissions: Array<KernelCommandEnvelope>;

    constructor() {

        this.codeSubmissions = new Array<KernelCommandEnvelope>();
    }
    public subscribeToKernelEvents(observer: KernelEventEnvelopeObserver) {
        return {
            dispose: (): void => {

            }
        }
    }

    public submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {

        this.codeSubmissions.push(<KernelCommandEnvelope>{
            commandType: commandType,
            command: command,
            token: token
        });
        return Promise.resolve();
    }

    public dispose() {
    }
}

export function createMockKernelTransport(rootUrl: string): Promise<KernelTransport> {
    return Promise.resolve(new MockKernelTransport());
}
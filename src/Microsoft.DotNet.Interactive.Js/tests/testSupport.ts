// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fetchMock from "fetch-mock";
import { DotnetInteractiveClient, KernelClientContainer } from "../src/dotnet-interactive/dotnet-interactive-interfaces";
import { KernelTransport, KernelCommandEnvelope, KernelEventEnvelopeObserver, KernelCommand, KernelCommandType, KernelCommandEnvelopeObserver, KernelEvent, KernelEventType, KernelEventEnvelope } from "../src/dotnet-interactive/contracts";
import { TokenGenerator } from "../src/dotnet-interactive/tokenGenerator";


export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}

export function configureFetchForKernelDiscovery(rootUrl: string) {
    fetchMock.get(`${rootUrl}/kernels`, require("./Responses/kernels-get-response.json"));
}

export class MockKernelTransport implements KernelTransport {

    public codeSubmissions: Array<KernelCommandEnvelope>;
    public publishedEvents: Array<KernelEventEnvelope>;
    private tokenGenerator = new TokenGenerator();
    private eventObservers: { [key: string]: KernelEventEnvelopeObserver } = {};
    private commandObservers: { [key: string]: KernelCommandEnvelopeObserver } = {};

    constructor() {

        this.codeSubmissions = new Array<KernelCommandEnvelope>();
        this.publishedEvents = new Array<KernelEventEnvelope>();
    }

    public subscribeToKernelEvents(observer: KernelEventEnvelopeObserver) {
        let key = this.tokenGenerator.GetNewToken();
        this.eventObservers[key] = observer;

        return {
            dispose: () => {
                delete  this.eventObservers[key];
            }
        };
    }

    public subscribeToCommands(observer: KernelCommandEnvelopeObserver) {
        let key =  this.tokenGenerator.GetNewToken();
        this.commandObservers[key] = observer;

        return {
            dispose: () => {
                delete  this.commandObservers[key];
            }
        };
    }

    public fakeIncomingSubmitCommand(envelope: KernelCommandEnvelope) {
        for (let key of Object.keys(this.commandObservers)) {
            let observer = this.commandObservers[key];
            observer(envelope);
        }
    }

    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void> {
        this.publishedEvents.push(eventEnvelope);
        return Promise.resolve();        
    }

    public submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {

        this.codeSubmissions.push(<KernelCommandEnvelope>{
            commandType: commandType,
            command: command,
            token: token
        });
        return Promise.resolve();
    }

    public waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    public dispose() {
    }
}

export function createMockKernelTransport(rootUrl: string): Promise<KernelTransport> {
    return Promise.resolve(new MockKernelTransport());
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";

import { KernelTransport, KernelEventEnvelope, KernelEventEnvelopeObserver, DisposableSubscription, KernelCommandEnvelope, KernelCommandEnvelopeHandler } from "./dotnet-interactive/contracts";
import { TokenGenerator } from "./dotnet-interactive/tokenGenerator";


export async function signalTransportFactory(rootUrl: string): Promise<KernelTransport> {

    let hubUrl = rootUrl;
    if (hubUrl.endsWith("/")) {
        hubUrl = `${hubUrl}kernelhub`;
    } else {
        hubUrl = `${hubUrl}/kernelhub`;
    }

    let connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    let tokenGenerator = new TokenGenerator();

    let eventObservers: { [key: string]: KernelEventEnvelopeObserver } = {};
    let commandHandlers: { [key: string]: KernelCommandEnvelopeHandler } = {};

    // deprecated
    connection.on("kernelEvent", (message: string) => {
        let eventEnvelope = <KernelEventEnvelope>JSON.parse(message);
        let keys = Object.keys(eventObservers);
        for (let key of keys) {
            let observer = eventObservers[key];
            observer(eventEnvelope);
        }
    });

    // deprecated
    connection.on("submitCommand", (message: string) => {
        let commandEnvelope = <KernelCommandEnvelope>JSON.parse(message);
        let keys = Object.keys(commandHandlers);
        for (let key of keys) {
            let observer = commandHandlers[key];
            observer(commandEnvelope);
        }
    });

    connection.on("commandFromServer", (message: string) => {
        let commandEnvelope = <KernelCommandEnvelope>JSON.parse(message);
        let keys = Object.keys(commandHandlers);
        for (let key of keys) {
            let observer = commandHandlers[key];
            observer(commandEnvelope);
        }
    });

    connection.on("eventFromServer", (message: string) => {
        let eventEnvelope = <KernelEventEnvelope>JSON.parse(message);
        let keys = Object.keys(eventObservers);
        for (let key of keys) {
            let observer = eventObservers[key];
            observer(eventEnvelope);
        }
    });

    await connection
        .start()
        .catch(err => console.log(err));

    let eventStream: KernelTransport = {

        subscribeToKernelEvents: (observer: KernelEventEnvelopeObserver): DisposableSubscription => {
            let key = tokenGenerator.GetNewToken();
            eventObservers[key] = observer;

            let disposableSubscription: DisposableSubscription = {
                dispose: () => {
                    delete eventObservers[key];
                }
            }

            return disposableSubscription;
        },

        setCommandHandler: (handler: KernelCommandEnvelopeHandler) => {
            const key = tokenGenerator.GetNewToken();
            commandHandlers[key] = handler;
        },

        submitCommand: (commandEnvelope: KernelCommandEnvelope): Promise<void> => {
            return connection.send("submitCommand", JSON.stringify(commandEnvelope));
        },

        publishKernelEvent: (eventEnvelope: KernelEventEnvelope): Promise<void> => {
            return connection.send("kernelEvent", JSON.stringify(eventEnvelope));
        },

        waitForReady: (): Promise<void> => {
            return Promise.resolve();
        },

        dispose: (): void => {
        }
    };

    await connection.send("connect");
    return Promise.resolve(eventStream);
}
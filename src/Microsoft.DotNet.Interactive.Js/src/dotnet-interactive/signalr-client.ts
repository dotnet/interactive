// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";

import { KernelTransport, KernelEventEnvelope, KernelEventEnvelopeObserver, DisposableSubscription, KernelCommand, KernelCommandType, KernelCommandEnvelope, SubmitCodeType, KernelCommandEnvelopeObserver, KernelEvent, KernelEventType } from "./contracts";
import { TokenGenerator } from "./tokenGenerator";


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
    let commandObservers: { [key: string]: KernelCommandEnvelopeObserver } = {};

    connection.on("kernelEvent", (message: string) => {
        let eventEnvelope = <KernelEventEnvelope>JSON.parse(message);
        let keys = Object.keys(eventObservers);
        for (let key of keys) {
            let observer = eventObservers[key];
            observer(eventEnvelope);
        }
    });

    connection.on("submitCommand", (message: string) => {
        let commandEnvelope = <KernelCommandEnvelope>JSON.parse(message);
        let keys = Object.keys(commandObservers);
        for (let key of keys) {
            let observer = commandObservers[key];
            observer(commandEnvelope);
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

        subscribeToCommands: (observer: KernelCommandEnvelopeObserver): DisposableSubscription => {
            let key = tokenGenerator.GetNewToken();
            commandObservers[key] = observer;

            let disposableSubscription: DisposableSubscription = {
                dispose: () => {
                    delete commandObservers[key];
                }
            }

            return disposableSubscription;
        },

        submitCommand: (command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> => {
            let envelope: KernelCommandEnvelope = {
                commandType: commandType,
                command: command,
                token: token,
            };
            return connection.send("submitCommand", JSON.stringify(envelope));
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
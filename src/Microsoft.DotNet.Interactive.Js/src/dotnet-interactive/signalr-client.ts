// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";

import { KernelTransport, KernelEventEnvelope, KernelEventEnvelopeObserver, DisposableSubscription, KernelCommand, KernelCommandType, KernelCommandEnvelope, SubmitCodeType } from "./contracts";
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

    let observers: { [key: string]: KernelEventEnvelopeObserver } = {};

    connection.on("kernelEvent", (message: string) => {
        let eventEnvelope = <KernelEventEnvelope>JSON.parse(message);
        let keys = Object.keys(observers);
        for (let key of keys) {
            let observer = observers[key];
            observer(eventEnvelope);
        }
    });

    await connection
        .start()
        .catch(err => console.log(err));

    let eventStream: KernelTransport = {

        subscribeToKernelEvents: (observer: KernelEventEnvelopeObserver): DisposableSubscription => {
            let key = tokenGenerator.GetNewToken();
            observers[key] = observer;

            let disposableSubscription: DisposableSubscription = {
                dispose: () => {
                    delete observers[key];
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

        dispose: (): void => {
        }
    };

    await connection.send("connect");
    return Promise.resolve(eventStream);
}
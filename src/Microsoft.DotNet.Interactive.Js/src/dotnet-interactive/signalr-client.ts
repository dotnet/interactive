// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";
import { Subject } from "rxjs";
import { KernelTransport, KernelEventEnvelope, KernelEventEvelopeObserver, DisposableSubscription, KernelCommand, KernelCommandType, KernelCommandEnvelope, SubmitCodeType } from "./contracts";


export async function signalTransportFactory(rootUrl: string): Promise<KernelTransport> {

    let hubUrl = rootUrl;
    if (hubUrl.endsWith("/")) {
        hubUrl = `${hubUrl}kernelhub`;
    } else {
        hubUrl = `${hubUrl}/kernelhub`;
    }

    let connection = new signalR.HubConnectionBuilder()
        .configureLogging(signalR.LogLevel.Debug)
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    let channel = new Subject<KernelEventEnvelope>();

    connection.on("kernelEvent", (message: string) => {
        let eventEnvelope = <KernelEventEnvelope>JSON.parse(message);
        channel.next(eventEnvelope)
    });

    await connection
        .start()
        .catch(err => console.log(err));

    let eventStream: KernelTransport = {

        subscribeToKernelEvents: (observer: KernelEventEvelopeObserver): DisposableSubscription => {
            let sub = channel.subscribe((envelope) => {
                observer(envelope);
            });

            let disposableSubscription: DisposableSubscription = {
                dispose: () => {
                    sub.unsubscribe();
                }
            }

            return disposableSubscription;
        },

        submitCommand: (command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> => {
            let envelope: KernelCommandEnvelope = {
                commandType: SubmitCodeType,
                command: command,
                token: token,
            };
            return connection.send("submitCommand", JSON.stringify(envelope));
        }
    };

    return Promise.resolve(eventStream);
}
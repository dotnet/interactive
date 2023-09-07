// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";
import { Subject } from "rxjs";
import { IKernelCommandAndEventReceiver, IKernelCommandAndEventSender, isKernelCommandEnvelope, isKernelEventEnvelope, KernelCommandAndEventReceiver, KernelCommandAndEventSender, KernelCommandOrEventEnvelope } from "./polyglot-notebooks";
import { KernelEventEnvelope, KernelCommandEnvelope } from "./polyglot-notebooks/commandsAndEvents";



export async function signalTransportFactory(rootUrl: string): Promise<{ sender: IKernelCommandAndEventSender, receiver: IKernelCommandAndEventReceiver }> {

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


    let remoteToLocalSubject = new Subject<KernelCommandOrEventEnvelope>()
    let localToRemoteSubject = new Subject<KernelCommandOrEventEnvelope>()

    // deprecated
    connection.on("kernelEvent", (message: string) => {
        let envelope = <KernelEventEnvelope>JSON.parse(message);
        remoteToLocalSubject.next(envelope);
    });

    // deprecated
    connection.on("submitCommand", (message: string) => {
        let envelope = <KernelCommandEnvelope>JSON.parse(message);
        remoteToLocalSubject.next(envelope);
    });

    connection.on("commandFromServer", (message: string) => {
        let envelope = <KernelCommandEnvelope>JSON.parse(message);
        remoteToLocalSubject.next(envelope);
    });

    connection.on("eventFromServer", (message: string) => {
        let envelope = <KernelEventEnvelope>JSON.parse(message);
        remoteToLocalSubject.next(envelope);
    });

    await connection
        .start()
        .catch(err => console.log(err));

    localToRemoteSubject.subscribe({
        next: (envelope) => {
            if (isKernelCommandEnvelope(envelope)) {
                connection.send("kernelCommandFromRemote", JSON.stringify(envelope));
            } else if (isKernelEventEnvelope(envelope)) {
                connection.send("kernelEventFromRemote", JSON.stringify(envelope));
            }
        }
    });
    let eventStream = {
        sender: KernelCommandAndEventSender.FromObserver(localToRemoteSubject),
        receiver: KernelCommandAndEventReceiver.FromObservable(remoteToLocalSubject)
    };


    await connection.send("connect");
    return Promise.resolve(eventStream);
}
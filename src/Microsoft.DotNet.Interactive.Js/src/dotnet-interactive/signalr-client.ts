// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";
import { KernelEventEnvelopeStream, KernelEventEvelopeObserver, DisposableSubscription } from "./dotnet-interactive-interfaces";
import { Subject } from "rxjs";
import { KernelEventEnvelope } from "./events";

export function signalREventStreamFactory(rootUrl: string): Promise<KernelEventEnvelopeStream> {

    let connection = new signalR.HubConnectionBuilder()
        .withUrl(`${rootUrl}/kernelhub`)
        .build();
        
    let channel = new Subject<KernelEventEnvelope>();

    connection.on("kernelEvents", (message: string) => {
        let eventEnvelope = <KernelEventEnvelope>JSON.parse(message);
        channel.next(eventEnvelope)
    });

    connection
        .start()
        .catch(err => console.log(err));

    let eventStream: KernelEventEnvelopeStream = {

        subscribe: (observer: KernelEventEvelopeObserver): DisposableSubscription => {
            let sub = channel.subscribe(observer);

            let disposableSubscription: DisposableSubscription = {
                dispose: () => {
                    sub.unsubscribe();
                }
            }

            return disposableSubscription;
        }
    };

    return Promise.resolve(eventStream);
}
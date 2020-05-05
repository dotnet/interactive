// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";
import { KernelTransport, KernelEventEvelopeObserver, DisposableSubscription } from "./dotnet-interactive-interfaces";
import { Subject } from "rxjs";
import { KernelEventEnvelope } from "./events";
import { KernelCommandEnvelope, SubmitCodeCommandType, SubmitCode } from "./commands";
import { TokenGenerator } from "./tokenGenerator";

export function signalTransportFactory(rootUrl: string): Promise<KernelTransport> {

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

    let eventStream: KernelTransport = {

        subscribeToKernelEvents: (observer: KernelEventEvelopeObserver): DisposableSubscription => {
            let sub = channel.subscribe(observer);

            let disposableSubscription: DisposableSubscription = {
                dispose: () => {
                    sub.unsubscribe();
                }
            }

            return disposableSubscription;
        },

        submidCode: (codeSubmission: { code: string, language?: string, token?: string }): Promise<void> => {
            let envelope: KernelCommandEnvelope = {
                commandType:  SubmitCodeCommandType,
                command: <SubmitCode> {
                    code: codeSubmission.language,
                    targetKernelName: codeSubmission.language
                },
                token: codeSubmission.token,
            };
            return connection.send("submitCommand", JSON.stringify(envelope));
        }
    };

    return Promise.resolve(eventStream);
}
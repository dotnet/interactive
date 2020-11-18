// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as signalR from "@microsoft/signalr";
import { env } from "process";

import { MessageTransport, LabelledKernelChannelMessageObserver, DisposableSubscription, KernelChannelMessageEnvelope, KernelChannelMessageObserver } from "./contracts";
import { TokenGenerator } from "./tokenGenerator";

// Would like to do this:
//import { parse } from './utilities';
// But can't get to that from here, so:

function parse(text: string): any {
    return JSON.parse(text, (key, value) => {
        if (key === 'rawData' && typeof value === 'string') {
            // this looks suspicously like a base64-encoded byte array; special-case this by interpreting this as a base64-encoded string
            const buffer = Buffer.from(value, 'base64');
            return Uint8Array.from(buffer.values());
        }

        return value;
    });
}

class SignalRTransport implements MessageTransport {

    private tokenGenerator = new TokenGenerator();
    private observers: { [key: string]: LabelledKernelChannelMessageObserver<object> } = {};
    private connection: signalR.HubConnection;

    async start(rootUrl: string) {
        let hubUrl = rootUrl;
        if (hubUrl.endsWith("/")) {
            hubUrl = `${hubUrl}kernelhub`;
        } else {
            hubUrl = `${hubUrl}/kernelhub`;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        this.connection.on("messages", (envelope: KernelChannelMessageEnvelope) => {
            let keys = Object.keys(this.observers);
            for (let key of keys) {
                let observer = this.observers[key];
                observer(envelope.label, envelope.payload);
            }
        });

        await this.connection
            .start()
            .catch(err => console.log(err));

        await this.connection.send("connect");
    }

    private subscribeWithFilter<T extends object>(filter: (label: string) => boolean, observer: LabelledKernelChannelMessageObserver<T>): DisposableSubscription {
        let key = this.tokenGenerator.GetNewToken();
        this.observers[key] = (messageLabel: string, message: object): void => {
            if (filter(messageLabel)) {
                let parsedMessage = <T>message;
                observer(messageLabel, parsedMessage);
            }
        };

        let disposableSubscription: DisposableSubscription = {
            dispose: () => {
                delete this.observers[key];
            }
        }

        return disposableSubscription;
    }


    subscribeToMessagesWithLabelPrefix<T extends object>(label: string, observer: LabelledKernelChannelMessageObserver<T>): DisposableSubscription {
        return this.subscribeWithFilter<T>(messageLabel => messageLabel.startsWith(label), observer);
    }

    subscribeToMessagesWithLabel<T extends object>(label: string, observer: KernelChannelMessageObserver<T>): DisposableSubscription {
        return this.subscribeWithFilter<T>(
            messageLabel => messageLabel === label,
            (_: string, message: T) => observer(message));
    }

    sendMessage<T>(label: string, message: T): Promise<void> {
        let wrappedMessage: KernelChannelMessageEnvelope = {
            label: label,
            payload: message
        };
        return this.connection.send("messages", JSON.stringify(wrappedMessage));
    }

    waitForReady(): Promise<void> {
        return Promise.resolve();
    }

    dispose(): void {
        this.connection.off("messages");
    }
}

export async function signalTransportFactory(rootUrl: string): Promise<MessageTransport> {
    let transport = new SignalRTransport();
    await transport.start(rootUrl);

    return transport;
}
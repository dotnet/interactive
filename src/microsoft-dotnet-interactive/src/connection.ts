// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import * as contracts from './contracts';
import * as disposable from './disposables';

export type KernelCommandOrEventEnvelope = contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope;

export function isKernelCommandEnvelope(commandOrEvent: KernelCommandOrEventEnvelope): commandOrEvent is contracts.KernelCommandEnvelope {
    return (<any>commandOrEvent).commandType !== undefined;
}

export function isKernelEventEnvelope(commandOrEvent: KernelCommandOrEventEnvelope): commandOrEvent is contracts.KernelEventEnvelope {
    return (<any>commandOrEvent).eventType !== undefined;
}

export interface KernelCommandAndEventSender {
    submitCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
    publishKernelEvent(eventEnvelope: contracts.KernelEventEnvelope): Promise<void>;
}

export interface KernelCommandAndEventReceiver {
    subscribeToKernelEvents(observer: contracts.KernelEventEnvelopeObserver): disposable.DisposableSubscription;
    setCommandHandler(handler: contracts.KernelCommandEnvelopeHandler): void;
}


export interface KernelCommandAndEventChannel extends KernelCommandAndEventSender, KernelCommandAndEventReceiver, disposable.Disposable {
}



export interface IKernelCommandAndEventReceiver extends rxjs.Subscribable<KernelCommandOrEventEnvelope> {

}

export interface IKernelCommandAndEventSender {
    send(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope): Promise<void>;
    get remoteHostUri(): string;
}

export class KernelCommandAndEventReceiver2 implements IKernelCommandAndEventReceiver {
    private _observable: rxjs.Subscribable<KernelCommandOrEventEnvelope>;
    private _disposables: disposable.Disposable[] = [];

    private constructor(observer: rxjs.Subscribable<KernelCommandOrEventEnvelope>) {
        this._observable = observer;
    }

    subscribe(observer: Partial<rxjs.Observer<KernelCommandOrEventEnvelope>>): rxjs.Unsubscribable {
        return this._observable.subscribe(observer);
    }

    public dispose(): void {
        for (let disposable of this._disposables) {
            disposable.dispose();
        }
    }

    public static FromObservable(observable: rxjs.Observable<KernelCommandOrEventEnvelope>): IKernelCommandAndEventReceiver {
        return new KernelCommandAndEventReceiver2(observable);
    }
}


export class KernelCommandAndEventSender2 implements IKernelCommandAndEventSender {
    private _remoteHostUri: string;
    private _sender: rxjs.Subject<KernelCommandOrEventEnvelope>;
    private constructor(remoteHostUri: string) {
        this._remoteHostUri = remoteHostUri;
        this._sender = new rxjs.Subject<KernelCommandOrEventEnvelope>();
    }
    send(kernelEventEnvelope: KernelCommandOrEventEnvelope): Promise<void> {
        this._sender.next(kernelEventEnvelope);
        return Promise.resolve();
    }
    get remoteHostUri(): string {
        return this._remoteHostUri;
    }
    public static FromObserver(observer: rxjs.Observer<KernelCommandOrEventEnvelope>): IKernelCommandAndEventSender {
        const sender = new KernelCommandAndEventSender2("");
        sender._sender.subscribe(observer);
        return sender;
    }
}
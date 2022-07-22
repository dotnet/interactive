// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import * as contracts from './contracts';
import * as disposables from './disposables';

export type KernelCommandOrEventEnvelope = contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope;

export function isKernelCommandEnvelope(commandOrEvent: KernelCommandOrEventEnvelope): commandOrEvent is contracts.KernelCommandEnvelope {
    return (<any>commandOrEvent).commandType !== undefined;
}

export function isKernelEventEnvelope(commandOrEvent: KernelCommandOrEventEnvelope): commandOrEvent is contracts.KernelEventEnvelope {
    return (<any>commandOrEvent).eventType !== undefined;
}

export interface IKernelCommandAndEventReceiver extends rxjs.Subscribable<KernelCommandOrEventEnvelope> {

}

export interface IKernelCommandAndEventSender {
    send(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope): Promise<void>;
    get remoteHostUri(): string;
}

export class KernelCommandAndEventReceiver implements IKernelCommandAndEventReceiver {
    private _observable: rxjs.Subscribable<KernelCommandOrEventEnvelope>;
    private _disposables: disposables.Disposable[] = [];

    private constructor(observer: rxjs.Observable<KernelCommandOrEventEnvelope>) {
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
        return new KernelCommandAndEventReceiver(observable);
    }

    public static FromEventListener(args: { map: (data: Event) => KernelCommandOrEventEnvelope, eventTarget: EventTarget, event: string }): IKernelCommandAndEventReceiver {
        let subject = new rxjs.Subject<KernelCommandOrEventEnvelope>();
        args.eventTarget.addEventListener(args.event, (e: Event) => {
            let mapped = args.map(e);
            subject.next(mapped);
        });
        return new KernelCommandAndEventReceiver(subject);
    }
}

function isObservable(source: any): source is rxjs.Observer<KernelCommandOrEventEnvelope> {
    return (<any>source).next !== undefined;
}

export class KernelCommandAndEventSender implements IKernelCommandAndEventSender {
    private _remoteHostUri: string;
    private _sender?: rxjs.Observer<KernelCommandOrEventEnvelope> | ((kernelEventEnvelope: KernelCommandOrEventEnvelope) => void);
    private constructor(remoteHostUri: string) {
        this._remoteHostUri = remoteHostUri;
    }
    send(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope): Promise<void> {
        if (this._sender) {
            try {
                if (typeof this._sender === "function") {
                    this._sender(kernelCommandOrEventEnvelope);
                } else if (isObservable(this._sender)) {
                    this._sender.next(kernelCommandOrEventEnvelope);
                } else {
                    return Promise.reject(new Error("Sender is not set"));
                }
            }
            catch (error) {
                return Promise.reject(error);
            }
            return Promise.resolve();
        }
        return Promise.reject(new Error("Sender is not set"));
    }

    get remoteHostUri(): string {
        return this._remoteHostUri;
    }

    public static FromObserver(observer: rxjs.Observer<KernelCommandOrEventEnvelope>): IKernelCommandAndEventSender {
        const sender = new KernelCommandAndEventSender("");
        sender._sender = observer;
        return sender;
    }

    public static FromFunction(send: (kernelEventEnvelope: KernelCommandOrEventEnvelope) => void): IKernelCommandAndEventSender {
        const sender = new KernelCommandAndEventSender("");
        sender._sender = send;
        return sender;
    }
}

export function isSetOfString(collection: any): collection is Set<string> {
    return typeof (collection) !== typeof (new Set<string>());
}

export function isArrayOfString(collection: any): collection is string[] {
    return Array.isArray(collection) && collection.length > 0 && typeof (collection[0]) === typeof ("");
}

export function tryAddUriToRoutingSlip(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUri: string): boolean {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }

    var canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => e === kernelUri);
    if (canAdd) {
        kernelCommandOrEventEnvelope.routingSlip.push(kernelUri);
        kernelCommandOrEventEnvelope.routingSlip;//?
    } else {
        kernelUri;//?
    }

    return canAdd;
}
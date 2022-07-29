// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import { CompositeKernel } from './compositeKernel';
import * as contracts from './contracts';
import * as disposables from './disposables';
import { KernelHost } from './kernelHost';
import { Logger } from './logger';

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
    }

    return canAdd;
}

export function ensureProxyForKernelInfo(kernelInfoProduced: contracts.KernelInfoProduced, compositeKernel: CompositeKernel) {
    const uriToLookup = kernelInfoProduced.kernelInfo.remoteUri ?? kernelInfoProduced.kernelInfo.uri;
    if (uriToLookup) {
        let kernel = compositeKernel.findKernelByUri(uriToLookup);
        if (!kernel) {
            // add
            if (compositeKernel.host) {
                Logger.default.info(`creating proxy for uri [${uriToLookup}] with info ${JSON.stringify(kernelInfoProduced)}`);
                kernel = compositeKernel.host.connectProxyKernelOnDefaultConnector(kernelInfoProduced.kernelInfo.localName, uriToLookup, kernelInfoProduced.kernelInfo.aliases);
            } else {
                throw new Error('no kernel host found');
            }
        } else {
            Logger.default.info(`patching proxy for uri [${uriToLookup}] with info ${JSON.stringify(kernelInfoProduced)}`);
        }

        // patch
        updateKernelInfo(kernel.kernelInfo, kernelInfoProduced.kernelInfo);
    }
}

export function isKernelInfoForProxy(kernelInfo: contracts.KernelInfo): boolean {
    const hasUri = !!kernelInfo.uri;
    const hasRemoteUri = !!kernelInfo.remoteUri;
    return hasUri && hasRemoteUri;
}

export function updateKernelInfo(destination: contracts.KernelInfo, incoming: contracts.KernelInfo) {
    destination.languageName = incoming.languageName ?? destination.languageName;
    destination.languageVersion = incoming.languageVersion ?? destination.languageVersion;

    const supportedDirectives = new Set<string>();
    const supportedCommands = new Set<string>();

    if (!destination.supportedDirectives) {
        destination.supportedDirectives = [];
    }

    if (!destination.supportedKernelCommands) {
        destination.supportedKernelCommands = [];
    }

    for (const supportedDirective of destination.supportedDirectives) {
        supportedDirectives.add(supportedDirective.name);
    }

    for (const supportedCommand of destination.supportedKernelCommands) {
        supportedCommands.add(supportedCommand.name);
    }

    for (const supportedDirective of incoming.supportedDirectives) {
        if (!supportedDirectives.has(supportedDirective.name)) {
            supportedDirectives.add(supportedDirective.name);
            destination.supportedDirectives.push(supportedDirective);
        }
    }

    for (const supportedCommand of incoming.supportedKernelCommands) {
        if (!supportedCommands.has(supportedCommand.name)) {
            supportedCommands.add(supportedCommand.name);
            destination.supportedKernelCommands.push(supportedCommand);
        }
    }
}

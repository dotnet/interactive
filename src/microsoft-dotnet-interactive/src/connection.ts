// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import { CompositeKernel } from './compositeKernel';
import * as contracts from './contracts';
import * as disposables from './disposables';
import { Disposable } from './disposables';
import { KernelType } from './kernel';
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
        const listener = (e: Event) => {
            let mapped = args.map(e);
            subject.next(mapped);
        };
        args.eventTarget.addEventListener(args.event, listener);
        const ret = new KernelCommandAndEventReceiver(subject);
        ret._disposables.push({
            dispose: () => {
                args.eventTarget.removeEventListener(args.event, listener);
            }
        });
        args.eventTarget.removeEventListener(args.event, listener);
        return ret;
    }
}

function isObservable(source: any): source is rxjs.Observer<KernelCommandOrEventEnvelope> {
    return (<any>source).next !== undefined;
}

export class KernelCommandAndEventSender implements IKernelCommandAndEventSender {
    private _sender?: rxjs.Observer<KernelCommandOrEventEnvelope> | ((kernelEventEnvelope: KernelCommandOrEventEnvelope) => void);
    private constructor() {
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

    public static FromObserver(observer: rxjs.Observer<KernelCommandOrEventEnvelope>): IKernelCommandAndEventSender {
        const sender = new KernelCommandAndEventSender();
        sender._sender = observer;
        return sender;
    }

    public static FromFunction(send: (kernelEventEnvelope: KernelCommandOrEventEnvelope) => void): IKernelCommandAndEventSender {
        const sender = new KernelCommandAndEventSender();
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



export function createKernelUri(kernelUri: string): string {
    kernelUri;//?
    const uri = new URL(kernelUri.replace("kernel:", "http:"));
    const absoluteUri = uri.toString().replace("http:", "kernel:");
    return absoluteUri;//?
}


export function stampCommandRoutingSlip(kernelCommandEnvelope: contracts.KernelCommandEnvelope, kernelUri: string) {
    stampRoutingSlip(kernelCommandEnvelope, kernelUri);
}

export function stampEventRoutingSlip(kernelEventEnvelope: contracts.KernelEventEnvelope, kernelUri: string) {
    stampRoutingSlip(kernelEventEnvelope, kernelUri);
}

function stampRoutingSlip(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUri: string) {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }
    const normalizedUri = createKernelUri(kernelUri);
    const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUri(e) === normalizedUri);
    if (canAdd) {
        kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
        kernelCommandOrEventEnvelope.routingSlip;//?
    } else {
        throw new Error(`The uri ${normalizedUri} is already in the routing slip`);
    }
}

function continueRoutingSlip(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUris: string[]): void {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }

    let i = 0;
    if (routingSlipStartsWith(kernelUris, kernelCommandOrEventEnvelope.routingSlip)) {
        i = kernelCommandOrEventEnvelope.routingSlip.length;
    }

    for (i; i < kernelUris.length; i++) {
        const normalizedUri = createKernelUri(kernelUris[i]);//?
        const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUri(e) === normalizedUri);
        if (canAdd) {
            kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
        } else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip`);
        }
    }
}


export function continueCommandRoutingSlip(kernelCommandEnvelope: contracts.KernelCommandEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(kernelCommandEnvelope, kernelUris);
}

export function continueEventRoutingSlip(kernelEventEnvelope: contracts.KernelEventEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(kernelEventEnvelope, kernelUris);
}

export function eventRoutingSlipStartsWith(thisEvent: contracts.KernelEventEnvelope, other: string[] | contracts.KernelEventEnvelope): boolean {
    const thisKernelUris = thisEvent.routingSlip ?? [];
    const otherKernelUris = (other instanceof Array ? other : other?.routingSlip) ?? [];

    return routingSlipStartsWith(thisKernelUris, otherKernelUris);
}

export function commandRoutingSlipStartsWith(thisCommand: contracts.KernelCommandEnvelope, other: string[] | contracts.KernelCommandEnvelope): boolean {
    const thisKernelUris = thisCommand.routingSlip ?? [];
    const otherKernelUris = (other instanceof Array ? other : other?.routingSlip) ?? [];

    return routingSlipStartsWith(thisKernelUris, otherKernelUris);
}


function routingSlipStartsWith(thisKernelUris: string[], otherKernelUris: string[]): boolean {
    let startsWith = true;

    if (otherKernelUris.length > 0 && thisKernelUris.length >= otherKernelUris.length) {
        for (let i = 0; i < otherKernelUris.length; i++) {
            if (createKernelUri(otherKernelUris[i]) !== createKernelUri(thisKernelUris[i])) {
                startsWith = false;
                break;
            }
        }
    }
    else {
        startsWith = false;
    }

    return startsWith;
}

export function eventRoutingSlipContains(kernlEvent: contracts.KernelEventEnvelope, kernelUri: string): boolean {
    return routingSlipContains(kernlEvent, kernelUri);
}

export function commandRoutingSlipContains(kernlEvent: contracts.KernelCommandEnvelope, kernelUri: string): boolean {
    return routingSlipContains(kernlEvent, kernelUri);
}

function routingSlipContains(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUri: string) {
    return kernelCommandOrEventEnvelope?.routingSlip?.find(e => e === createKernelUri(kernelUri)) !== undefined;
}

export function ensureOrUpdateProxyForKernelInfo(kernelInfoProduced: contracts.KernelInfoProduced, compositeKernel: CompositeKernel) {
    const uriToLookup = kernelInfoProduced.kernelInfo.remoteUri ?? kernelInfoProduced.kernelInfo.uri;
    if (uriToLookup) {
        let kernel = compositeKernel.findKernelByUri(uriToLookup);
        if (!kernel) {
            // add
            if (compositeKernel.host) {
                Logger.default.info(`creating proxy for uri [${uriToLookup}] with info ${JSON.stringify(kernelInfoProduced)}`);
                kernel = compositeKernel.host.connectProxyKernel(kernelInfoProduced.kernelInfo.localName, uriToLookup, kernelInfoProduced.kernelInfo.aliases);
            } else {
                throw new Error('no kernel host found');
            }
        } else {
            Logger.default.info(`patching proxy for uri [${uriToLookup}] with info ${JSON.stringify(kernelInfoProduced)}`);
        }

        if (kernel.kernelType === KernelType.proxy) {
            // patch
            updateKernelInfo(kernel.kernelInfo, kernelInfoProduced.kernelInfo);
        }
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

export class Connector implements Disposable {
    private readonly _listener: rxjs.Unsubscribable;
    private readonly _receiver: IKernelCommandAndEventReceiver;
    private readonly _sender: IKernelCommandAndEventSender;
    private readonly _remoteUris: Set<string> = new Set<string>();

    public get remoteHostUris(): string[] {
        return Array.from(this._remoteUris.values());
    }

    public get sender(): IKernelCommandAndEventSender {
        return this._sender;
    }

    public get receiver(): IKernelCommandAndEventReceiver {
        return this._receiver;
    }

    constructor(configuration: { receiver: IKernelCommandAndEventReceiver, sender: IKernelCommandAndEventSender, remoteUris?: string[] }) {
        this._receiver = configuration.receiver;
        this._sender = configuration.sender;
        if (configuration.remoteUris) {
            for (const remoteUri of configuration.remoteUris) {
                const uri = extractHostAndNomalize(remoteUri);
                if (uri) {
                    this._remoteUris.add(uri);
                }
            }
        }

        this._listener = this._receiver.subscribe({
            next: (kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope) => {
                if (isKernelEventEnvelope(kernelCommandOrEventEnvelope)) {
                    if (kernelCommandOrEventEnvelope.eventType === contracts.KernelInfoProducedType) {
                        const event = <contracts.KernelInfoProduced>kernelCommandOrEventEnvelope.event;
                        if (!event.kernelInfo.remoteUri) {
                            const uri = extractHostAndNomalize(event.kernelInfo.uri!);
                            if (uri) {
                                this._remoteUris.add(uri);
                            }
                        }
                    }
                    if ((kernelCommandOrEventEnvelope.routingSlip?.length ?? 0) > 0) {
                        const eventOrigin = kernelCommandOrEventEnvelope.routingSlip![0];
                        const uri = extractHostAndNomalize(eventOrigin);
                        if (uri) {
                            this._remoteUris.add(uri);
                        }
                    }
                }
            }
        });
    }

    public canReach(remoteUri: string): boolean {
        const host = extractHostAndNomalize(remoteUri);//?
        if (host) {
            return this._remoteUris.has(host);
        }
        return false;
    }
    dispose(): void {
        this._listener.unsubscribe();
    }
}

export function extractHostAndNomalize(kernelUri: string): string | undefined {
    const filter: RegExp = /(?<host>.+:\/\/[^\/]+)(\/[^\/])*/gi;
    const match = filter.exec(kernelUri); //?
    if (match?.groups?.host) {
        const host = match.groups.host;
        return host;//?
    }
    return "";
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import { CompositeKernel } from './compositeKernel';
import * as commandsAndEvents from './commandsAndEvents';
import * as disposables from './disposables';
import { Disposable } from './disposables';
import { Logger } from './logger';

export type KernelCommandOrEventEnvelope = commandsAndEvents.KernelCommandEnvelope | commandsAndEvents.KernelEventEnvelope;

export type KernelCommandOrEventEnvelopeModel = commandsAndEvents.KernelCommandEnvelopeModel | commandsAndEvents.KernelEventEnvelopeModel;

export function isKernelCommandEnvelope(commandOrEvent: KernelCommandOrEventEnvelope): commandOrEvent is commandsAndEvents.KernelCommandEnvelope {
    return (<any>commandOrEvent).commandType !== undefined;
}

export function isKernelCommandEnvelopeModel(commandOrEvent: KernelCommandOrEventEnvelopeModel): commandOrEvent is commandsAndEvents.KernelCommandEnvelopeModel {
    return (<any>commandOrEvent).commandType !== undefined;
}

export function isKernelEventEnvelope(commandOrEvent: KernelCommandOrEventEnvelope): commandOrEvent is commandsAndEvents.KernelEventEnvelope {
    return (<any>commandOrEvent).eventType !== undefined;
}

export function isKernelEventEnvelopeModel(commandOrEvent: KernelCommandOrEventEnvelopeModel): commandOrEvent is commandsAndEvents.KernelEventEnvelopeModel {
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
                const clone = kernelCommandOrEventEnvelope.clone();
                if (typeof this._sender === "function") {
                    this._sender(clone);
                } else if (isObservable(this._sender)) {
                    if (isKernelCommandEnvelope(kernelCommandOrEventEnvelope)) {
                        this._sender.next(clone);
                    } else {
                        this._sender.next(clone);
                    }
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

const onKernelInfoUpdates: ((compositeKernel: CompositeKernel) => void)[] = [];
export function registerForKernelInfoUpdates(callback: (compositeKernel: CompositeKernel) => void) {
    onKernelInfoUpdates.push(callback);
}
function notifyOfKernelInfoUpdates(compositeKernel: CompositeKernel) {
    for (const updater of onKernelInfoUpdates) {
        updater(compositeKernel);
    }
}

export function ensureOrUpdateProxyForKernelInfo(kernelInfo: commandsAndEvents.KernelInfo, compositeKernel: CompositeKernel) {
    if (kernelInfo.isProxy) {
        const host = extractHostAndNomalize(kernelInfo.remoteUri!);
        if (host === extractHostAndNomalize(compositeKernel.kernelInfo.uri)) {
            Logger.default.warn(`skippin creation of proxy for a proxy kernel : [${JSON.stringify(kernelInfo)}]`);
            return;
        }
    }
    const uriToLookup = kernelInfo.isProxy ? kernelInfo.remoteUri! : kernelInfo.uri;
    if (uriToLookup) {
        let kernel = compositeKernel.findKernelByUri(uriToLookup);
        if (!kernel) {
            // add
            if (compositeKernel.host) {
                Logger.default.info(`creating proxy for uri[${uriToLookup}]with info ${JSON.stringify(kernelInfo)}`);
                // check for clash with `kernelInfo.localName`
                kernel = compositeKernel.host.connectProxyKernel(kernelInfo.localName, uriToLookup, kernelInfo.aliases);
                updateKernelInfo(kernel.kernelInfo, kernelInfo);
            } else {
                throw new Error('no kernel host found');
            }
        } else {
            Logger.default.info(`patching proxy for uri[${uriToLookup}]with info ${JSON.stringify(kernelInfo)} `);
        }

        if (kernel.kernelInfo.isProxy) {
            // patch
            updateKernelInfo(kernel.kernelInfo, kernelInfo);
        }

        notifyOfKernelInfoUpdates(compositeKernel);
    }
}

export function isKernelInfoForProxy(kernelInfo: commandsAndEvents.KernelInfo): boolean {
    return kernelInfo.isProxy;
}

export function updateKernelInfo(destination: commandsAndEvents.KernelInfo, source: commandsAndEvents.KernelInfo) {
    destination.languageName = source.languageName ?? destination.languageName;
    destination.languageVersion = source.languageVersion ?? destination.languageVersion;
    destination.displayName = source.displayName;
    destination.isComposite = source.isComposite;

    if (source.displayName) {
        destination.displayName = source.displayName;
    }

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

    for (const supportedDirective of source.supportedDirectives) {
        if (!supportedDirectives.has(supportedDirective.name)) {
            supportedDirectives.add(supportedDirective.name);
            destination.supportedDirectives.push(supportedDirective);
        }
    }

    for (const supportedCommand of source.supportedKernelCommands) {
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
                    if (kernelCommandOrEventEnvelope.eventType === commandsAndEvents.KernelInfoProducedType) {
                        const event = <commandsAndEvents.KernelInfoProduced>kernelCommandOrEventEnvelope.event;
                        if (!event.kernelInfo.remoteUri) {
                            const uri = extractHostAndNomalize(event.kernelInfo.uri!);
                            if (uri) {
                                this._remoteUris.add(uri);
                            }
                        }
                    }
                    const eventRoutingSlip = kernelCommandOrEventEnvelope.routingSlip.toArray();
                    if ((eventRoutingSlip.length ?? 0) > 0) {
                        const eventOrigin = eventRoutingSlip![0];
                        const uri = extractHostAndNomalize(eventOrigin);
                        if (uri) {
                            this._remoteUris.add(uri);
                        }
                    }
                }
            }
        });
    }

    public addRemoteHostUri(remoteUri: string) {
        const uri = extractHostAndNomalize(remoteUri);
        if (uri) {
            this._remoteUris.add(uri);
        }
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

export function extractHostAndNomalize(kernelUri: string): string {
    const filter: RegExp = /(?<host>.+:\/\/[^\/]+)(\/[^\/])*/gi;
    const match = filter.exec(kernelUri); //?
    if (match?.groups?.host) {
        const host = match.groups.host;
        return host;//?
    }
    return "";
}

export function Serialize<T>(source: T): string {
    return JSON.stringify(source, function (key, value) {
        //handling NaN, Infinity and -Infinity
        const processed = SerializeNumberLiterals(value);
        return processed;
    });
}

export function SerializeNumberLiterals(value: any): string {
    if (value !== value) {
        return "NaN";
    } else if (value === Infinity) {
        return "Infinity";
    } else if (value === -Infinity) {
        return "-Infinity";
    }
    return value;
}

export function Deserialize(json: string): any {
    return JSON.parse(json, function (key, value) {
        //handling NaN, Infinity and -Infinity
        const deserialized = DeserializeNumberLiterals(value);
        return deserialized;
    });
}


export function DeserializeNumberLiterals(value: any): any {
    if (value === "NaN") {
        return NaN;
    } else if (value === "Infinity") {
        return Infinity;
    } else if (value === "-Infinity") {
        return -Infinity;
    }
    return value;
}

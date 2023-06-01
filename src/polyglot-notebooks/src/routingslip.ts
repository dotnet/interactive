// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from './commandsAndEvents';
import { URI } from 'vscode-uri';
import { KernelCommandOrEventEnvelope, isKernelCommandEnvelope } from './connection';


export function createKernelUri(kernelUri: string): string {
    kernelUri;//?
    const uri = URI.parse(kernelUri);
    uri.authority;//?
    uri.path;//?
    let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
    return absoluteUri;//?
}

export function createKernelUriWithQuery(kernelUri: string): string {
    kernelUri;//?
    const uri = URI.parse(kernelUri);
    uri.authority;//?
    uri.path;//?
    let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
    if (uri.query) {
        absoluteUri += `?${uri.query}`;
    }
    return absoluteUri;//?
}
export function getTag(kernelUri: string): string | undefined {
    const uri = URI.parse(kernelUri);
    if (uri.query) {//?
        const parts = uri.query.split("tag=");
        if (parts.length > 1) {
            return parts[1];
        }
    }
    return undefined;
}

export function stampCommandRoutingSlipAsArrived(kernelCommandEnvelope: commandsAndEvents.KernelCommandEnvelope, kernelUri: string) {
    stampCommandRoutingSlipAs(kernelCommandEnvelope, kernelUri, "arrived");
}

export function stampCommandRoutingSlip(kernelCommandEnvelope: commandsAndEvents.KernelCommandEnvelope, kernelUri: string) {
    if (kernelCommandEnvelope.routingSlip === undefined || kernelCommandEnvelope.routingSlip === null) {
        throw new Error("The command does not have a routing slip");
    }
    kernelCommandEnvelope.routingSlip;//?
    kernelUri;//?
    let absoluteUriWithQuery = createKernelUriWithQuery(kernelUri); //?
    let absoluteUriWithoutQuery = createKernelUri(kernelUri); //?
    let tag = getTag(kernelUri);
    if (kernelCommandEnvelope.routingSlip.find(e => e === absoluteUriWithQuery)) {
        // the uri is already in the routing slip
        throw Error(`The uri ${absoluteUriWithQuery} is already in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
    } else if (!tag && kernelCommandEnvelope.routingSlip.find(e => e.startsWith(absoluteUriWithoutQuery))) {
        // there is no tag, this is to complete
        kernelCommandEnvelope.routingSlip.push(absoluteUriWithQuery);
    }
    else if (tag) {
        // there is atag and this uri is not found
        kernelCommandEnvelope.routingSlip.push(absoluteUriWithQuery);
    }
    else {
        throw new Error(`The uri ${absoluteUriWithQuery} is not in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
    }
}

export function stampEventRoutingSlip(kernelEventEnvelope: commandsAndEvents.KernelEventEnvelope, kernelUri: string) {
    stampRoutingSlip(kernelEventEnvelope, kernelUri);
}

function stampCommandRoutingSlipAs(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUri: string, tag: string) {
    const absoluteUri = `${createKernelUri(kernelUri)}?tag=${tag}`;//?
    stampRoutingSlip(kernelCommandOrEventEnvelope, absoluteUri);
}


function stampRoutingSlip(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUri: string) {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }
    const normalizedUri = createKernelUriWithQuery(kernelUri);
    const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUriWithQuery(e) === normalizedUri);
    if (canAdd) {
        kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
        kernelCommandOrEventEnvelope.routingSlip;//?
    } else {
        throw new Error(`The uri ${normalizedUri} is already in the routing slip [${kernelCommandOrEventEnvelope.routingSlip}]`);
    }
}

function continueRoutingSlip(toContinue: KernelCommandOrEventEnvelope, kernelUris: string[]): void {
    if (toContinue.routingSlip === undefined || toContinue.routingSlip === null) {
        toContinue.routingSlip = [];
    }

    let continuationUris = createRoutingSlip(kernelUris);

    if (routingSlipStartsWith(continuationUris, toContinue.routingSlip)) {
        continuationUris = continuationUris.slice(toContinue.routingSlip.length);
    }

    const original = [...toContinue.routingSlip];
    for (let i = 0; i < continuationUris.length; i++) {
        const normalizedUri = continuationUris[i];//?
        const canAdd = !toContinue.routingSlip.find(e => createKernelUriWithQuery(e) === normalizedUri);
        if (canAdd) {
            if (isKernelCommandEnvelope(toContinue)) {
                stampCommandRoutingSlip(toContinue, normalizedUri);
            } else {
                stampEventRoutingSlip(toContinue, normalizedUri);
            }
        } else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${original}], cannot continue with routing slip [${kernelUris.map(e => createKernelUri(e))}]`);
        }
    }
}

export function continueCommandRoutingSlip(toContinue: commandsAndEvents.KernelCommandEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(toContinue, kernelUris);
}

export function continueEventRoutingSlip(toContinue: commandsAndEvents.KernelEventEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(toContinue, kernelUris);
}

export function createRoutingSlip(kernelUris: string[]): string[] {
    return Array.from(new Set(kernelUris.map(e => createKernelUriWithQuery(e))));
}

export function eventRoutingSlipStartsWith(thisEvent: commandsAndEvents.KernelEventEnvelope, other: string[] | commandsAndEvents.KernelEventEnvelope): boolean {
    const thisKernelUris = thisEvent.routingSlip ?? [];
    const otherKernelUris = (other instanceof Array ? other : other?.routingSlip) ?? [];

    return routingSlipStartsWith(thisKernelUris, otherKernelUris);
}

export function commandRoutingSlipStartsWith(thisCommand: commandsAndEvents.KernelCommandEnvelope, other: string[] | commandsAndEvents.KernelCommandEnvelope): boolean {
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

export function eventRoutingSlipContains(kernlEvent: commandsAndEvents.KernelEventEnvelope, kernelUri: string, ignoreQuery: boolean = false): boolean {
    return routingSlipContains(kernlEvent.routingSlip || [], kernelUri, ignoreQuery);
}

export function commandRoutingSlipContains(kernlEvent: commandsAndEvents.KernelCommandEnvelope, kernelUri: string, ignoreQuery: boolean = false): boolean {
    return routingSlipContains(kernlEvent.routingSlip || [], kernelUri, ignoreQuery);
}

function routingSlipContains(routingSlip: string[], kernelUri: string, ignoreQuery: boolean = false): boolean {
    const normalizedUri = ignoreQuery ? createKernelUri(kernelUri) : createKernelUriWithQuery(kernelUri);
    return routingSlip.find(e => normalizedUri === (!ignoreQuery ? createKernelUriWithQuery(e) : createKernelUri(e))) !== undefined;
}

export abstract class RoutingSlip {
    private _uris: string[] = [];

    protected get uris(): string[] {
        return this._uris;
    }

    public contains(kernelUri: string, ignoreQuery: boolean = false): boolean {
        return routingSlipContains(this._uris, kernelUri, ignoreQuery);
    }

    public startsWith(other: string[] | RoutingSlip): boolean {
        if (other instanceof Array) {
            return routingSlipStartsWith(this._uris, other);
        } else {
            return routingSlipStartsWith(this._uris, other._uris);
        }
    }

    public continueWith(other: string[] | RoutingSlip): void {
        let otherUris = (other instanceof Array ? other : other._uris) || [];
        if (otherUris.length > 0) {
            if (routingSlipStartsWith(otherUris, this._uris)) {
                otherUris = otherUris.slice(this._uris.length);
            }
        }

        for (let i = 0; i < otherUris.length; i++) {
            if (!this.contains(otherUris[i])) {
                this._uris.push(otherUris[i]);
            } else {
                throw new Error(`The uri ${otherUris[i]} is already in the routing slip [${this._uris}], cannot continue with routing slip [${otherUris}]`);
            }
        }
    }

    public toArray(): string[] {
        return [...this._uris];
    }

    public abstract stamp(kernelUri: string): void;
}

export class CommandRoutingSlip extends RoutingSlip {
    constructor() {
        super();
    }

    public stampAsArrived(kernelUri: string): void {
        this.stampAs(kernelUri, "arrived");

    }

    public override stamp(kernelUri: string): void {
        this.stampAs(kernelUri);
    }

    private stampAs(kernelUri: string, tag?: string): void {
        if (tag) {
            const absoluteUriWithQuery = `${createKernelUri(kernelUri)}?tag=${tag}`;
            const absoluteUriWithoutQuery = createKernelUri(kernelUri);
            if (this.uris.find(e => e.startsWith(absoluteUriWithoutQuery))) {
                throw new Error(`The uri ${absoluteUriWithQuery} is already in the routing slip [${this.uris}]`);
            } else {
                this.uris.push(absoluteUriWithQuery);
            }
        } else {
            const absoluteUriWithQuery = `${createKernelUri(kernelUri)}?tag=arrived`;
            const absoluteUriWithoutQuery = createKernelUri(kernelUri);
            if (!this.uris.find(e => e.startsWith(absoluteUriWithQuery))) {
                throw new Error(`The uri ${absoluteUriWithQuery} is not in the routing slip [${this.uris}]`);
            } else if (this.uris.find(e => e === absoluteUriWithoutQuery)) {
                throw new Error(`The uri ${absoluteUriWithoutQuery} is already in the routing slip [${this.uris}]`);
            } else {
                this.uris.push(absoluteUriWithoutQuery);
            }
        }
    }
}

export class EventRoutingSlip extends RoutingSlip {
    constructor() {
        super();
    }

    public override stamp(kernelUri: string): void {
        const normalizedUri = createKernelUriWithQuery(kernelUri);
        const canAdd = !this.uris.find(e => createKernelUriWithQuery(e) === normalizedUri);
        if (canAdd) {
            this.uris.push(normalizedUri);
            this.uris;//?
        } else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${this.uris}]`);
        }
    }
}
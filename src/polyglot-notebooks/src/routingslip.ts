// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as commandsAndEvents from './commandsAndEvents';
import { URI } from 'vscode-uri';
import { KernelCommandOrEventEnvelope, isKernelCommandEnvelope } from './connection';


export function createKernelUri(kernelUri: string): string {
    const uri = URI.parse(kernelUri);
    uri.authority;
    uri.path;
    let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
    return absoluteUri;
}

export function createKernelUriWithQuery(kernelUri: string): string {
    const uri = URI.parse(kernelUri);
    uri.authority;
    uri.path;
    let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
    if (uri.query) {
        absoluteUri += `?${uri.query}`;
    }
    return absoluteUri;
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

export function createRoutingSlip(kernelUris: string[]): string[] {
    return Array.from(new Set(kernelUris.map(e => createKernelUriWithQuery(e))));
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

function routingSlipContains(routingSlip: string[], kernelUri: string, ignoreQuery: boolean = false): boolean {
    const normalizedUri = ignoreQuery ? createKernelUri(kernelUri) : createKernelUriWithQuery(kernelUri);
    return routingSlip.find(e => normalizedUri === (!ignoreQuery ? createKernelUriWithQuery(e) : createKernelUri(e))) !== undefined;
}

export abstract class RoutingSlip {
    private _uris: string[] = [];

    protected get uris(): string[] {
        return this._uris;
    }

    protected set uris(value: string[]) {
        this._uris = value;
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

    public static fromUris(uris: string[]): CommandRoutingSlip {
        const routingSlip = new CommandRoutingSlip();
        routingSlip.uris = uris;
        return routingSlip;
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

    public static fromUris(uris: string[]): EventRoutingSlip {
        const routingSlip = new EventRoutingSlip();
        routingSlip.uris = uris;
        return routingSlip;
    }

    public override stamp(kernelUri: string): void {
        const normalizedUri = createKernelUriWithQuery(kernelUri);
        const canAdd = !this.uris.find(e => createKernelUriWithQuery(e) === normalizedUri);
        if (canAdd) {
            this.uris.push(normalizedUri);
            this.uris;
        } else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${this.uris}]`);
        }
    }
}
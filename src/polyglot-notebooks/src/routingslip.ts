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

function continueRoutingSlip(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUris: string[]): void {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }

    let toContinue = createRoutingSlip(kernelUris);

    if (routingSlipStartsWith(toContinue, kernelCommandOrEventEnvelope.routingSlip)) {
        toContinue = toContinue.slice(kernelCommandOrEventEnvelope.routingSlip.length);
    }

    const original = [...kernelCommandOrEventEnvelope.routingSlip];
    for (let i = 0; i < toContinue.length; i++) {
        const normalizedUri = toContinue[i];//?
        const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUriWithQuery(e) === normalizedUri);
        if (canAdd) {
            if (isKernelCommandEnvelope(kernelCommandOrEventEnvelope)) {
                stampCommandRoutingSlip(kernelCommandOrEventEnvelope, normalizedUri);
            } else {
                stampEventRoutingSlip(kernelCommandOrEventEnvelope, normalizedUri);
            }
        } else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${original}], cannot continue with routing slip [${kernelUris.map(e => createKernelUri(e))}]`);
        }
    }
}

export function continueCommandRoutingSlip(kernelCommandEnvelope: commandsAndEvents.KernelCommandEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(kernelCommandEnvelope, kernelUris);
}

export function continueEventRoutingSlip(kernelEventEnvelope: commandsAndEvents.KernelEventEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(kernelEventEnvelope, kernelUris);
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
    return routingSlipContains(kernlEvent, kernelUri, ignoreQuery);
}

export function commandRoutingSlipContains(kernlEvent: commandsAndEvents.KernelCommandEnvelope, kernelUri: string, ignoreQuery: boolean = false): boolean {
    return routingSlipContains(kernlEvent, kernelUri, ignoreQuery);
}

function routingSlipContains(kernelCommandOrEventEnvelope: KernelCommandOrEventEnvelope, kernelUri: string, ignoreQuery: boolean = false): boolean {
    const normalizedUri = ignoreQuery ? createKernelUri(kernelUri) : createKernelUriWithQuery(kernelUri);
    return kernelCommandOrEventEnvelope?.routingSlip?.find(e => normalizedUri === (!ignoreQuery ? createKernelUriWithQuery(e) : createKernelUri(e))) !== undefined;
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from './contracts';
import { URI } from 'vscode-uri';
import { KernelCommandOrEventEnvelope } from './connection';
import { throwError } from 'rxjs';


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

export function stampCommandRoutingSlipAsArrived(kernelCommandEnvelope: contracts.KernelCommandEnvelope, kernelUri: string) {
    stampCommandRoutingSlipAs(kernelCommandEnvelope, kernelUri, "arrived");
}



export function stampCommandRoutingSlip(kernelCommandEnvelope: contracts.KernelCommandEnvelope, kernelUri: string) {
    if (kernelCommandEnvelope.routingSlip === undefined || kernelCommandEnvelope.routingSlip === null) {
        throw new Error("The command does not have a routing slip");
    }
    kernelCommandEnvelope.routingSlip;//?
    kernelUri;//?
    let absoluteUri = createKernelUri(kernelUri); //?
    if (kernelCommandEnvelope.routingSlip.find(e => e === absoluteUri)) {
        throw Error(`The uri ${absoluteUri} is already in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
    } else if (kernelCommandEnvelope.routingSlip.find(e => e.startsWith(absoluteUri))) {
        kernelCommandEnvelope.routingSlip.push(absoluteUri);
    }
    else {
        throw new Error(`The uri ${absoluteUri} is not in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
    }
}

export function stampEventRoutingSlip(kernelEventEnvelope: contracts.KernelEventEnvelope, kernelUri: string) {
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
        const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUri(e) === normalizedUri);
        if (canAdd) {
            kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
        } else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${original}], cannot continue with routing slip [${kernelUris.map(e => createKernelUri(e))}]`);
        }
    }
}

export function continueCommandRoutingSlip(kernelCommandEnvelope: contracts.KernelCommandEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(kernelCommandEnvelope, kernelUris);
}

export function continueEventRoutingSlip(kernelEventEnvelope: contracts.KernelEventEnvelope, kernelUris: string[]): void {
    continueRoutingSlip(kernelEventEnvelope, kernelUris);
}

export function createRoutingSlip(kernelUris: string[]): string[] {
    return Array.from(new Set(kernelUris.map(e => createKernelUri(e))));
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
    const normalizedUri = createKernelUri(kernelUri);
    return kernelCommandOrEventEnvelope?.routingSlip?.find(e => normalizedUri === createKernelUri(e)) !== undefined;
}
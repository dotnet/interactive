"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.commandRoutingSlipContains = exports.eventRoutingSlipContains = exports.commandRoutingSlipStartsWith = exports.eventRoutingSlipStartsWith = exports.createRoutingSlip = exports.continueEventRoutingSlip = exports.continueCommandRoutingSlip = exports.stampEventRoutingSlip = exports.stampCommandRoutingSlip = exports.stampCommandRoutingSlipAsArrived = exports.createKernelUriWithQuery = exports.createKernelUri = void 0;
const vscode_uri_1 = require("vscode-uri");
function createKernelUri(kernelUri) {
    kernelUri; //?
    const uri = vscode_uri_1.URI.parse(kernelUri);
    uri.authority; //?
    uri.path; //?
    let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
    return absoluteUri; //?
}
exports.createKernelUri = createKernelUri;
function createKernelUriWithQuery(kernelUri) {
    kernelUri; //?
    const uri = vscode_uri_1.URI.parse(kernelUri);
    uri.authority; //?
    uri.path; //?
    let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
    if (uri.query) {
        absoluteUri += `?${uri.query}`;
    }
    return absoluteUri; //?
}
exports.createKernelUriWithQuery = createKernelUriWithQuery;
function stampCommandRoutingSlipAsArrived(kernelCommandEnvelope, kernelUri) {
    stampCommandRoutingSlipAs(kernelCommandEnvelope, kernelUri, "arrived");
}
exports.stampCommandRoutingSlipAsArrived = stampCommandRoutingSlipAsArrived;
function stampCommandRoutingSlip(kernelCommandEnvelope, kernelUri) {
    if (kernelCommandEnvelope.routingSlip === undefined || kernelCommandEnvelope.routingSlip === null) {
        throw new Error("The command does not have a routing slip");
    }
    kernelCommandEnvelope.routingSlip; //?
    kernelUri; //?
    let absoluteUri = createKernelUri(kernelUri); //?
    if (kernelCommandEnvelope.routingSlip.find(e => e === absoluteUri)) {
        throw Error(`The uri ${absoluteUri} is already in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
    }
    else if (kernelCommandEnvelope.routingSlip.find(e => e.startsWith(absoluteUri))) {
        kernelCommandEnvelope.routingSlip.push(absoluteUri);
    }
    else {
        throw new Error(`The uri ${absoluteUri} is not in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
    }
}
exports.stampCommandRoutingSlip = stampCommandRoutingSlip;
function stampEventRoutingSlip(kernelEventEnvelope, kernelUri) {
    stampRoutingSlip(kernelEventEnvelope, kernelUri);
}
exports.stampEventRoutingSlip = stampEventRoutingSlip;
function stampCommandRoutingSlipAs(kernelCommandOrEventEnvelope, kernelUri, tag) {
    const absoluteUri = `${createKernelUri(kernelUri)}?tag=${tag}`; //?
    stampRoutingSlip(kernelCommandOrEventEnvelope, absoluteUri);
}
function stampRoutingSlip(kernelCommandOrEventEnvelope, kernelUri) {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }
    const normalizedUri = createKernelUriWithQuery(kernelUri);
    const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUriWithQuery(e) === normalizedUri);
    if (canAdd) {
        kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
        kernelCommandOrEventEnvelope.routingSlip; //?
    }
    else {
        throw new Error(`The uri ${normalizedUri} is already in the routing slip [${kernelCommandOrEventEnvelope.routingSlip}]`);
    }
}
function continueRoutingSlip(kernelCommandOrEventEnvelope, kernelUris) {
    if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
        kernelCommandOrEventEnvelope.routingSlip = [];
    }
    let toContinue = createRoutingSlip(kernelUris);
    if (routingSlipStartsWith(toContinue, kernelCommandOrEventEnvelope.routingSlip)) {
        toContinue = toContinue.slice(kernelCommandOrEventEnvelope.routingSlip.length);
    }
    const original = [...kernelCommandOrEventEnvelope.routingSlip];
    for (let i = 0; i < toContinue.length; i++) {
        const normalizedUri = toContinue[i]; //?
        const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUri(e) === normalizedUri);
        if (canAdd) {
            kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
        }
        else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${original}], cannot continue with routing slip [${kernelUris.map(e => createKernelUri(e))}]`);
        }
    }
}
function continueCommandRoutingSlip(kernelCommandEnvelope, kernelUris) {
    continueRoutingSlip(kernelCommandEnvelope, kernelUris);
}
exports.continueCommandRoutingSlip = continueCommandRoutingSlip;
function continueEventRoutingSlip(kernelEventEnvelope, kernelUris) {
    continueRoutingSlip(kernelEventEnvelope, kernelUris);
}
exports.continueEventRoutingSlip = continueEventRoutingSlip;
function createRoutingSlip(kernelUris) {
    return Array.from(new Set(kernelUris.map(e => createKernelUri(e))));
}
exports.createRoutingSlip = createRoutingSlip;
function eventRoutingSlipStartsWith(thisEvent, other) {
    var _a, _b;
    const thisKernelUris = (_a = thisEvent.routingSlip) !== null && _a !== void 0 ? _a : [];
    const otherKernelUris = (_b = (other instanceof Array ? other : other === null || other === void 0 ? void 0 : other.routingSlip)) !== null && _b !== void 0 ? _b : [];
    return routingSlipStartsWith(thisKernelUris, otherKernelUris);
}
exports.eventRoutingSlipStartsWith = eventRoutingSlipStartsWith;
function commandRoutingSlipStartsWith(thisCommand, other) {
    var _a, _b;
    const thisKernelUris = (_a = thisCommand.routingSlip) !== null && _a !== void 0 ? _a : [];
    const otherKernelUris = (_b = (other instanceof Array ? other : other === null || other === void 0 ? void 0 : other.routingSlip)) !== null && _b !== void 0 ? _b : [];
    return routingSlipStartsWith(thisKernelUris, otherKernelUris);
}
exports.commandRoutingSlipStartsWith = commandRoutingSlipStartsWith;
function routingSlipStartsWith(thisKernelUris, otherKernelUris) {
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
function eventRoutingSlipContains(kernlEvent, kernelUri, ignoreQuery = false) {
    return routingSlipContains(kernlEvent, kernelUri, ignoreQuery);
}
exports.eventRoutingSlipContains = eventRoutingSlipContains;
function commandRoutingSlipContains(kernlEvent, kernelUri, ignoreQuery = false) {
    return routingSlipContains(kernlEvent, kernelUri, ignoreQuery);
}
exports.commandRoutingSlipContains = commandRoutingSlipContains;
function routingSlipContains(kernelCommandOrEventEnvelope, kernelUri, ignoreQuery = false) {
    var _a;
    const normalizedUri = ignoreQuery ? createKernelUri(kernelUri) : createKernelUriWithQuery(kernelUri);
    return ((_a = kernelCommandOrEventEnvelope === null || kernelCommandOrEventEnvelope === void 0 ? void 0 : kernelCommandOrEventEnvelope.routingSlip) === null || _a === void 0 ? void 0 : _a.find(e => normalizedUri === (!ignoreQuery ? createKernelUriWithQuery(e) : createKernelUri(e)))) !== undefined;
}
//# sourceMappingURL=routingslip.js.map
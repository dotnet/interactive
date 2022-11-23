"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.extractHostAndNomalize = exports.Connector = exports.updateKernelInfo = exports.isKernelInfoForProxy = exports.ensureOrUpdateProxyForKernelInfo = exports.onKernelInfoUpdates = exports.isArrayOfString = exports.isSetOfString = exports.KernelCommandAndEventSender = exports.KernelCommandAndEventReceiver = exports.isKernelEventEnvelope = exports.isKernelCommandEnvelope = void 0;
const rxjs = require("rxjs");
const contracts = require("./contracts");
const kernel_1 = require("./kernel");
const logger_1 = require("./logger");
function isKernelCommandEnvelope(commandOrEvent) {
    return commandOrEvent.commandType !== undefined;
}
exports.isKernelCommandEnvelope = isKernelCommandEnvelope;
function isKernelEventEnvelope(commandOrEvent) {
    return commandOrEvent.eventType !== undefined;
}
exports.isKernelEventEnvelope = isKernelEventEnvelope;
class KernelCommandAndEventReceiver {
    constructor(observer) {
        this._disposables = [];
        this._observable = observer;
    }
    subscribe(observer) {
        return this._observable.subscribe(observer);
    }
    dispose() {
        for (let disposable of this._disposables) {
            disposable.dispose();
        }
    }
    static FromObservable(observable) {
        return new KernelCommandAndEventReceiver(observable);
    }
    static FromEventListener(args) {
        let subject = new rxjs.Subject();
        const listener = (e) => {
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
exports.KernelCommandAndEventReceiver = KernelCommandAndEventReceiver;
function isObservable(source) {
    return source.next !== undefined;
}
class KernelCommandAndEventSender {
    constructor() {
    }
    send(kernelCommandOrEventEnvelope) {
        if (this._sender) {
            try {
                const serislized = JSON.parse(JSON.stringify(kernelCommandOrEventEnvelope));
                if (typeof this._sender === "function") {
                    this._sender(serislized);
                }
                else if (isObservable(this._sender)) {
                    this._sender.next(serislized);
                }
                else {
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
    static FromObserver(observer) {
        const sender = new KernelCommandAndEventSender();
        sender._sender = observer;
        return sender;
    }
    static FromFunction(send) {
        const sender = new KernelCommandAndEventSender();
        sender._sender = send;
        return sender;
    }
}
exports.KernelCommandAndEventSender = KernelCommandAndEventSender;
function isSetOfString(collection) {
    return typeof (collection) !== typeof (new Set());
}
exports.isSetOfString = isSetOfString;
function isArrayOfString(collection) {
    return Array.isArray(collection) && collection.length > 0 && typeof (collection[0]) === typeof ("");
}
exports.isArrayOfString = isArrayOfString;
exports.onKernelInfoUpdates = [];
function ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, compositeKernel) {
    var _a;
    const uriToLookup = (_a = kernelInfoProduced.kernelInfo.uri) !== null && _a !== void 0 ? _a : kernelInfoProduced.kernelInfo.remoteUri;
    if (uriToLookup) {
        let kernel = compositeKernel.findKernelByUri(uriToLookup);
        if (!kernel) {
            // add
            if (compositeKernel.host) {
                logger_1.Logger.default.info(`creating proxy for uri[${uriToLookup}]with info ${JSON.stringify(kernelInfoProduced)}`);
                // check for clash with `kernelInfo.localName`
                kernel = compositeKernel.host.connectProxyKernel(kernelInfoProduced.kernelInfo.localName, uriToLookup, kernelInfoProduced.kernelInfo.aliases);
            }
            else {
                throw new Error('no kernel host found');
            }
        }
        else {
            logger_1.Logger.default.info(`patching proxy for uri[${uriToLookup}]with info ${JSON.stringify(kernelInfoProduced)} `);
        }
        if (kernel.kernelType === kernel_1.KernelType.proxy) {
            // patch
            updateKernelInfo(kernel.kernelInfo, kernelInfoProduced.kernelInfo);
        }
        for (const updater of exports.onKernelInfoUpdates) {
            updater(compositeKernel);
        }
    }
}
exports.ensureOrUpdateProxyForKernelInfo = ensureOrUpdateProxyForKernelInfo;
function isKernelInfoForProxy(kernelInfo) {
    const hasUri = !!kernelInfo.uri;
    const hasRemoteUri = !!kernelInfo.remoteUri;
    return hasUri && hasRemoteUri;
}
exports.isKernelInfoForProxy = isKernelInfoForProxy;
function updateKernelInfo(destination, incoming) {
    var _a, _b;
    destination.languageName = (_a = incoming.languageName) !== null && _a !== void 0 ? _a : destination.languageName;
    destination.languageVersion = (_b = incoming.languageVersion) !== null && _b !== void 0 ? _b : destination.languageVersion;
    destination.displayName = incoming.displayName;
    const supportedDirectives = new Set();
    const supportedCommands = new Set();
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
exports.updateKernelInfo = updateKernelInfo;
class Connector {
    constructor(configuration) {
        this._remoteUris = new Set();
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
            next: (kernelCommandOrEventEnvelope) => {
                var _a, _b;
                if (isKernelEventEnvelope(kernelCommandOrEventEnvelope)) {
                    if (kernelCommandOrEventEnvelope.eventType === contracts.KernelInfoProducedType) {
                        const event = kernelCommandOrEventEnvelope.event;
                        if (!event.kernelInfo.remoteUri) {
                            const uri = extractHostAndNomalize(event.kernelInfo.uri);
                            if (uri) {
                                this._remoteUris.add(uri);
                            }
                        }
                    }
                    if (((_b = (_a = kernelCommandOrEventEnvelope.routingSlip) === null || _a === void 0 ? void 0 : _a.length) !== null && _b !== void 0 ? _b : 0) > 0) {
                        const eventOrigin = kernelCommandOrEventEnvelope.routingSlip[0];
                        const uri = extractHostAndNomalize(eventOrigin);
                        if (uri) {
                            this._remoteUris.add(uri);
                        }
                    }
                }
            }
        });
    }
    get remoteHostUris() {
        return Array.from(this._remoteUris.values());
    }
    get sender() {
        return this._sender;
    }
    get receiver() {
        return this._receiver;
    }
    canReach(remoteUri) {
        const host = extractHostAndNomalize(remoteUri); //?
        if (host) {
            return this._remoteUris.has(host);
        }
        return false;
    }
    dispose() {
        this._listener.unsubscribe();
    }
}
exports.Connector = Connector;
function extractHostAndNomalize(kernelUri) {
    var _a;
    const filter = /(?<host>.+:\/\/[^\/]+)(\/[^\/])*/gi;
    const match = filter.exec(kernelUri); //?
    if ((_a = match === null || match === void 0 ? void 0 : match.groups) === null || _a === void 0 ? void 0 : _a.host) {
        const host = match.groups.host;
        return host; //?
    }
    return "";
}
exports.extractHostAndNomalize = extractHostAndNomalize;
//# sourceMappingURL=connection.js.map
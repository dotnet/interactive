"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.KernelHost = void 0;
const contracts = require("./contracts");
const connection = require("./connection");
const routingSlip = require("./routingslip");
const proxyKernel_1 = require("./proxyKernel");
const logger_1 = require("./logger");
const kernelScheduler_1 = require("./kernelScheduler");
class KernelHost {
    constructor(kernel, sender, receiver, hostUri) {
        this._remoteUriToKernel = new Map();
        this._uriToKernel = new Map();
        this._kernelToKernelInfo = new Map();
        this._connectors = [];
        this._kernel = kernel;
        this._uri = routingSlip.createKernelUri(hostUri || "kernel://vscode");
        this._kernel.host = this;
        this._scheduler = new kernelScheduler_1.KernelScheduler();
        this._defaultConnector = new connection.Connector({ sender, receiver });
        this._connectors.push(this._defaultConnector);
    }
    get defaultConnector() {
        return this._defaultConnector;
    }
    get uri() {
        return this._uri;
    }
    tryGetKernelByRemoteUri(remoteUri) {
        return this._remoteUriToKernel.get(remoteUri);
    }
    trygetKernelByOriginUri(originUri) {
        return this._uriToKernel.get(originUri);
    }
    tryGetKernelInfo(kernel) {
        return this._kernelToKernelInfo.get(kernel);
    }
    addKernelInfo(kernel, kernelInfo) {
        kernelInfo.uri = routingSlip.createKernelUri(`${this._uri}${kernel.name}`);
        this._kernelToKernelInfo.set(kernel, kernelInfo);
        this._uriToKernel.set(kernelInfo.uri, kernel);
    }
    getKernel(kernelCommandEnvelope) {
        var _a;
        const uriToLookup = (_a = kernelCommandEnvelope.command.destinationUri) !== null && _a !== void 0 ? _a : kernelCommandEnvelope.command.originUri;
        let kernel = undefined;
        if (uriToLookup) {
            kernel = this._kernel.findKernelByUri(uriToLookup);
        }
        if (!kernel) {
            if (kernelCommandEnvelope.command.targetKernelName) {
                kernel = this._kernel.findKernelByName(kernelCommandEnvelope.command.targetKernelName);
            }
        }
        kernel !== null && kernel !== void 0 ? kernel : (kernel = this._kernel);
        logger_1.Logger.default.info(`Using Kernel ${kernel.name}`);
        return kernel;
    }
    connectProxyKernelOnDefaultConnector(localName, remoteKernelUri, aliases) {
        return this.connectProxyKernelOnConnector(localName, this._defaultConnector.sender, this._defaultConnector.receiver, remoteKernelUri, aliases);
    }
    tryAddConnector(connector) {
        if (!connector.remoteUris) {
            this._connectors.push(new connection.Connector(connector));
            return true;
        }
        else {
            const found = connector.remoteUris.find(uri => this._connectors.find(c => c.canReach(uri)));
            if (!found) {
                this._connectors.push(new connection.Connector(connector));
                return true;
            }
            return false;
        }
    }
    tryRemoveConnector(connector) {
        if (!connector.remoteUris) {
            for (let uri of connector.remoteUris) {
                const index = this._connectors.findIndex(c => c.canReach(uri));
                if (index >= 0) {
                    this._connectors.splice(index, 1);
                }
            }
            return true;
        }
        else {
            return false;
        }
    }
    connectProxyKernel(localName, remoteKernelUri, aliases) {
        this._connectors; //?
        const connector = this._connectors.find(c => c.canReach(remoteKernelUri));
        if (!connector) {
            throw new Error(`Cannot find connector to reach ${remoteKernelUri}`);
        }
        let kernel = new proxyKernel_1.ProxyKernel(localName, connector.sender, connector.receiver);
        kernel.kernelInfo.remoteUri = remoteKernelUri;
        this._kernel.add(kernel, aliases);
        return kernel;
    }
    connectProxyKernelOnConnector(localName, sender, receiver, remoteKernelUri, aliases) {
        let kernel = new proxyKernel_1.ProxyKernel(localName, sender, receiver);
        kernel.kernelInfo.remoteUri = remoteKernelUri;
        this._kernel.add(kernel, aliases);
        return kernel;
    }
    tryGetConnector(remoteUri) {
        return this._connectors.find(c => c.canReach(remoteUri));
    }
    connect() {
        this._kernel.subscribeToKernelEvents(e => {
            logger_1.Logger.default.info(`KernelHost forwarding event: ${JSON.stringify(e)}`);
            this._defaultConnector.sender.send(e);
        });
        this._defaultConnector.receiver.subscribe({
            next: (kernelCommandOrEventEnvelope) => {
                if (connection.isKernelCommandEnvelope(kernelCommandOrEventEnvelope)) {
                    logger_1.Logger.default.info(`KernelHost dispacthing command: ${JSON.stringify(kernelCommandOrEventEnvelope)}`);
                    this._scheduler.runAsync(kernelCommandOrEventEnvelope, commandEnvelope => {
                        const kernel = this._kernel;
                        return kernel.send(commandEnvelope);
                    });
                }
            }
        });
        this._defaultConnector.sender.send({ eventType: contracts.KernelReadyType, event: {}, routingSlip: [this._kernel.kernelInfo.uri] });
        this.publishKerneInfo();
    }
    publishKerneInfo() {
        const events = this.getKernelInfoProduced();
        for (const event of events) {
            this._defaultConnector.sender.send(event);
        }
    }
    getKernelInfoProduced() {
        let events = [];
        events.push({ eventType: contracts.KernelInfoProducedType, event: { kernelInfo: this._kernel.kernelInfo }, routingSlip: [this._kernel.kernelInfo.uri] });
        for (let kernel of this._kernel.childKernels) {
            events.push({ eventType: contracts.KernelInfoProducedType, event: { kernelInfo: kernel.kernelInfo }, routingSlip: [kernel.kernelInfo.uri] });
        }
        return events;
    }
}
exports.KernelHost = KernelHost;
//# sourceMappingURL=kernelHost.js.map
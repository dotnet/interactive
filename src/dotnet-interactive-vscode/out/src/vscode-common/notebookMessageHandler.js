"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.hashBangConnect = void 0;
const logger_1 = require("./dotnet-interactive/logger");
const dotnet_interactive_1 = require("./dotnet-interactive");
const rxjs = require("rxjs");
const connection = require("./dotnet-interactive/connection");
const contracts = require("./dotnet-interactive/contracts");
function hashBangConnect(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri) {
    logger_1.Logger.default.info(`handling #!connect for ${documentUri.toString()}`);
    hashBangConnectPrivate(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri);
    clientMapper.onClientCreate((clientUri, _client) => {
        if (clientUri.toString() === documentUri.toString()) {
            logger_1.Logger.default.info(`reconnecting webview kernels for ${documentUri.toString()}`);
            hashBangConnectPrivate(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri);
            return Promise.resolve();
        }
    });
}
exports.hashBangConnect = hashBangConnect;
function hashBangConnectPrivate(clientMapper, hostUri, kernelInfoProduced, messageHandlerMap, controllerPostMessage, documentUri) {
    logger_1.Logger.default.info(`handling #!connect from '${hostUri}' for not ebook: ${documentUri.toString()}`);
    const documentUriString = documentUri.toString();
    let messageHandler = messageHandlerMap.get(documentUriString);
    if (!messageHandler) {
        messageHandler = new rxjs.Subject();
        messageHandlerMap.set(documentUriString, messageHandler);
    }
    const extensionHostToWebviewSender = dotnet_interactive_1.KernelCommandAndEventSender.FromFunction(envelope => {
        controllerPostMessage({ envelope });
    });
    const WebviewToExtensionHostReceiver = dotnet_interactive_1.KernelCommandAndEventReceiver.FromObservable(messageHandler);
    clientMapper.getOrAddClient(documentUri).then(client => {
        logger_1.Logger.default.info(`configuring routing for host '${hostUri}'`);
        client.channel.receiver.subscribe({
            next: envelope => {
                if ((0, dotnet_interactive_1.isKernelEventEnvelope)(envelope)) {
                    logger_1.Logger.default.info(`forwarding event to '${hostUri}' ${JSON.stringify(envelope)}`);
                    extensionHostToWebviewSender.send(envelope);
                }
            }
        });
        WebviewToExtensionHostReceiver.subscribe({
            next: envelope => {
                var _a, _b, _c;
                if ((0, dotnet_interactive_1.isKernelCommandEnvelope)(envelope)) {
                    // handle command routing
                    if (envelope.command.destinationUri) {
                        if (envelope.command.destinationUri.startsWith("kernel://vscode")) {
                            // wants to go to vscode
                            logger_1.Logger.default.info(`routing command from '${hostUri}' ${JSON.stringify(envelope)} to extension host`);
                            const kernel = client.kernelHost.getKernel(envelope);
                            kernel.send(envelope);
                        }
                        else {
                            const host = (0, dotnet_interactive_1.extractHostAndNomalize)(envelope.command.destinationUri);
                            const connector = client.kernelHost.tryGetConnector(host);
                            if (connector) {
                                // route to interactive
                                logger_1.Logger.default.info(`routing command from '${hostUri}' ${JSON.stringify(envelope)} to '${host}'`);
                                connector.sender.send(envelope);
                            }
                            else {
                                logger_1.Logger.default.error(`cannot find connector to reach${envelope.command.destinationUri}`);
                            }
                        }
                    }
                    else {
                        const kernel = client.kernelHost.getKernel(envelope);
                        kernel.send(envelope);
                    }
                }
                if ((0, dotnet_interactive_1.isKernelEventEnvelope)(envelope)) {
                    if (envelope.eventType === contracts.KernelInfoProducedType) {
                        const kernelInfoProduced = envelope.event;
                        if (!connection.isKernelInfoForProxy(kernelInfoProduced.kernelInfo)) {
                            connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, client.kernel);
                        }
                    }
                    if ((_a = envelope.command) === null || _a === void 0 ? void 0 : _a.command.originUri) {
                        const host = (0, dotnet_interactive_1.extractHostAndNomalize)((_b = envelope.command) === null || _b === void 0 ? void 0 : _b.command.originUri);
                        const connector = client.kernelHost.tryGetConnector(host);
                        if (connector) {
                            // route to interactive
                            logger_1.Logger.default.info(`routing command from webview ${JSON.stringify(envelope)} to host ${host}`);
                            connector.sender.send(envelope);
                        }
                        else {
                            logger_1.Logger.default.error(`cannot find connector to reach ${(_c = envelope.command) === null || _c === void 0 ? void 0 : _c.command.originUri}`);
                        }
                    }
                }
            }
        });
        const knownKernels = client.kernelHost.getKernelInfoProduced();
        for (const knwonKernel of knownKernels) {
            const kernelInfoProduced = knwonKernel.event;
            logger_1.Logger.default.info(`forwarding kernelInfo [${JSON.stringify(kernelInfoProduced.kernelInfo)}] to webview`);
            extensionHostToWebviewSender.send(knwonKernel);
        }
        client.kernelHost.tryAddConnector({
            sender: extensionHostToWebviewSender,
            receiver: WebviewToExtensionHostReceiver,
            remoteUris: ["kernel://webview"]
        });
        for (const kernelInfo of kernelInfoProduced) {
            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, client.kernel);
        }
    });
}
//# sourceMappingURL=notebookMessageHandler.js.map
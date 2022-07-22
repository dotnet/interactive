// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { Logger } from './dotnet-interactive/logger';
import { isKernelCommandEnvelope, isKernelEventEnvelope, KernelCommandAndEventReceiver, KernelCommandAndEventSender, KernelCommandOrEventEnvelope } from './dotnet-interactive';
import * as rxjs from 'rxjs';



export function hashBangConnect(clientMapper: ClientMapper, messageHandlerMap: Map<string, rxjs.Subject<KernelCommandOrEventEnvelope>>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {
    Logger.default.info(`handling #!connect for ${documentUri.toString()}`);
    hashBangConnectPrivate(clientMapper, messageHandlerMap, controllerPostMessage, documentUri);
    clientMapper.onClientCreate((clientUri, _client) => {
        if (clientUri.toString() === documentUri.toString()) {
            Logger.default.info(`reconnecting webview kernels for ${documentUri.toString()}`);
            hashBangConnectPrivate(clientMapper, messageHandlerMap, controllerPostMessage, documentUri);
            return Promise.resolve();
        }
    });
}

function hashBangConnectPrivate(clientMapper: ClientMapper, messageHandlerMap: Map<string, rxjs.Subject<KernelCommandOrEventEnvelope>>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {
    const documentUriString = documentUri.toString();
    let messageHandler = messageHandlerMap.get(documentUriString);
    if (!messageHandler) {
        messageHandler = new rxjs.Subject<KernelCommandOrEventEnvelope>();
        messageHandlerMap.set(documentUriString, messageHandler);
    }

    const extensionHostToWebviewSender = KernelCommandAndEventSender.FromFunction(envelope => {
        controllerPostMessage({ envelope });
    });

    const WebviewToExtensionHostReceiver = KernelCommandAndEventReceiver.FromObservable(messageHandler);


    clientMapper.getOrAddClient(documentUri).then(client => {
        client.kernelHost.connectProxyKernelOntConnector('javascript', extensionHostToWebviewSender, WebviewToExtensionHostReceiver, "kernel://webview/javascript", ['js']);

        WebviewToExtensionHostReceiver.subscribe({
            next: envelope => {
                if (isKernelCommandEnvelope(envelope)) {
                    // handle command routing
                    if (envelope.command.destinationUri) {
                        if (envelope.command.destinationUri.startsWith("kernel://vscode")) {
                            // wants to go to vscode
                            Logger.default.info(`routing command from webview ${JSON.stringify(envelope)} to extension host`);
                            const kernel = client.kernelHost.getKernel(envelope);
                            kernel.send(envelope);

                        } else if (envelope.command.destinationUri.startsWith("kernel://pid")) {
                            // route to interactive
                            Logger.default.info(`routing command from webview ${JSON.stringify(envelope)} to interactive`);
                            client.channel.sender.send(envelope);
                        }
                    }

                    else {
                        const kernel = client.kernelHost.getKernel(envelope);
                        kernel.send(envelope);
                    }
                }

                if (isKernelEventEnvelope(envelope)) {
                    if (envelope.command?.command.originUri) {
                        // route to interactive
                        if (envelope.command?.command.originUri.startsWith("kernel://pid")) {
                            Logger.default.info(`routing event from webview ${JSON.stringify(envelope)} to interactive`);
                            client.channel.sender.send(envelope);
                        }
                    }
                }
            }
        });

        client.channel.receiver.subscribe({
            next: envelope => {
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`forwarding event to webview ${JSON.stringify(envelope)}`);
                    extensionHostToWebviewSender.send(envelope);
                }
            }
        });
    });
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { ProxyKernel } from './dotnet-interactive/proxyKernel';
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

    const documentToWebviewSender = KernelCommandAndEventSender.FromFunction(envelope => {
        controllerPostMessage({ envelope });
    });

    const WebviewToDocumentReceiver = KernelCommandAndEventReceiver.FromObservable(messageHandler);


    clientMapper.getOrAddClient(documentUri).then(client => {
        client.kernelHost.connectProxyKernelOntConnector('javascript', documentToWebviewSender, WebviewToDocumentReceiver, "kernel://webview/javascript", ['js']);

        WebviewToDocumentReceiver.subscribe({
            next: envelope => {
                if (isKernelCommandEnvelope(envelope)) {
                    const kernel = client.kernelHost.getKernel(envelope);
                    kernel.send(envelope);
                }
            }
        });

        client.channel.receiver.subscribe({
            next: envelope => {
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`forwarding event to webview ${JSON.stringify(envelope)}`);
                    documentToWebviewSender.send(envelope);
                }
            }
        });
    });
}

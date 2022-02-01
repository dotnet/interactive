// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from './dotnet-interactive/contracts';
import * as genericTransport from './dotnet-interactive/genericTransport';
import * as vscodeLike from './interfaces/vscode-like';
import { ClientMapper } from './clientMapper';
import { ProxyKernel } from './dotnet-interactive/proxyKernel';
import { Logger } from './dotnet-interactive/logger';

export type MessageHandler = {
    waitingOnMessages: genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> | null;
    envelopeQueue: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[];
};

export function hashBangConnect(clientMapper: ClientMapper, messageHandlerMap: Map<string, MessageHandler>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {
    Logger.default.info(`handling #!connect for ${documentUri.toString()}`);
    hashBangConnectPrivate(clientMapper, messageHandlerMap, controllerPostMessage, documentUri);
    clientMapper.onClientCreate(documentUri, (_client) => {
        Logger.default.info(`reconnecting webview kernels for ${documentUri.toString()}`);
        hashBangConnectPrivate(clientMapper, messageHandlerMap, controllerPostMessage, documentUri);
        return Promise.resolve();
    });
}

function hashBangConnectPrivate(clientMapper: ClientMapper, messageHandlerMap: Map<string, MessageHandler>, controllerPostMessage: (_: any) => void, documentUri: vscodeLike.Uri) {
    const documentUriString = documentUri.toString();
    let messageHandler = messageHandlerMap.get(documentUriString);
    if (!messageHandler) {
        messageHandler = {
            waitingOnMessages: null,
            envelopeQueue: [],
        };
        messageHandlerMap.set(documentUriString, messageHandler);
    }

    const documentWebViewTrasport = new genericTransport.GenericTransport(envelope => {
        controllerPostMessage({ envelope });
        return Promise.resolve();
    }, () => {
        let envelope = messageHandler!.envelopeQueue.shift();
        if (envelope) {
            return Promise.resolve<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(envelope);
        }
        else {
            messageHandler!.waitingOnMessages = new genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>();
            return messageHandler!.waitingOnMessages.promise;
        }
    });

    clientMapper.getOrAddClient(documentUri).then(client => {
        const proxyJsKernel = new ProxyKernel('javascript', documentWebViewTrasport);
        client.kernel.add(proxyJsKernel, ['js']);

        client.kernelHost.registerDestinationUriForProxy(proxyJsKernel.name, "kernel://webview/javascript");

        documentWebViewTrasport.setCommandHandler(envelope => {
            const kernel = client.kernelHost.getKernel(envelope);
            kernel.send(envelope);
            return Promise.resolve();
        });

        client.transport.subscribeToKernelEvents(eventEnvelope => {
            Logger.default.info(`forwarding event to webview ${eventEnvelope.toString()}`);
            return documentWebViewTrasport.publishKernelEvent(eventEnvelope);
        });

        documentWebViewTrasport.run();
    });
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./common/interactive/compositeKernel";
import { ProxyKernel } from "./common/interactive/proxyKernel";
import * as genericTransport from "./common/interactive/genericTransport";
import { JavascriptKernel } from "./common/interactive/javascriptKernel";
import { Kernel } from "./common/interactive/kernel";
import * as contracts from "./common/interfaces/contracts";

export function configure(global?: any) {
    if (!global) {
        global = window;
    }

    global.interactive = {};

    global.kernel = {
        get root() {
            return Kernel.root;
        }
    };

    const jsKernel = new JavascriptKernel();
    const compositeKernel = new CompositeKernel("webview");

    compositeKernel.add(jsKernel, ["js"]);

    let waitingOnMessages: genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> | null;
    let envelopeQueue: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];

    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        if (event.envelope) {
            const envelope = <contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope><any>(event.envelope);
            if (waitingOnMessages) {
                let capturedMessageWaiter = waitingOnMessages;
                waitingOnMessages = null;
                capturedMessageWaiter.resolve(envelope);
            } else {
                envelopeQueue.push(envelope);
            }
        }
    });

    const transport = new genericTransport.GenericTransport(
        (envelope) => {
            // @ts-ignore
            postKernelMessage({ envelope });
            return Promise.resolve();
        },
        () => {
            let envelope = envelopeQueue.shift();
            if (envelope) {
                return Promise.resolve<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(envelope);
            }
            else {
                waitingOnMessages = new genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>();
                return waitingOnMessages.promise;
            }
        }
    );

    const reverseProxy = new ProxyKernel('reverse-to-extension-host', transport);
    compositeKernel.add(reverseProxy, ['csharp', 'fsharp', 'pwsh']);

    transport.setCommandHandler(commandEnvelope => {
        return compositeKernel.send(commandEnvelope);
    });
    compositeKernel.subscribeToKernelEvents((eventEnvelope) => transport.publishKernelEvent(eventEnvelope));

    transport.run();

}

// TODO: sent create proxy message
// @ts-ignore
postKernelMessage({ preloadCommand: 'dostuff' });

configure(window);

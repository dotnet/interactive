// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./common/interactive/compositeKernel";
import { ProxyKernel } from "./common/interactive/proxyKernel";
import * as genericTransport from "./common/interactive/genericTransport";
import { JavascriptKernel } from "./common/interactive/javascriptKernel";
import { Kernel } from "./common/interactive/kernel";
import * as contracts from "./common/interfaces/contracts";
import { isKernelEventEnvelope } from "./common/interfaces/utilities";
import { KernelCommandScheduler } from "./common/interactive/kernelCommandScheduler";
import { Logger } from "./common/logger";

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

    Logger.configure('webview', entry => {
        // @ts-ignore
        postKernelMessage({ logEntry: entry });
    });

    const jsKernel = new JavascriptKernel();
    const compositeKernel = new CompositeKernel("webview");

    compositeKernel.add(jsKernel, ["js"]);

    let waitingOnMessages: genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> | null;
    let envelopeQueue: (contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope)[] = [];

    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        if (event.envelope) {
            const envelope = <contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope><any>(event.envelope);
            if (isKernelEventEnvelope(envelope)) {
                Logger.default.info(`transport got ${envelope.eventType} with token ${envelope.command?.token} and id ${envelope.command?.command.id}`);
            }
            if (waitingOnMessages) {
                let capturedMessageWaiter = waitingOnMessages;
                waitingOnMessages = null;
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`transport using awaiter`);
                }
                capturedMessageWaiter.resolve(envelope);
            } else {
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`transport adding to queue`);
                }
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
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`transport extracting from queue`);
                }
                return Promise.resolve<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(envelope);
            }
            else {
                Logger.default.info(`transport building promise awaiter`);
                waitingOnMessages = new genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>();
                return waitingOnMessages.promise;
            }
        }
    );

    const reverseProxy = new ProxyKernel('reverse-to-extension-host', transport);
    compositeKernel.add(reverseProxy, ['csharp', 'fsharp', 'pwsh']);

    const scheduler = new KernelCommandScheduler(commandEnvelope => {
        return compositeKernel.send(commandEnvelope);
    });
    transport.setCommandHandler(commandEnvelope => {
        // fire and forget this one
        scheduler.schedule(commandEnvelope);
        return Promise.resolve();
    });

    compositeKernel.subscribeToKernelEvents((eventEnvelope) => {
        Logger.default.info(`composite kernel sending forwarding event ${eventEnvelope.eventType} with token ${eventEnvelope.command?.token} from ${eventEnvelope.command?.command.targetKernelName} to vscode extension host`);
        return transport.publishKernelEvent(eventEnvelope);
    });

    transport.run();

}

// @ts-ignore
postKernelMessage({ preloadCommand: '#!connect' });

configure(window);

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

    global.devconsole = console;

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
                // @ts-ignore
                devconsole.log(`webview transport got ${envelope.eventType} with token ${envelope.command?.token}`);
            }
            if (waitingOnMessages) {
                let capturedMessageWaiter = waitingOnMessages;
                waitingOnMessages = null;
                if (isKernelEventEnvelope(envelope)) {
                    // @ts-ignore
                    devconsole.log(`webview transport using awaiter`);
                }
                capturedMessageWaiter.resolve(envelope);
            } else {
                if (isKernelEventEnvelope(envelope)) {
                    // @ts-ignore
                    devconsole.log(`webview transport adding to queue`);
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
                    // @ts-ignore
                    devconsole.log(`webview transport extracting from queue`);
                }
                return Promise.resolve<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(envelope);
            }
            else {
                // @ts-ignore
                devconsole.log(`webview transport building promise awaiter`);
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
        // @ts-ignore
        devconsole.log(`webview composite kernel sending forwarding event ${eventEnvelope.eventType} with token ${eventEnvelope.command?.token} from ${eventEnvelope.command?.command.targetKernelName} to vscode extension host`);
        return transport.publishKernelEvent(eventEnvelope);
    });

    transport.run();

}

// TODO: sent create proxy message
// @ts-ignore
postKernelMessage({ preloadCommand: 'dostuff' });

configure(window);

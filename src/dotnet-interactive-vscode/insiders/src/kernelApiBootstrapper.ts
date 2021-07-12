// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./common/interactive/compositeKernel";
import * as genericTransport from "./common/interactive/genericTransport";
import { JavascriptKernel } from "./common/interactive/javascriptKernel";
import * as contracts from "./common/interfaces/contracts";

export function configure(global?: any) {
    if (!global) {
        global = window;
    }

    global.interactive = {};

    const jsKernel = new JavascriptKernel();
    const compositeKernel = new CompositeKernel("webview");

    compositeKernel.add(jsKernel, ["js"]);

    let completionSources: (genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope> | Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>)[] = [];

    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        if (event.envelope) {
            const envelope = <contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope><any>(event.envelope);
            let completionSource = completionSources[0];
            if (genericTransport.isPromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(completionSource)) {
                completionSources.shift();
                completionSource.resolve(envelope);
            } else {
                completionSources.push(Promise.resolve(envelope));
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
            if (completionSources.length == 0) {
                completionSources.push(new genericTransport.PromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>());
            }
            if (genericTransport.isPromiseCompletionSource<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>(completionSources[0])) {
                return completionSources[0].promise;
            }
            else {
                return <Promise<contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope>>completionSources.shift();
            }
        }
    );

    transport.setCommandHandler(commandEnvelope => compositeKernel.send(commandEnvelope));
    compositeKernel.subscribeToKernelEvents((eventEnvelope) => transport.publishKernelEvent(eventEnvelope));

    transport.run();

}

configure(window);
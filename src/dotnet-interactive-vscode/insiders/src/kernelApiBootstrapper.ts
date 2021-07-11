// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./common/interactive/compositeKernel";
import { GenericTransport } from "./common/interactive/genericTransport";
import { JavascriptKernel } from "./common/interactive/javascriptKernel";

export function configure(global?: any) {
    if (!global) {
        global = window;
    }

    global.interactive = {};

    const jsKernel = new JavascriptKernel();
    const compositeKernel = new CompositeKernel("webview");

    compositeKernel.add(jsKernel, ["js"]);

    const transport = new GenericTransport(
        (envelope) => {
            // @ts-ignore
            postKernelMessage({ envelope });
            return Promise.resolve();
        },
        () => {
            throw new Error("Not Implemented");
        }
    );

    transport.setCommandHandler(commandEnvelope => compositeKernel.send(commandEnvelope));
    compositeKernel.subscribeToKernelEvents((eventEnvelope) => transport.publishKernelEvent(eventEnvelope));

    transport.run();

}

configure(window);
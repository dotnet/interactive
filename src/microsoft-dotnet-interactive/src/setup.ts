// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./compositeKernel";
import { JavascriptKernel } from "./javascriptKernel";
import * as contracts from "./contracts";
import { HtmlKernel } from "./htmlKernel";

export function setup(global?: any) {

    global = global || window;
    let compositeKernel = new CompositeKernel("browser");

    const jsKernel = new JavascriptKernel();
    const htmlKernel = new HtmlKernel();

    compositeKernel.add(jsKernel, ["js"]);
    compositeKernel.add(htmlKernel);

    compositeKernel.subscribeToKernelEvents(envelope => {
        global?.publishCommandOrEvent(envelope);
    });

    if (global) {
        global.sendKernelCommand = (kernelCommandEnvelope: contracts.KernelCommandEnvelope) => {
            compositeKernel.send(kernelCommandEnvelope);
        };
    }
}
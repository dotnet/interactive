// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./common/interactive/compositeKernel";
import * as genericTransport from "./common/interactive/genericTransport";
import { JavascriptKernel } from "./common/interactive/javascriptKernel";
import { Kernel } from "./common/interactive/kernel";
import * as contracts from "./common/interfaces/contracts";
import { isKernelEventEnvelope } from "./common/interfaces/utilities";
import { Logger } from "./common/logger";
import { KernelHost } from "./common/interactive/kernelHost";

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

    const receiver = new genericTransport.CommandAndEventReceiver();

    Logger.configure('webview', entry => {
        // @ts-ignore
        postKernelMessage({ logEntry: entry });
    });


    const transport = new genericTransport.GenericTransport(
        (envelope) => {
            // @ts-ignore
            postKernelMessage({ envelope });
            return Promise.resolve();
        },
        () => {
            return receiver.read();
        }
    );

    const compositeKernel = new CompositeKernel("webview");
    const kernelHost = new KernelHost(compositeKernel, transport, "kernel://webview");

    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        if (event.envelope) {
            const envelope = <contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope><any>(event.envelope);
            if (isKernelEventEnvelope(envelope)) {
                Logger.default.info(`transport got ${envelope.eventType} with token ${envelope.command?.token} and id ${envelope.command?.id}`);
            }
            receiver.delegate(envelope);
        }
    });

    const jsKernel = new JavascriptKernel();
    compositeKernel.add(jsKernel, ["js"]);

    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'csharp', aliases: ['c#', 'C#'] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'fsharp', aliases: ['fs', 'F#'] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'pwsh', aliases: ['powershell'] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'vscode', destinationUri: "kernel://vscode/vscode" });

    kernelHost.connect();
    transport.run();
}

// @ts-ignore
postKernelMessage({ preloadCommand: '#!connect' });

configure(window);

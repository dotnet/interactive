// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "./vscode-common/dotnet-interactive/compositeKernel";
import * as genericChannel from "./vscode-common/dotnet-interactive/genericChannel";
import { JavascriptKernel } from "./vscode-common/dotnet-interactive/javascriptKernel";
import { Kernel } from "./vscode-common/dotnet-interactive/kernel";
import * as contracts from "./vscode-common/dotnet-interactive/contracts";
import { isKernelEventEnvelope } from "./vscode-common/dotnet-interactive/utilities";
import { Logger } from "./vscode-common/dotnet-interactive/logger";
import { KernelHost } from "./vscode-common/dotnet-interactive/kernelHost";

export function configure(global?: any) {
    if (!global) {
        global = window;
    }

    global.interactive = {};
    if ((typeof (require) !== typeof (Function)) || (typeof ((<any>require).config) !== typeof (Function))) {
        let require_script = document.createElement('script');
        require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
        require_script.setAttribute('type', 'text/javascript');
        require_script.onload = function () {
            global.interactive.configureRequire = (confing: any) => {
                return (<any>require).config(confing) || require;
            };

        };
        document.getElementsByTagName('head')[0].appendChild(require_script);

    } else {
        global.interactive.configureRequire = (confing: any) => {
            return (<any>require).config(confing) || require;
        };
    }


    global.kernel = {
        get root() {
            return Kernel.root;
        }
    };

    const receiver = new genericChannel.CommandAndEventReceiver();

    Logger.configure('webview', entry => {
        // @ts-ignore
        postKernelMessage({ logEntry: entry });
    });


    const channel = new genericChannel.GenericChannel(
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
    const kernelHost = new KernelHost(compositeKernel, channel, "kernel://webview");

    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        if (event.envelope) {
            const envelope = <contracts.KernelCommandEnvelope | contracts.KernelEventEnvelope><any>(event.envelope);
            if (isKernelEventEnvelope(envelope)) {
                Logger.default.info(`channel got ${envelope.eventType} with token ${envelope.command?.token} and id ${envelope.command?.id}`);
            }
            receiver.delegate(envelope);
        }
    });

    const jsKernel = new JavascriptKernel();
    compositeKernel.add(jsKernel, ["js"]);

    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'csharp', aliases: ['c#', 'C#'], supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'fsharp', aliases: ['fs', 'F#'], supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'pwsh', aliases: ['powershell'], supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'vscode', aliases: [], destinationUri: "kernel://vscode/vscode", supportedDirectives: [], supportedKernelCommands: [] });

    kernelHost.connect();
    channel.run();
}

// @ts-ignore
postKernelMessage({ preloadCommand: '#!connect' });

configure(window);

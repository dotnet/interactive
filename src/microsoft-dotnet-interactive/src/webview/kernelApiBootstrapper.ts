// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "../compositeKernel";
import { JavascriptKernel } from "../javascriptKernel";
import { Kernel } from "../kernel";
import { Logger } from "../logger";
import { KernelHost } from "../kernelHost";
import * as rxjs from "rxjs";
import * as connection from "../connection";

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

    Logger.configure('webview', entry => {
        // @ts-ignore
        postKernelMessage({ logEntry: entry });
    });


    const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
    const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

    localToRemote.subscribe({
        next: envelope => {
            // @ts-ignore
            postKernelMessage({ envelope });
        }
    });
    const compositeKernel = new CompositeKernel("webview");
    const kernelHost = new KernelHost(compositeKernel, connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal), "kernel://webview");

    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        if (event.envelope) {
            const envelope = <connection.KernelCommandOrEventEnvelope><any>(event.envelope);
            if (connection.isKernelEventEnvelope(envelope)) {
                Logger.default.info(`channel got ${envelope.eventType} with token ${envelope.command?.token} and id ${envelope.command?.id}`);
            }
            remoteToLocal.next(envelope);
        }
    });

    const jsKernel = new JavascriptKernel();
    compositeKernel.add(jsKernel, ["js"]);

    kernelHost.connectProxyKernelOnDefaultConnector('csharp', undefined, ['c#', 'C#']);
    kernelHost.connectProxyKernelOnDefaultConnector('fsharp', undefined, ['fs', 'F#']);
    kernelHost.connectProxyKernelOnDefaultConnector('pwsh', undefined, ['powershell']);
    kernelHost.connectProxyKernelOnDefaultConnector('mermaid', undefined, []);
    kernelHost.connectProxyKernelOnDefaultConnector('vscode', "kernel://vscode/vscode");

    kernelHost.connect();
}

configure(window);

// @ts-ignore
postKernelMessage({ preloadCommand: '#!connect' });

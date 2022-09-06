// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as frontEndHost from './frontEndHost';
import * as rxjs from "rxjs";
import * as connection from "../connection";
import { Logger } from "../logger";
import { KernelHost } from '../kernelHost';

export function configure(global?: any) {
    if (!global) {
        global = window;
    }

    const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
    const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

    localToRemote.subscribe({
        next: envelope => {
            // @ts-ignore
            postKernelMessage({ envelope });
        }
    });

    // @ts-ignore
    onDidReceiveKernelMessage((arg: any) => {
        if (arg.envelope) {
            const envelope = <connection.KernelCommandOrEventEnvelope><any>(arg.envelope);
            if (connection.isKernelEventEnvelope(envelope)) {
                Logger.default.info(`channel got ${envelope.eventType} with token ${envelope.command?.token} and id ${envelope.command?.id}`);
            }

            remoteToLocal.next(envelope);
        }
    });

    frontEndHost.createHost(
        global,
        'webview',
        configureRequire,
        entry => {
            // @ts-ignore
            postKernelMessage({ logEntry: entry });
        },
        localToRemote,
        remoteToLocal,
        () => {
            let kernelInfoProduced = (<KernelHost>(global['webview'].kernelHost)).getKernelInfoProduced();
            // @ts-ignore
            postKernelMessage({ preloadCommand: '#!connect', kernelInfoProduced, hostUri: (<KernelHost>(global['webview'].kernelHost)).hostUri });

        }
    );
}

function configureRequire(interactive: any) {
    if ((typeof (require) !== typeof (Function)) || (typeof ((<any>require).config) !== typeof (Function))) {
        let require_script = document.createElement('script');
        require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
        require_script.setAttribute('type', 'text/javascript');
        require_script.onload = function () {
            interactive.configureRequire = (confing: any) => {
                return (<any>require).config(confing) || require;
            };

        };
        document.getElementsByTagName('head')[0].appendChild(require_script);

    } else {
        interactive.configureRequire = (confing: any) => {
            return (<any>require).config(confing) || require;
        };
    }
}

configure(window);

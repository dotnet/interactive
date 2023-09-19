// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as frontEndHost from './frontEndHost';
import * as rxjs from "rxjs";
import * as connection from "../connection";
import { Logger } from "../logger";
import { KernelHost } from '../kernelHost';
import { v4 as uuid } from 'uuid';
import { KernelInfo } from '../contracts';
import { KernelCommandEnvelope, KernelEventEnvelope } from '../commandsAndEvents';

type KernelMessagingApi = {
    onDidReceiveKernelMessage: (arg: any) => any;
    postKernelMessage: (data: unknown) => void;
};

export function activate(context: KernelMessagingApi) {
    configure(window, context);
    Logger.default.info(`set up 'webview' host module complete`);
}

function configure(global: any, context: KernelMessagingApi) {
    if (!global) {
        global = window;
    }

    const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
    const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

    localToRemote.subscribe({
        next: envelope => {
            const envelopeJson = envelope.toJson();
            context.postKernelMessage({ envelope: envelopeJson });
        }
    });

    const webViewId = uuid();
    context.onDidReceiveKernelMessage((arg: any) => {
        if (arg.envelope && arg.webViewId === webViewId) {
            const envelope = <connection.KernelCommandOrEventEnvelopeModel><any>(arg.envelope);
            if (connection.isKernelEventEnvelopeModel(envelope)) {
                Logger.default.info(`channel got ${envelope.eventType} with token ${envelope.command?.token}`);
                const event = KernelEventEnvelope.fromJson(envelope);
                remoteToLocal.next(event);
            } else {
                const command = KernelCommandEnvelope.fromJson(envelope);
                remoteToLocal.next(command);
            }

        } else if (arg.webViewId === webViewId) {
            const kernelHost = (<KernelHost>(global['webview'].kernelHost));
            if (kernelHost) {
                switch (arg.preloadCommand) {
                    case '#!connect': {
                        Logger.default.info(`connecting to kernels from extension host`);
                        const kernelInfos = <KernelInfo[]>(arg.kernelInfos);
                        for (const kernelInfo of kernelInfos) {
                            const remoteUri = kernelInfo.isProxy ? kernelInfo.remoteUri! : kernelInfo.uri;
                            if (!kernelHost.tryGetConnector(remoteUri)) {
                                kernelHost.defaultConnector.addRemoteHostUri(remoteUri);
                            }
                            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, kernelHost.kernel);
                        }
                    }
                }
            }
        }
    });

    frontEndHost.createHost(
        global,
        'webview',
        configureRequire,
        entry => {
            context.postKernelMessage({ logEntry: entry });
        },
        localToRemote,
        remoteToLocal,
        () => {
            const kernelInfos = (<KernelHost>(global['webview'].kernelHost)).getKernelInfos();
            const hostUri = (<KernelHost>(global['webview'].kernelHost)).uri;
            context.postKernelMessage({ preloadCommand: '#!connect', kernelInfos, hostUri, webViewId });
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

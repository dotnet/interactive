// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./commandsAndEvents";
import { createHtmlKernelForBrowser, HtmlKernel, HtmlKernelInBrowserConfiguration } from "./htmlKernel";
import * as frontEndHost from './webview/frontEndHost';
import * as rxjs from "rxjs";
import * as connection from "./connection";
import { Kernel } from "./kernel";
import { LogEntry, LogLevel } from "./logger";

export type SetupConfiguration = {
    global?: any,
    hostName: string,
    htmlKernelConfiguration?: HtmlKernelInBrowserConfiguration,
    enableLogger?: boolean,
};

export function setup(configuration?: SetupConfiguration) {

    let logWriter = (_entry: LogEntry) => { };

    if (configuration?.enableLogger) {
        const log = console.log;
        const error = console.error;
        const warn = console.warn;
        logWriter = (entry: LogEntry) => {
            const messageLogLevel = LogLevel[entry.logLevel];
            const message = `[${messageLogLevel}] ${entry.source}: ${entry.message}`;
            switch (entry.logLevel) {
                case LogLevel.Error:
                    error(message);
                    break;
                case LogLevel.Warn:
                    warn(message);
                    break;
                default:
                    log(message);
                    break;
            }
        };
    }
    const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
    const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();

    const global = (configuration?.global || window);

    localToRemote.subscribe({
        next: envelope => {
            global?.publishCommandOrEvent(envelope);
        }
    });

    if (global) {
        global.sendKernelCommand = (kernelCommandEnvelope: contracts.KernelCommandEnvelope) => {
            remoteToLocal.next(kernelCommandEnvelope);
        };
    }

    const compositeKernelName = configuration?.hostName || 'browser';
    frontEndHost.createHost(
        global,
        compositeKernelName,
        configureRequire,
        logWriter,
        localToRemote,
        remoteToLocal,
        () => {
            let htmlKernel: Kernel;

            if (configuration?.htmlKernelConfiguration) {
                htmlKernel = createHtmlKernelForBrowser(configuration.htmlKernelConfiguration);
            } else {
                htmlKernel = new HtmlKernel();
            }

            global[compositeKernelName].compositeKernel.add(htmlKernel);
        }
    );

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
}
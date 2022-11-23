"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.setup = void 0;
const htmlKernel_1 = require("./htmlKernel");
const frontEndHost = require("./webview/frontEndHost");
const rxjs = require("rxjs");
const logger_1 = require("./logger");
function setup(configuration) {
    let logWriter = (_entry) => { };
    if (configuration === null || configuration === void 0 ? void 0 : configuration.enableLogger) {
        const log = console.log;
        const error = console.error;
        const warn = console.warn;
        logWriter = (entry) => {
            const messageLogLevel = logger_1.LogLevel[entry.logLevel];
            const message = `[${messageLogLevel}] ${entry.source}: ${entry.message}`;
            switch (entry.logLevel) {
                case logger_1.LogLevel.Error:
                    error(message);
                    break;
                case logger_1.LogLevel.Warn:
                    warn(message);
                    break;
                default:
                    log(message);
                    break;
            }
        };
    }
    const remoteToLocal = new rxjs.Subject();
    const localToRemote = new rxjs.Subject();
    const global = ((configuration === null || configuration === void 0 ? void 0 : configuration.global) || window);
    localToRemote.subscribe({
        next: envelope => {
            global === null || global === void 0 ? void 0 : global.publishCommandOrEvent(envelope);
        }
    });
    if (global) {
        global.sendKernelCommand = (kernelCommandEnvelope) => {
            remoteToLocal.next(kernelCommandEnvelope);
        };
    }
    const compositeKernelName = (configuration === null || configuration === void 0 ? void 0 : configuration.hostName) || 'browser';
    frontEndHost.createHost(global, compositeKernelName, configureRequire, logWriter, localToRemote, remoteToLocal, () => {
        let htmlKernel;
        if (configuration === null || configuration === void 0 ? void 0 : configuration.htmlKernelConfiguration) {
            htmlKernel = (0, htmlKernel_1.createHtmlKernelForBrowser)(configuration.htmlKernelConfiguration);
        }
        else {
            htmlKernel = new htmlKernel_1.HtmlKernel();
        }
        global[compositeKernelName].compositeKernel.add(htmlKernel);
    });
    function configureRequire(interactive) {
        if ((typeof (require) !== typeof (Function)) || (typeof (require.config) !== typeof (Function))) {
            let require_script = document.createElement('script');
            require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
            require_script.setAttribute('type', 'text/javascript');
            require_script.onload = function () {
                interactive.configureRequire = (confing) => {
                    return require.config(confing) || require;
                };
            };
            document.getElementsByTagName('head')[0].appendChild(require_script);
        }
        else {
            interactive.configureRequire = (confing) => {
                return require.config(confing) || require;
            };
        }
    }
}
exports.setup = setup;
//# sourceMappingURL=setup.js.map
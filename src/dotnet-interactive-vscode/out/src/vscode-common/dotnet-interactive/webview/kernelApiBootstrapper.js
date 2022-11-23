"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.configure = void 0;
const frontEndHost = require("./frontEndHost");
const rxjs = require("rxjs");
const connection = require("../connection");
const logger_1 = require("../logger");
function configure(global) {
    if (!global) {
        global = window;
    }
    const remoteToLocal = new rxjs.Subject();
    const localToRemote = new rxjs.Subject();
    localToRemote.subscribe({
        next: envelope => {
            // @ts-ignore
            postKernelMessage({ envelope });
        }
    });
    // @ts-ignore
    onDidReceiveKernelMessage((arg) => {
        var _a, _b;
        if (arg.envelope) {
            const envelope = (arg.envelope);
            if (connection.isKernelEventEnvelope(envelope)) {
                logger_1.Logger.default.info(`channel got ${envelope.eventType} with token ${(_a = envelope.command) === null || _a === void 0 ? void 0 : _a.token} and id ${(_b = envelope.command) === null || _b === void 0 ? void 0 : _b.id}`);
            }
            remoteToLocal.next(envelope);
        }
    });
    frontEndHost.createHost(global, 'webview', configureRequire, entry => {
        // @ts-ignore
        postKernelMessage({ logEntry: entry });
    }, localToRemote, remoteToLocal, () => {
        const kernelInfoProduced = (global['webview'].kernelHost).getKernelInfoProduced();
        const hostUri = (global['webview'].kernelHost).uri;
        // @ts-ignore
        postKernelMessage({ preloadCommand: '#!connect', kernelInfoProduced, hostUri });
    });
}
exports.configure = configure;
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
logger_1.Logger.default.info(`setting up 'webview' host`);
configure(window);
logger_1.Logger.default.info(`set up 'webview' host complete`);
//# sourceMappingURL=kernelApiBootstrapper.js.map
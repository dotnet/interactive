"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.configure = void 0;
const compositeKernel_1 = require("./vscode-common/dotnet-interactive/compositeKernel");
const genericChannel = require("./vscode-common/dotnet-interactive/genericChannel");
const javascriptKernel_1 = require("./vscode-common/dotnet-interactive/javascriptKernel");
const kernel_1 = require("./vscode-common/dotnet-interactive/kernel");
const utilities_1 = require("./vscode-common/dotnet-interactive/utilities");
const logger_1 = require("./vscode-common/dotnet-interactive/logger");
const kernelHost_1 = require("./vscode-common/dotnet-interactive/kernelHost");
function configure(global) {
    if (!global) {
        global = window;
    }
    global.interactive = {};
    if ((typeof (require) !== typeof (Function)) || (typeof (require.config) !== typeof (Function))) {
        let require_script = document.createElement('script');
        require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
        require_script.setAttribute('type', 'text/javascript');
        require_script.onload = function () {
            global.interactive.configureRequire = (confing) => {
                return require.config(confing) || require;
            };
        };
        document.getElementsByTagName('head')[0].appendChild(require_script);
    }
    else {
        global.interactive.configureRequire = (confing) => {
            return require.config(confing) || require;
        };
    }
    global.kernel = {
        get root() {
            return kernel_1.Kernel.root;
        }
    };
    const receiver = new genericChannel.CommandAndEventReceiver();
    logger_1.Logger.configure('webview', entry => {
        // @ts-ignore
        postKernelMessage({ logEntry: entry });
    });
    const channel = new genericChannel.GenericChannel((envelope) => {
        // @ts-ignore
        postKernelMessage({ envelope });
        return Promise.resolve();
    }, () => {
        return receiver.read();
    });
    const compositeKernel = new compositeKernel_1.CompositeKernel("webview");
    const kernelHost = new kernelHost_1.KernelHost(compositeKernel, channel, "kernel://webview");
    // @ts-ignore
    onDidReceiveKernelMessage(event => {
        var _a, _b;
        if (event.envelope) {
            const envelope = (event.envelope);
            if ((0, utilities_1.isKernelEventEnvelope)(envelope)) {
                logger_1.Logger.default.info(`channel got ${envelope.eventType} with token ${(_a = envelope.command) === null || _a === void 0 ? void 0 : _a.token} and id ${(_b = envelope.command) === null || _b === void 0 ? void 0 : _b.id}`);
            }
            receiver.delegate(envelope);
        }
    });
    const jsKernel = new javascriptKernel_1.JavascriptKernel();
    compositeKernel.add(jsKernel, ["js"]);
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'csharp', aliases: ['c#', 'C#'], supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'fsharp', aliases: ['fs', 'F#'], supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'pwsh', aliases: ['powershell'], supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.createProxyKernelOnDefaultConnector({ localName: 'vscode', aliases: [], remoteUri: "kernel://vscode/vscode", supportedDirectives: [], supportedKernelCommands: [] });
    kernelHost.connect();
    channel.run();
}
exports.configure = configure;
// @ts-ignore
postKernelMessage({ preloadCommand: '#!connect' });
configure(window);
//# sourceMappingURL=kernelApiBootstrapper.js.map
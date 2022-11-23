"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.createHost = void 0;
const compositeKernel_1 = require("../compositeKernel");
const javascriptKernel_1 = require("../javascriptKernel");
const logger_1 = require("../logger");
const kernelHost_1 = require("../kernelHost");
const connection = require("../connection");
const contracts = require("../contracts");
function createHost(global, compositeKernelName, configureRequire, logMessage, localToRemote, remoteToLocal, onReady) {
    logger_1.Logger.configure(compositeKernelName, logMessage);
    global.interactive = {};
    configureRequire(global.interactive);
    const compositeKernel = new compositeKernel_1.CompositeKernel(compositeKernelName);
    const kernelHost = new kernelHost_1.KernelHost(compositeKernel, connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal), `kernel://${compositeKernelName}`);
    kernelHost.defaultConnector.receiver.subscribe({
        next: (envelope) => {
            if (connection.isKernelEventEnvelope(envelope) && envelope.eventType === contracts.KernelInfoProducedType) {
                const kernelInfoProduced = envelope.event;
                connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, compositeKernel);
            }
        }
    });
    // use composite kernel as root
    global.kernel = {
        get root() {
            return compositeKernel;
        }
    };
    global[compositeKernelName] = {
        compositeKernel,
        kernelHost,
    };
    const jsKernel = new javascriptKernel_1.JavascriptKernel();
    compositeKernel.add(jsKernel, ["js"]);
    kernelHost.connect();
    onReady();
}
exports.createHost = createHost;
//# sourceMappingURL=frontEndHost.js.map
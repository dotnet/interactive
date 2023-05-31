// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompositeKernel } from "../compositeKernel";
import { JavascriptKernel } from "../javascriptKernel";
import { LogEntry, Logger } from "../logger";
import { KernelHost } from "../kernelHost";
import * as rxjs from "rxjs";
import * as connection from "../connection";
import * as contracts from "../commandsAndEvents";

export function createHost(
    global: any,
    compositeKernelName: string,
    configureRequire: (interactive: any) => void,
    logMessage: (entry: LogEntry) => void,
    localToRemote: rxjs.Observer<connection.KernelCommandOrEventEnvelope>,
    remoteToLocal: rxjs.Observable<connection.KernelCommandOrEventEnvelope>,
    onReady: () => void) {
    Logger.configure(compositeKernelName, logMessage);

    global.interactive = {};
    configureRequire(global.interactive);

    const compositeKernel = new CompositeKernel(compositeKernelName);
    const kernelHost = new KernelHost(compositeKernel, connection.KernelCommandAndEventSender.FromObserver(localToRemote), connection.KernelCommandAndEventReceiver.FromObservable(remoteToLocal), `kernel://${compositeKernelName}`);

    kernelHost.defaultConnector.receiver.subscribe({
        next: (envelope) => {
            if (connection.isKernelEventEnvelope(envelope) && envelope.eventType === contracts.KernelInfoProducedType) {
                const kernelInfoProduced = <contracts.KernelInfoProduced>envelope.event;
                connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced.kernelInfo, compositeKernel);
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

    const jsKernel = new JavascriptKernel();
    compositeKernel.add(jsKernel, ["js"]);

    kernelHost.connect();

    onReady();
}

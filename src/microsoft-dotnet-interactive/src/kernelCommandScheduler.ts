// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contracts from "./contracts";
import { PromiseCompletionSource } from "./genericTransport";

interface SchedulerOperation {
    commandEnvelope: contracts.KernelCommandEnvelope;
    promiseCompletionSource: PromiseCompletionSource<void>;
}

export class KernelCommandScheduler {
    private operationQueue: Array<SchedulerOperation> = [];

    constructor(private readonly executor: (commandEnvelope: contracts.KernelCommandEnvelope) => Promise<void>) {
    }

    schedule(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void> {
        const promiseCompletionSource = new PromiseCompletionSource<void>();
        const operation = {
            commandEnvelope,
            promiseCompletionSource,
        };
        this.operationQueue.push(operation);
        if (this.operationQueue.length === 1) {
            this.executeNextCommand();
        }

        return promiseCompletionSource.promise;
    }

    private executeNextCommand(): void {
        const nextOperation = this.operationQueue.shift();
        if (nextOperation) {
            this.executor(nextOperation.commandEnvelope)
                .then(() => {
                    nextOperation.promiseCompletionSource.resolve();
                    this.executeNextCommand();
                })
                .catch(e => {
                    nextOperation.promiseCompletionSource.reject(e);
                    this.executeNextCommand();
                });
        }
    }
}

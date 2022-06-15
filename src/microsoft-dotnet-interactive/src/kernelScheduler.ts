// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PromiseCompletionSource } from "./genericChannel";

interface SchedulerOperation<T> {
    value: T;
    executor: (value: T) => Promise<void>;
    promiseCompletionSource: PromiseCompletionSource<void>;
}

export class KernelScheduler<T> {
    private operationQueue: Array<SchedulerOperation<T>> = [];
    private inFlightOperation?: SchedulerOperation<T>;

    constructor() {
    }

    runAsync(value: T, executor: (value: T) => Promise<void>): Promise<void> {
        const operation = {
            value,
            executor,
            promiseCompletionSource: new PromiseCompletionSource<void>(),
        };

        if (this.inFlightOperation) {
            // invoke immediately
            return operation.executor(operation.value)
                .then(() => {
                    operation.promiseCompletionSource.resolve();
                })
                .catch(e => {
                    operation.promiseCompletionSource.reject(e);
                });
        }

        this.operationQueue.push(operation);
        if (this.operationQueue.length === 1) {
            this.executeNextCommand();
        }

        return operation.promiseCompletionSource.promise;
    }

    private executeNextCommand(): void {
        const nextOperation = this.operationQueue.length > 0 ? this.operationQueue[0] : undefined;
        if (nextOperation) {
            this.inFlightOperation = nextOperation;
            nextOperation.executor(nextOperation.value)
                .then(() => {
                    this.inFlightOperation = undefined;
                    nextOperation.promiseCompletionSource.resolve();
                })
                .catch(e => {
                    this.inFlightOperation = undefined;
                    nextOperation.promiseCompletionSource.reject(e);
                })
                .finally(() => {
                    this.operationQueue.shift();
                    this.executeNextCommand();
                });
        }
    }
}

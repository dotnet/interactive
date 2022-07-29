// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PromiseCompletionSource } from "./promiseCompletionSource";

interface SchedulerOperation<T> {
    value: T;
    executor: (value: T) => Promise<void>;
    promiseCompletionSource: PromiseCompletionSource<void>;
}
export class KernelScheduler<T> {
    private _operationQueue: Array<SchedulerOperation<T>> = [];
    private _inFlightOperation?: SchedulerOperation<T>;

    constructor() {
    }

    public cancelCurrentOperation(): void {
        this._inFlightOperation?.promiseCompletionSource.reject(new Error("Operation cancelled"));
    }

    runAsync(value: T, executor: (value: T) => Promise<void>): Promise<void> {
        const operation = {
            value,
            executor,
            promiseCompletionSource: new PromiseCompletionSource<void>(),
        };

        if (this._inFlightOperation) {
            // invoke immediately
            return operation.executor(operation.value)
                .then(() => {
                    operation.promiseCompletionSource.resolve();
                })
                .catch(e => {
                    operation.promiseCompletionSource.reject(e);
                });
        }

        this._operationQueue.push(operation);
        if (this._operationQueue.length === 1) {
            this.executeNextCommand();
        }

        return operation.promiseCompletionSource.promise;
    }

    private executeNextCommand(): void {
        const nextOperation = this._operationQueue.length > 0 ? this._operationQueue[0] : undefined;
        if (nextOperation) {
            this._inFlightOperation = nextOperation;
            nextOperation.executor(nextOperation.value)
                .then(() => {
                    this._inFlightOperation = undefined;
                    nextOperation.promiseCompletionSource.resolve();
                })
                .catch(e => {
                    this._inFlightOperation = undefined;
                    nextOperation.promiseCompletionSource.reject(e);
                })
                .finally(() => {
                    this._operationQueue.shift();
                    this.executeNextCommand();
                });
        }
    }
}

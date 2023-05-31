// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Logger } from "./logger";
import { PromiseCompletionSource } from "./promiseCompletionSource";

interface SchedulerOperation<T> {
    value: T;
    executor: (value: T) => Promise<void>;
    promiseCompletionSource: PromiseCompletionSource<void>;
}

export class KernelScheduler<T> {
    setMustTrampoline(predicate: (c: T) => boolean) {
        this._runPreemptively = predicate ?? ((_c) => false);
    }
    private _operationQueue: Array<SchedulerOperation<T>> = [];
    private _inFlightOperation?: SchedulerOperation<T>;
    private _runPreemptively: (c: T) => boolean;
    constructor() {
        this._runPreemptively = (_c) => true;
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

        const runPreemptively = this._runPreemptively(value);

        if (this._inFlightOperation && runPreemptively) {
            Logger.default.info(`kernelScheduler: starting immediate execution of ${JSON.stringify(operation.value)}`);

            // invoke immediately
            return operation.executor(operation.value)
                .then(() => {
                    Logger.default.info(`kernelScheduler: immediate execution completed: ${JSON.stringify(operation.value)}`);
                    operation.promiseCompletionSource.resolve();
                })
                .catch(e => {
                    Logger.default.info(`kernelScheduler: immediate execution failed: ${JSON.stringify(e)} - ${JSON.stringify(operation.value)}`);
                    operation.promiseCompletionSource.reject(e);
                });
        }

        Logger.default.info(`kernelScheduler: scheduling execution of ${JSON.stringify(operation.value)}`);
        this._operationQueue.push(operation);
        if (this._operationQueue.length === 1) {
            setTimeout(() => {
                this.executeNextCommand();
            }, 0);
        }

        return operation.promiseCompletionSource.promise;
    }

    private executeNextCommand(): void {
        const nextOperation = this._operationQueue.length > 0 ? this._operationQueue[0] : undefined;
        if (nextOperation) {
            this._inFlightOperation = nextOperation;
            Logger.default.info(`kernelScheduler: starting scheduled execution of ${JSON.stringify(nextOperation.value)}`);
            nextOperation.executor(nextOperation.value)
                .then(() => {
                    this._inFlightOperation = undefined;
                    Logger.default.info(`kernelScheduler: completing inflight operation: success ${JSON.stringify(nextOperation.value)}`);
                    nextOperation.promiseCompletionSource.resolve();
                })
                .catch(e => {
                    this._inFlightOperation = undefined;
                    Logger.default.info(`kernelScheduler: completing inflight operation: failure ${JSON.stringify(e)} - ${JSON.stringify(nextOperation.value)}`);
                    nextOperation.promiseCompletionSource.reject(e);
                })
                .finally(() => {
                    setTimeout(() => {
                        this._operationQueue.shift();
                        this.executeNextCommand();
                    }, 0);
                });
        }
    }
}

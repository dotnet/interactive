"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.KernelScheduler = void 0;
const logger_1 = require("./logger");
const promiseCompletionSource_1 = require("./promiseCompletionSource");
class KernelScheduler {
    constructor() {
        this._operationQueue = [];
    }
    cancelCurrentOperation() {
        var _a;
        (_a = this._inFlightOperation) === null || _a === void 0 ? void 0 : _a.promiseCompletionSource.reject(new Error("Operation cancelled"));
    }
    runAsync(value, executor) {
        const operation = {
            value,
            executor,
            promiseCompletionSource: new promiseCompletionSource_1.PromiseCompletionSource(),
        };
        if (this._inFlightOperation) {
            logger_1.Logger.default.info(`kernelScheduler: starting immediate execution of ${JSON.stringify(operation.value)}`);
            // invoke immediately
            return operation.executor(operation.value)
                .then(() => {
                logger_1.Logger.default.info(`kernelScheduler: immediate execution completed: ${JSON.stringify(operation.value)}`);
                operation.promiseCompletionSource.resolve();
            })
                .catch(e => {
                logger_1.Logger.default.info(`kernelScheduler: immediate execution failed: ${JSON.stringify(e)} - ${JSON.stringify(operation.value)}`);
                operation.promiseCompletionSource.reject(e);
            });
        }
        logger_1.Logger.default.info(`kernelScheduler: scheduling execution of ${JSON.stringify(operation.value)}`);
        this._operationQueue.push(operation);
        if (this._operationQueue.length === 1) {
            this.executeNextCommand();
        }
        return operation.promiseCompletionSource.promise;
    }
    executeNextCommand() {
        const nextOperation = this._operationQueue.length > 0 ? this._operationQueue[0] : undefined;
        if (nextOperation) {
            this._inFlightOperation = nextOperation;
            logger_1.Logger.default.info(`kernelScheduler: starting scheduled execution of ${JSON.stringify(nextOperation.value)}`);
            nextOperation.executor(nextOperation.value)
                .then(() => {
                this._inFlightOperation = undefined;
                logger_1.Logger.default.info(`kernelScheduler: completing inflight operation: success ${JSON.stringify(nextOperation.value)}`);
                nextOperation.promiseCompletionSource.resolve();
            })
                .catch(e => {
                this._inFlightOperation = undefined;
                logger_1.Logger.default.info(`kernelScheduler: completing inflight operation: failure ${JSON.stringify(e)} - ${JSON.stringify(nextOperation.value)}`);
                nextOperation.promiseCompletionSource.reject(e);
            })
                .finally(() => {
                this._operationQueue.shift();
                this.executeNextCommand();
            });
        }
    }
}
exports.KernelScheduler = KernelScheduler;
//# sourceMappingURL=kernelScheduler.js.map
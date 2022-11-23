"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.KernelCommandScheduler = void 0;
const genericChannel_1 = require("./genericChannel");
class KernelCommandScheduler {
    constructor(executor) {
        this.executor = executor;
        this.operationQueue = [];
    }
    schedule(commandEnvelope) {
        const promiseCompletionSource = new genericChannel_1.PromiseCompletionSource();
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
    executeNextCommand() {
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
exports.KernelCommandScheduler = KernelCommandScheduler;
//# sourceMappingURL=kernelCommandScheduler.js.map
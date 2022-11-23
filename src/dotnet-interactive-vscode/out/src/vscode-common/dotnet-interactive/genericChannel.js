"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.CommandAndEventReceiver = exports.GenericChannel = exports.PromiseCompletionSource = exports.isPromiseCompletionSource = void 0;
const utilities = require("./utilities");
const logger_1 = require("./logger");
function isPromiseCompletionSource(obj) {
    return obj.promise
        && obj.resolve
        && obj.reject;
}
exports.isPromiseCompletionSource = isPromiseCompletionSource;
class PromiseCompletionSource {
    constructor() {
        this._resolve = () => { };
        this._reject = () => { };
        this.promise = new Promise((resolve, reject) => {
            this._resolve = resolve;
            this._reject = reject;
        });
    }
    resolve(value) {
        this._resolve(value);
    }
    reject(reason) {
        this._reject(reason);
    }
}
exports.PromiseCompletionSource = PromiseCompletionSource;
class GenericChannel {
    constructor(messageSender, messageReceiver) {
        this.messageSender = messageSender;
        this.messageReceiver = messageReceiver;
        this.commandHandler = () => Promise.resolve();
        this.eventSubscribers = [];
        this.stillRunning = new PromiseCompletionSource();
    }
    dispose() {
        this.stop();
    }
    run() {
        return __awaiter(this, void 0, void 0, function* () {
            while (true) {
                let message = yield Promise.race([this.messageReceiver(), this.stillRunning.promise]);
                if (typeof message === 'number') {
                    return;
                }
                if (utilities.isKernelCommandEnvelope(message)) {
                    this.commandHandler(message);
                }
                else if (utilities.isKernelEventEnvelope(message)) {
                    for (let i = this.eventSubscribers.length - 1; i >= 0; i--) {
                        this.eventSubscribers[i](message);
                    }
                }
            }
        });
    }
    stop() {
        this.stillRunning.resolve(-1);
    }
    submitCommand(commandEnvelope) {
        return this.messageSender(commandEnvelope);
    }
    publishKernelEvent(eventEnvelope) {
        return this.messageSender(eventEnvelope);
    }
    subscribeToKernelEvents(observer) {
        this.eventSubscribers.push(observer);
        return {
            dispose: () => {
                const i = this.eventSubscribers.indexOf(observer);
                if (i >= 0) {
                    this.eventSubscribers.splice(i, 1);
                }
            }
        };
    }
    setCommandHandler(handler) {
        this.commandHandler = handler;
    }
}
exports.GenericChannel = GenericChannel;
class CommandAndEventReceiver {
    constructor() {
        this._waitingOnMessages = null;
        this._envelopeQueue = [];
    }
    delegate(commandOrEvent) {
        if (this._waitingOnMessages) {
            let capturedMessageWaiter = this._waitingOnMessages;
            this._waitingOnMessages = null;
            capturedMessageWaiter.resolve(commandOrEvent);
        }
        else {
            this._envelopeQueue.push(commandOrEvent);
        }
    }
    read() {
        let envelope = this._envelopeQueue.shift();
        if (envelope) {
            return Promise.resolve(envelope);
        }
        else {
            logger_1.Logger.default.info(`channel building promise awaiter`);
            this._waitingOnMessages = new PromiseCompletionSource();
            return this._waitingOnMessages.promise;
        }
    }
}
exports.CommandAndEventReceiver = CommandAndEventReceiver;
//# sourceMappingURL=genericChannel.js.map
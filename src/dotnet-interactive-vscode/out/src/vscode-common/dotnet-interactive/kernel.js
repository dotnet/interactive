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
exports.getKernelUri = exports.submitCommandAndGetResult = exports.Kernel = exports.KernelType = void 0;
const kernelInvocationContext_1 = require("./kernelInvocationContext");
const tokenGenerator_1 = require("./tokenGenerator");
const contracts = require("./contracts");
const logger_1 = require("./logger");
const kernelScheduler_1 = require("./kernelScheduler");
const promiseCompletionSource_1 = require("./promiseCompletionSource");
const routingslip = require("./routingslip");
const rxjs = require("rxjs");
var KernelType;
(function (KernelType) {
    KernelType[KernelType["composite"] = 0] = "composite";
    KernelType[KernelType["proxy"] = 1] = "proxy";
    KernelType[KernelType["default"] = 2] = "default";
})(KernelType = exports.KernelType || (exports.KernelType = {}));
;
class Kernel {
    constructor(name, languageName, languageVersion, displayName) {
        this.name = name;
        this._commandHandlers = new Map();
        this._eventSubject = new rxjs.Subject();
        this._tokenGenerator = new tokenGenerator_1.TokenGenerator();
        this.rootKernel = this;
        this.parentKernel = null;
        this._scheduler = null;
        this._kernelType = KernelType.default;
        this._kernelInfo = {
            localName: name,
            languageName: languageName,
            aliases: [],
            uri: routingslip.createKernelUri(`kernel://local/${name}`),
            languageVersion: languageVersion,
            displayName: displayName !== null && displayName !== void 0 ? displayName : name,
            supportedDirectives: [],
            supportedKernelCommands: []
        };
        this._internalRegisterCommandHandler({
            commandType: contracts.RequestKernelInfoType, handle: (invocation) => __awaiter(this, void 0, void 0, function* () {
                yield this.handleRequestKernelInfo(invocation);
            })
        });
    }
    get kernelInfo() {
        return this._kernelInfo;
    }
    get kernelType() {
        return this._kernelType;
    }
    set kernelType(value) {
        this._kernelType = value;
    }
    get kernelEvents() {
        return this._eventSubject.asObservable();
    }
    handleRequestKernelInfo(invocation) {
        return __awaiter(this, void 0, void 0, function* () {
            const eventEnvelope = {
                eventType: contracts.KernelInfoProducedType,
                command: invocation.commandEnvelope,
                event: { kernelInfo: this._kernelInfo }
            }; //?
            invocation.context.publish(eventEnvelope);
            return Promise.resolve();
        });
    }
    getScheduler() {
        var _a, _b;
        if (!this._scheduler) {
            this._scheduler = (_b = (_a = this.parentKernel) === null || _a === void 0 ? void 0 : _a.getScheduler()) !== null && _b !== void 0 ? _b : new kernelScheduler_1.KernelScheduler();
        }
        return this._scheduler;
    }
    ensureCommandTokenAndId(commandEnvelope) {
        var _a;
        if (!commandEnvelope.token) {
            let nextToken = this._tokenGenerator.GetNewToken();
            if ((_a = kernelInvocationContext_1.KernelInvocationContext.current) === null || _a === void 0 ? void 0 : _a.commandEnvelope) {
                // a parent command exists, create a token hierarchy
                nextToken = kernelInvocationContext_1.KernelInvocationContext.current.commandEnvelope.token;
            }
            commandEnvelope.token = nextToken;
        }
        if (!commandEnvelope.id) {
            commandEnvelope.id = tokenGenerator_1.Guid.create().toString();
        }
    }
    static get current() {
        if (kernelInvocationContext_1.KernelInvocationContext.current) {
            return kernelInvocationContext_1.KernelInvocationContext.current.handlingKernel;
        }
        return null;
    }
    static get root() {
        if (Kernel.current) {
            return Kernel.current.rootKernel;
        }
        return null;
    }
    // Is it worth us going to efforts to ensure that the Promise returned here accurately reflects
    // the command's progress? The only thing that actually calls this is the kernel channel, through
    // the callback set up by attachKernelToChannel, and the callback is expected to return void, so
    // nothing is ever going to look at the promise we return here.
    send(commandEnvelope) {
        return __awaiter(this, void 0, void 0, function* () {
            this.ensureCommandTokenAndId(commandEnvelope);
            const kernelUri = getKernelUri(this);
            if (!routingslip.commandRoutingSlipContains(commandEnvelope, kernelUri)) {
                routingslip.stampCommandRoutingSlipAsArrived(commandEnvelope, kernelUri);
            }
            else {
                "should not be here"; //?
            }
            commandEnvelope.routingSlip; //?
            kernelInvocationContext_1.KernelInvocationContext.establish(commandEnvelope);
            return this.getScheduler().runAsync(commandEnvelope, (value) => this.executeCommand(value).finally(() => {
                routingslip.stampCommandRoutingSlip(commandEnvelope, kernelUri);
            }));
        });
    }
    executeCommand(commandEnvelope) {
        var _a;
        return __awaiter(this, void 0, void 0, function* () {
            let context = kernelInvocationContext_1.KernelInvocationContext.establish(commandEnvelope);
            let previousHandlingKernel = context.handlingKernel;
            try {
                yield this.handleCommand(commandEnvelope);
            }
            catch (e) {
                context.fail(((_a = e) === null || _a === void 0 ? void 0 : _a.message) || JSON.stringify(e));
            }
            finally {
                context.handlingKernel = previousHandlingKernel;
            }
        });
    }
    getCommandHandler(commandType) {
        return this._commandHandlers.get(commandType);
    }
    handleCommand(commandEnvelope) {
        return new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
            var _a;
            let context = kernelInvocationContext_1.KernelInvocationContext.establish(commandEnvelope);
            const previoudHendlingKernel = context.handlingKernel;
            context.handlingKernel = this;
            let isRootCommand = (0, kernelInvocationContext_1.areCommandsTheSame)(context.commandEnvelope, commandEnvelope);
            let eventSubscription = undefined; //?
            if (isRootCommand) {
                this.name; //?
                logger_1.Logger.default.info(`kernel ${this.name} of type ${KernelType[this.kernelType]} subscribing to context events`);
                eventSubscription = context.kernelEvents.pipe(rxjs.map(e => {
                    var _a;
                    const message = `kernel ${this.name} of type ${KernelType[this.kernelType]} saw event ${e.eventType} with token ${(_a = e.command) === null || _a === void 0 ? void 0 : _a.token}`;
                    message; //?
                    logger_1.Logger.default.info(message);
                    const kernelUri = getKernelUri(this);
                    if (!routingslip.eventRoutingSlipContains(e, kernelUri)) {
                        routingslip.stampEventRoutingSlip(e, kernelUri);
                    }
                    else {
                        "should not get here"; //?
                    }
                    return e;
                }))
                    .subscribe(this.publishEvent.bind(this));
            }
            let handler = this.getCommandHandler(commandEnvelope.commandType);
            if (handler) {
                try {
                    logger_1.Logger.default.info(`kernel ${this.name} about to handle command: ${JSON.stringify(commandEnvelope)}`);
                    yield handler.handle({ commandEnvelope: commandEnvelope, context });
                    context.complete(commandEnvelope);
                    context.handlingKernel = previoudHendlingKernel;
                    if (isRootCommand) {
                        eventSubscription === null || eventSubscription === void 0 ? void 0 : eventSubscription.unsubscribe();
                        context.dispose();
                    }
                    logger_1.Logger.default.info(`kernel ${this.name} done handling command: ${JSON.stringify(commandEnvelope)}`);
                    resolve();
                }
                catch (e) {
                    context.fail(((_a = e) === null || _a === void 0 ? void 0 : _a.message) || JSON.stringify(e));
                    context.handlingKernel = previoudHendlingKernel;
                    if (isRootCommand) {
                        eventSubscription === null || eventSubscription === void 0 ? void 0 : eventSubscription.unsubscribe();
                        context.dispose();
                    }
                    reject(e);
                }
            }
            else {
                context.handlingKernel = previoudHendlingKernel;
                if (isRootCommand) {
                    eventSubscription === null || eventSubscription === void 0 ? void 0 : eventSubscription.unsubscribe();
                    context.dispose();
                }
                reject(new Error(`No handler found for command type ${commandEnvelope.commandType}`));
            }
        }));
    }
    subscribeToKernelEvents(observer) {
        const sub = this._eventSubject.subscribe(observer);
        return {
            dispose: () => { sub.unsubscribe(); }
        };
    }
    canHandle(commandEnvelope) {
        if (commandEnvelope.command.targetKernelName && commandEnvelope.command.targetKernelName !== this.name) {
            return false;
        }
        if (commandEnvelope.command.destinationUri) {
            const normalizedUri = routingslip.createKernelUri(commandEnvelope.command.destinationUri);
            if (this.kernelInfo.uri !== normalizedUri) {
                return false;
            }
        }
        return this.supportsCommand(commandEnvelope.commandType);
    }
    supportsCommand(commandType) {
        return this._commandHandlers.has(commandType);
    }
    registerCommandHandler(handler) {
        // When a registration already existed, we want to overwrite it because we want users to
        // be able to develop handlers iteratively, and it would be unhelpful for handler registration
        // for any particular command to be cumulative.
        const shouldNotify = !this._commandHandlers.has(handler.commandType);
        this._internalRegisterCommandHandler(handler);
        if (shouldNotify) {
            const event = {
                kernelInfo: this._kernelInfo,
            };
            const envelope = {
                eventType: contracts.KernelInfoProducedType,
                event: event
            };
            routingslip.stampEventRoutingSlip(envelope, getKernelUri(this));
            const context = kernelInvocationContext_1.KernelInvocationContext.current;
            if (context) {
                envelope.command = context.commandEnvelope;
                context.publish(envelope);
            }
            else {
                this.publishEvent(envelope);
            }
        }
    }
    _internalRegisterCommandHandler(handler) {
        this._commandHandlers.set(handler.commandType, handler);
        this._kernelInfo.supportedKernelCommands = Array.from(this._commandHandlers.keys()).map(commandName => ({ name: commandName }));
    }
    getHandlingKernel(commandEnvelope, context) {
        if (this.canHandle(commandEnvelope)) {
            return this;
        }
        else {
            context === null || context === void 0 ? void 0 : context.fail(`Command ${commandEnvelope.commandType} is not supported by Kernel ${this.name}`);
            return null;
        }
    }
    publishEvent(kernelEvent) {
        this._eventSubject.next(kernelEvent);
    }
}
exports.Kernel = Kernel;
function submitCommandAndGetResult(kernel, commandEnvelope, expectedEventType) {
    return __awaiter(this, void 0, void 0, function* () {
        let completionSource = new promiseCompletionSource_1.PromiseCompletionSource();
        let handled = false;
        let disposable = kernel.subscribeToKernelEvents(eventEnvelope => {
            var _a, _b;
            if (((_a = eventEnvelope.command) === null || _a === void 0 ? void 0 : _a.token) === commandEnvelope.token) {
                switch (eventEnvelope.eventType) {
                    case contracts.CommandFailedType:
                        if (!handled) {
                            handled = true;
                            let err = eventEnvelope.event; //?
                            completionSource.reject(err);
                        }
                        break;
                    case contracts.CommandSucceededType:
                        if ((0, kernelInvocationContext_1.areCommandsTheSame)(eventEnvelope.command, commandEnvelope)
                            && (((_b = eventEnvelope.command) === null || _b === void 0 ? void 0 : _b.id) === commandEnvelope.id)) {
                            if (!handled) { //? ($ ? eventEnvelope : {})
                                handled = true;
                                completionSource.reject('Command was handled before reporting expected result.');
                            }
                            break;
                        }
                    default:
                        if (eventEnvelope.eventType === expectedEventType) {
                            handled = true;
                            let event = eventEnvelope.event; //? ($ ? eventEnvelope : {})
                            completionSource.resolve(event);
                        }
                        break;
                }
            }
        });
        try {
            yield kernel.send(commandEnvelope);
        }
        finally {
            disposable.dispose();
        }
        return completionSource.promise;
    });
}
exports.submitCommandAndGetResult = submitCommandAndGetResult;
function getKernelUri(kernel) {
    var _a;
    return (_a = kernel.kernelInfo.uri) !== null && _a !== void 0 ? _a : `kernel://local/${kernel.kernelInfo.localName}`;
}
exports.getKernelUri = getKernelUri;
//# sourceMappingURL=kernel.js.map
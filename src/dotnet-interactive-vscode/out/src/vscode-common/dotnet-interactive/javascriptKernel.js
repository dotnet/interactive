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
exports.formatValue = exports.JavascriptKernel = void 0;
const contracts = require("./contracts");
const consoleCapture_1 = require("./consoleCapture");
const kernel_1 = require("./kernel");
const logger_1 = require("./logger");
class JavascriptKernel extends kernel_1.Kernel {
    constructor(name) {
        super(name !== null && name !== void 0 ? name : "javascript", "JavaScript");
        this.suppressedLocals = new Set(this.allLocalVariableNames());
        this.registerCommandHandler({ commandType: contracts.SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
        this.registerCommandHandler({ commandType: contracts.RequestValueInfosType, handle: invocation => this.handleRequestValueInfos(invocation) });
        this.registerCommandHandler({ commandType: contracts.RequestValueType, handle: invocation => this.handleRequestValue(invocation) });
        this.registerCommandHandler({ commandType: contracts.SendValueType, handle: invocation => this.handleSendValue(invocation) });
        this.capture = new consoleCapture_1.ConsoleCapture();
    }
    handleSendValue(invocation) {
        const sendValue = invocation.commandEnvelope.command;
        if (sendValue.formattedValue) {
            switch (sendValue.formattedValue.mimeType) {
                case 'application/json':
                    globalThis[sendValue.name] = JSON.parse(sendValue.formattedValue.value);
                    break;
                default:
                    throw new Error(`mimetype ${sendValue.formattedValue.mimeType} not supported`);
            }
            return Promise.resolve();
        }
        throw new Error("formattedValue is required");
    }
    handleSubmitCode(invocation) {
        const _super = Object.create(null, {
            kernelInfo: { get: () => super.kernelInfo }
        });
        return __awaiter(this, void 0, void 0, function* () {
            const submitCode = invocation.commandEnvelope.command;
            const code = submitCode.code;
            _super.kernelInfo.localName; //?
            _super.kernelInfo.uri; //?
            _super.kernelInfo.remoteUri; //?
            invocation.context.publish({ eventType: contracts.CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });
            invocation.context.commandEnvelope.routingSlip; //?
            this.capture.kernelInvocationContext = invocation.context;
            let result = undefined;
            try {
                const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
                const evaluator = AsyncFunction("console", code);
                result = yield evaluator(this.capture);
                if (result !== undefined) {
                    const formattedValue = formatValue(result, 'application/json');
                    const event = {
                        formattedValues: [formattedValue]
                    };
                    invocation.context.publish({ eventType: contracts.ReturnValueProducedType, event, command: invocation.commandEnvelope });
                }
            }
            catch (e) {
                throw e; //?
            }
            finally {
                this.capture.kernelInvocationContext = undefined;
            }
        });
    }
    handleRequestValueInfos(invocation) {
        const valueInfos = this.allLocalVariableNames().filter(v => !this.suppressedLocals.has(v)).map(v => ({ name: v, preferredMimeTypes: [] }));
        const event = {
            valueInfos
        };
        invocation.context.publish({ eventType: contracts.ValueInfosProducedType, event, command: invocation.commandEnvelope });
        return Promise.resolve();
    }
    handleRequestValue(invocation) {
        const requestValue = invocation.commandEnvelope.command;
        const rawValue = this.getLocalVariable(requestValue.name);
        const formattedValue = formatValue(rawValue, requestValue.mimeType || 'application/json');
        logger_1.Logger.default.info(`returning ${JSON.stringify(formattedValue)} for ${requestValue.name}`);
        const event = {
            name: requestValue.name,
            formattedValue
        };
        invocation.context.publish({ eventType: contracts.ValueProducedType, event, command: invocation.commandEnvelope });
        return Promise.resolve();
    }
    allLocalVariableNames() {
        const result = [];
        try {
            for (const key in globalThis) {
                try {
                    if (typeof globalThis[key] !== 'function') {
                        result.push(key);
                    }
                }
                catch (e) {
                    logger_1.Logger.default.error(`error getting value for ${key} : ${e}`);
                }
            }
        }
        catch (e) {
            logger_1.Logger.default.error(`error scanning globla variables : ${e}`);
        }
        return result;
    }
    getLocalVariable(name) {
        return globalThis[name];
    }
}
exports.JavascriptKernel = JavascriptKernel;
function formatValue(arg, mimeType) {
    let value;
    switch (mimeType) {
        case 'text/plain':
            value = (arg === null || arg === void 0 ? void 0 : arg.toString()) || 'undefined';
            break;
        case 'application/json':
            value = JSON.stringify(arg);
            break;
        default:
            throw new Error(`unsupported mime type: ${mimeType}`);
    }
    return {
        mimeType,
        value,
    };
}
exports.formatValue = formatValue;
//# sourceMappingURL=javascriptKernel.js.map
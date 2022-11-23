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
exports.InteractiveClient = void 0;
const contracts_1 = require("./dotnet-interactive/contracts");
const utilities_1 = require("./utilities");
const compositeKernel_1 = require("./dotnet-interactive/compositeKernel");
const tokenGenerator_1 = require("./dotnet-interactive/tokenGenerator");
const kernelHost_1 = require("./dotnet-interactive/kernelHost");
const connection = require("./dotnet-interactive/connection");
class InteractiveClient {
    constructor(config) {
        this.config = config;
        this.disposables = [];
        this.nextExecutionCount = 1;
        this.nextOutputId = 1;
        this.nextToken = 1;
        this.tokenEventObservers = new Map();
        this.deferredOutput = [];
        this.valueIdMap = new Map();
        this._kernel = new compositeKernel_1.CompositeKernel("vscode");
        this._kernelHost = new kernelHost_1.KernelHost(this._kernel, config.channel.sender, config.channel.receiver, "kernel://vscode");
        config.channel.receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    this.eventListener(envelope);
                    if (envelope.eventType === contracts_1.KernelInfoProducedType) {
                        const kernelInfoProduced = envelope.event;
                        connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, this._kernel);
                    }
                }
            }
        });
        this._kernelHost.connect();
    }
    get kernel() {
        return this._kernel;
    }
    get kernelHost() {
        return this._kernelHost;
    }
    get channel() {
        return this.config.channel;
    }
    tryGetProperty(propertyName) {
        try {
            return (this.config.channel[propertyName]);
        }
        catch (_a) {
            return null;
        }
    }
    clearExistingLanguageServiceRequests(requestId) {
        (0, utilities_1.clearDebounce)(requestId);
        (0, utilities_1.clearDebounce)(`completion-${requestId}`);
        (0, utilities_1.clearDebounce)(`diagnostics-${requestId}`);
        (0, utilities_1.clearDebounce)(`hover-${requestId}`);
        (0, utilities_1.clearDebounce)(`sighelp-${requestId}`);
    }
    execute(source, language, outputObserver, diagnosticObserver, configuration) {
        if (configuration !== undefined && configuration.id !== undefined) {
            this.clearExistingLanguageServiceRequests(configuration.id);
        }
        return new Promise((resolve, reject) => {
            let diagnostics = [];
            let outputs = [];
            let reportDiagnostics = () => {
                diagnosticObserver(diagnostics);
            };
            let reportOutputs = () => {
                outputObserver(outputs);
            };
            let failureReported = false;
            const commandToken = (configuration === null || configuration === void 0 ? void 0 : configuration.token) ? configuration.token : this.getNextToken();
            const commandId = tokenGenerator_1.Guid.create().toString();
            try {
                return this.submitCode(source, language, eventEnvelope => {
                    var _a, _b, _c;
                    if (this.deferredOutput.length > 0) {
                        outputs.push(...this.deferredOutput);
                        this.deferredOutput = [];
                    }
                    switch (eventEnvelope.eventType) {
                        // if kernel languages were added, handle those events here
                        case contracts_1.CommandSucceededType:
                            if (((_a = eventEnvelope.command) === null || _a === void 0 ? void 0 : _a.id) === commandId) {
                                // only complete this promise if it's the root command
                                resolve(!failureReported);
                            }
                            break;
                        case contracts_1.CommandFailedType:
                            {
                                const err = eventEnvelope.event;
                                const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                                outputs.push(errorOutput);
                                reportOutputs();
                                failureReported = true;
                                if (((_b = eventEnvelope.command) === null || _b === void 0 ? void 0 : _b.id) === commandId) {
                                    // only complete this promise if it's the root command
                                    reject(err);
                                }
                            }
                            break;
                        case contracts_1.ErrorProducedType: {
                            const err = eventEnvelope.event;
                            const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                            outputs.push(errorOutput);
                            reportOutputs();
                            failureReported = true;
                        }
                        case contracts_1.DiagnosticsProducedType:
                            {
                                const diags = eventEnvelope.event;
                                diagnostics.push(...((_c = diags.diagnostics) !== null && _c !== void 0 ? _c : []));
                                reportDiagnostics();
                            }
                            break;
                        case contracts_1.StandardErrorValueProducedType:
                        case contracts_1.StandardOutputValueProducedType:
                            {
                                let disp = eventEnvelope.event;
                                const stream = eventEnvelope.eventType === contracts_1.StandardErrorValueProducedType ? 'stderr' : 'stdout';
                                let output = this.displayEventToCellOutput(disp, stream);
                                outputs.push(output);
                                reportOutputs();
                            }
                            break;
                        case contracts_1.DisplayedValueProducedType:
                        case contracts_1.DisplayedValueUpdatedType:
                        case contracts_1.ReturnValueProducedType:
                            {
                                let disp = eventEnvelope.event;
                                let output = this.displayEventToCellOutput(disp);
                                if (disp.valueId) {
                                    let valueId = this.valueIdMap.get(disp.valueId);
                                    if (valueId !== undefined) {
                                        // update existing value
                                        valueId.outputs[valueId.idx] = output;
                                        valueId.observer(valueId.outputs);
                                        // don't report through regular channels
                                        break;
                                    }
                                    else {
                                        // add new tracked value
                                        this.valueIdMap.set(disp.valueId, {
                                            idx: outputs.length,
                                            outputs,
                                            observer: outputObserver
                                        });
                                        outputs.push(output);
                                    }
                                }
                                else {
                                    // raw value, just push it
                                    outputs.push(output);
                                }
                                reportOutputs();
                            }
                            break;
                    }
                }, commandToken, commandId).catch(e => {
                    // only report a failure if it's not a `CommandFailed` event from above (which has already called `reject()`)
                    if (!failureReported) {
                        const errorMessage = typeof (e === null || e === void 0 ? void 0 : e.message) === 'string' ? e.message : '' + e;
                        const errorOutput = this.config.createErrorOutput(errorMessage, this.getNextOutputId());
                        outputs.push(errorOutput);
                        reportOutputs();
                        reject(e);
                    }
                });
            }
            catch (e) {
                reject(e);
            }
        });
    }
    completion(kernelName, code, line, character, token) {
        let command = {
            code: code,
            linePosition: {
                line,
                character
            },
            targetKernelName: kernelName
        };
        return this.submitCommandAndGetResult(command, contracts_1.RequestCompletionsType, contracts_1.CompletionsProducedType, token);
    }
    hover(language, code, line, character, token) {
        let command = {
            code: code,
            linePosition: {
                line: line,
                character: character,
            },
            targetKernelName: language
        };
        return this.submitCommandAndGetResult(command, contracts_1.RequestHoverTextType, contracts_1.HoverTextProducedType, token);
    }
    signatureHelp(language, code, line, character, token) {
        let command = {
            code,
            linePosition: {
                line,
                character
            },
            targetKernelName: language
        };
        return this.submitCommandAndGetResult(command, contracts_1.RequestSignatureHelpType, contracts_1.SignatureHelpProducedType, token);
    }
    getDiagnostics(kernelName, code, token) {
        return __awaiter(this, void 0, void 0, function* () {
            const command = {
                code,
                targetKernelName: kernelName
            };
            const diagsProduced = yield this.submitCommandAndGetResult(command, contracts_1.RequestDiagnosticsType, contracts_1.DiagnosticsProducedType, token);
            return diagsProduced.diagnostics;
        });
    }
    submitCode(code, language, observer, token, id) {
        return __awaiter(this, void 0, void 0, function* () {
            let command = {
                code: code,
                submissionType: contracts_1.SubmissionType.Run,
                targetKernelName: language
            };
            token = token || this.getNextToken();
            id = id || tokenGenerator_1.Guid.create().toString();
            let disposable = this.subscribeToKernelTokenEvents(token, observer);
            try {
                yield this.submitCommand(command, contracts_1.SubmitCodeType, token, id);
            }
            catch (error) {
                return Promise.reject(error);
            }
            return disposable;
        });
    }
    requestValueInfos(kernelName) {
        const command = {
            targetKernelName: kernelName,
        };
        return this.submitCommandAndGetResult(command, contracts_1.RequestValueInfosType, contracts_1.ValueInfosProducedType, undefined);
    }
    requestValue(valueName, kernelName) {
        const command = {
            name: valueName,
            mimeType: 'text/plain',
            targetKernelName: kernelName,
        };
        return this.submitCommandAndGetResult(command, contracts_1.RequestValueType, contracts_1.ValueProducedType, undefined);
    }
    cancel(token) {
        let command = {};
        token = token || this.getNextToken();
        return this.submitCommand(command, contracts_1.CancelType, token, undefined);
    }
    dispose() {
        const command = {};
        this.config.channel.sender.send({
            commandType: contracts_1.QuitType,
            command,
        });
        this.config.channel.dispose();
        for (let disposable of this.disposables) {
            disposable();
        }
    }
    registerForDisposal(disposable) {
        this.disposables.push(disposable);
    }
    submitCommandAndGetResult(command, commandType, expectedEventType, token) {
        return new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
            let handled = false;
            token = token || this.getNextToken();
            const id = tokenGenerator_1.Guid.create().toString();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                var _a;
                if (((_a = eventEnvelope.command) === null || _a === void 0 ? void 0 : _a.token) === token) {
                    switch (eventEnvelope.eventType) {
                        case contracts_1.CommandFailedType:
                            if (!handled) {
                                handled = true;
                                disposable.dispose();
                                let err = eventEnvelope.event;
                                reject(err);
                            }
                            break;
                        case contracts_1.CommandSucceededType:
                            if (!handled) {
                                handled = true;
                                disposable.dispose();
                                reject('Command was handled before reporting expected result.');
                            }
                            break;
                        default:
                            if (eventEnvelope.eventType === expectedEventType) {
                                handled = true;
                                disposable.dispose();
                                let event = eventEnvelope.event;
                                resolve(event);
                            }
                            break;
                    }
                }
            });
            yield this.config.channel.sender.send({ command, commandType, token, id });
        }));
    }
    submitCommand(command, commandType, token, id) {
        return new Promise((resolve, reject) => {
            let failureReported = false;
            token = token || this.getNextToken();
            id = id || tokenGenerator_1.Guid.create().toString();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                var _a, _b;
                switch (eventEnvelope.eventType) {
                    case contracts_1.CommandFailedType:
                        let err = eventEnvelope.event;
                        failureReported = true;
                        if (((_a = eventEnvelope.command) === null || _a === void 0 ? void 0 : _a.id) === id) {
                            disposable.dispose();
                            reject(err);
                        }
                        break;
                    case contracts_1.CommandSucceededType:
                        if (((_b = eventEnvelope.command) === null || _b === void 0 ? void 0 : _b.id) === id) {
                            disposable.dispose();
                            resolve();
                        }
                        break;
                    default:
                        break;
                }
            });
            try {
                this.config.channel.sender
                    .send({ command, commandType, token, id })
                    .catch(e => {
                    // only report a failure if it's not a `CommandFailed` event from above (which has already called `reject()`)
                    if (!failureReported) {
                        reject(e);
                    }
                });
            }
            catch (error) {
                reject(error);
            }
        });
    }
    subscribeToKernelTokenEvents(token, observer) {
        var _a;
        if (!this.tokenEventObservers.get(token)) {
            this.tokenEventObservers.set(token, []);
        }
        (_a = this.tokenEventObservers.get(token)) === null || _a === void 0 ? void 0 : _a.push(observer);
        return {
            dispose: () => {
                let listeners = this.tokenEventObservers.get(token);
                if (listeners) {
                    let i = listeners.indexOf(observer);
                    if (i >= 0) {
                        listeners.splice(i, 1);
                    }
                    if (listeners.length === 0) {
                        this.tokenEventObservers.delete(token);
                    }
                }
            }
        };
    }
    eventListener(eventEnvelope) {
        var _a;
        let token = (_a = eventEnvelope.command) === null || _a === void 0 ? void 0 : _a.token;
        if (token) {
            if (token.startsWith("deferredCommand::")) {
                switch (eventEnvelope.eventType) {
                    case contracts_1.DisplayedValueProducedType:
                    case contracts_1.DisplayedValueUpdatedType:
                    case contracts_1.ReturnValueProducedType:
                        let disp = eventEnvelope.event;
                        let output = this.displayEventToCellOutput(disp);
                        this.deferredOutput.push(output);
                        break;
                }
            }
            else {
                const tokenParts = token.split('/');
                for (let i = tokenParts.length; i >= 1; i--) {
                    const candidateToken = tokenParts.slice(0, i).join('/');
                    let listeners = this.tokenEventObservers.get(candidateToken);
                    if (listeners) {
                        for (let listener of listeners) {
                            listener(eventEnvelope);
                        }
                    }
                }
            }
        }
    }
    displayEventToCellOutput(disp, stream) {
        const encoder = new TextEncoder();
        let outputItems = [];
        if (disp.formattedValues && disp.formattedValues.length > 0) {
            for (let formatted of disp.formattedValues) {
                let data = this.IsEncodedMimeType(formatted.mimeType)
                    ? Buffer.from(formatted.value, 'base64')
                    : encoder.encode(formatted.value);
                const outputItem = {
                    mime: formatted.mimeType,
                    data
                };
                if (stream) {
                    outputItem.stream = stream;
                }
                outputItems.push(outputItem);
            }
        }
        const output = (0, utilities_1.createOutput)(outputItems, this.getNextOutputId());
        return output;
    }
    IsEncodedMimeType(mimeType) {
        const encdodedMimetypes = new Set(["image/png", "image/jpeg", "image/gif"]);
        return encdodedMimetypes.has(mimeType);
    }
    resetExecutionCount() {
        this.nextExecutionCount = 1;
    }
    getNextExecutionCount() {
        const next = this.nextExecutionCount;
        this.nextExecutionCount++;
        return next;
    }
    getNextOutputId() {
        return (this.nextOutputId++).toString();
    }
    getNextToken() {
        return (this.nextToken++).toString();
    }
}
exports.InteractiveClient = InteractiveClient;
//# sourceMappingURL=interactiveClient.js.map
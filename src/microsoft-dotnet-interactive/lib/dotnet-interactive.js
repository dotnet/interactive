(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
    typeof define === 'function' && define.amd ? define(['exports'], factory) :
    (global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global.dotnetInteractive = {}));
})(this, (function (exports) { 'use strict';

    /******************************************************************************
    Copyright (c) Microsoft Corporation.

    Permission to use, copy, modify, and/or distribute this software for any
    purpose with or without fee is hereby granted.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
    REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
    AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
    INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
    LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
    OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
    PERFORMANCE OF THIS SOFTWARE.
    ***************************************************************************** */

    function __awaiter(thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.
    // Generated TypeScript interfaces and types.
    // --------------------------------------------- Kernel Commands
    const AddPackageType = "AddPackage";
    const CancelType = "Cancel";
    const ChangeWorkingDirectoryType = "ChangeWorkingDirectory";
    const CompileProjectType = "CompileProject";
    const DisplayErrorType = "DisplayError";
    const DisplayValueType = "DisplayValue";
    const OpenDocumentType = "OpenDocument";
    const OpenProjectType = "OpenProject";
    const QuitType = "Quit";
    const RequestCompletionsType = "RequestCompletions";
    const RequestDiagnosticsType = "RequestDiagnostics";
    const RequestHoverTextType = "RequestHoverText";
    const RequestInputType = "RequestInput";
    const RequestKernelInfoType = "RequestKernelInfo";
    const RequestSignatureHelpType = "RequestSignatureHelp";
    const RequestValueType = "RequestValue";
    const RequestValueInfosType = "RequestValueInfos";
    const SendEditableCodeType = "SendEditableCode";
    const SubmitCodeType = "SubmitCode";
    const UpdateDisplayedValueType = "UpdateDisplayedValue";
    // --------------------------------------------- Kernel events
    const AssemblyProducedType = "AssemblyProduced";
    const CodeSubmissionReceivedType = "CodeSubmissionReceived";
    const CommandCancelledType = "CommandCancelled";
    const CommandFailedType = "CommandFailed";
    const CommandSucceededType = "CommandSucceeded";
    const CompleteCodeSubmissionReceivedType = "CompleteCodeSubmissionReceived";
    const CompletionsProducedType = "CompletionsProduced";
    const DiagnosticLogEntryProducedType = "DiagnosticLogEntryProduced";
    const DiagnosticsProducedType = "DiagnosticsProduced";
    const DisplayedValueProducedType = "DisplayedValueProduced";
    const DisplayedValueUpdatedType = "DisplayedValueUpdated";
    const DocumentOpenedType = "DocumentOpened";
    const ErrorProducedType = "ErrorProduced";
    const HoverTextProducedType = "HoverTextProduced";
    const IncompleteCodeSubmissionReceivedType = "IncompleteCodeSubmissionReceived";
    const InputProducedType = "InputProduced";
    const KernelExtensionLoadedType = "KernelExtensionLoaded";
    const KernelInfoProducedType = "KernelInfoProduced";
    const KernelReadyType = "KernelReady";
    const PackageAddedType = "PackageAdded";
    const ProjectOpenedType = "ProjectOpened";
    const ReturnValueProducedType = "ReturnValueProduced";
    const SignatureHelpProducedType = "SignatureHelpProduced";
    const StandardErrorValueProducedType = "StandardErrorValueProduced";
    const StandardOutputValueProducedType = "StandardOutputValueProduced";
    const ValueInfosProducedType = "ValueInfosProduced";
    const ValueProducedType = "ValueProduced";
    const WorkingDirectoryChangedType = "WorkingDirectoryChanged";
    exports.InsertTextFormat = void 0;
    (function (InsertTextFormat) {
        InsertTextFormat["PlainText"] = "plaintext";
        InsertTextFormat["Snippet"] = "snippet";
    })(exports.InsertTextFormat || (exports.InsertTextFormat = {}));
    exports.DiagnosticSeverity = void 0;
    (function (DiagnosticSeverity) {
        DiagnosticSeverity["Hidden"] = "hidden";
        DiagnosticSeverity["Info"] = "info";
        DiagnosticSeverity["Warning"] = "warning";
        DiagnosticSeverity["Error"] = "error";
    })(exports.DiagnosticSeverity || (exports.DiagnosticSeverity = {}));
    exports.DocumentSerializationType = void 0;
    (function (DocumentSerializationType) {
        DocumentSerializationType["Dib"] = "dib";
        DocumentSerializationType["Ipynb"] = "ipynb";
    })(exports.DocumentSerializationType || (exports.DocumentSerializationType = {}));
    exports.RequestType = void 0;
    (function (RequestType) {
        RequestType["Parse"] = "parse";
        RequestType["Serialize"] = "serialize";
    })(exports.RequestType || (exports.RequestType = {}));
    exports.SubmissionType = void 0;
    (function (SubmissionType) {
        SubmissionType["Run"] = "run";
        SubmissionType["Diagnose"] = "diagnose";
    })(exports.SubmissionType || (exports.SubmissionType = {}));

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.
    function isKernelEventEnvelope(obj) {
        return obj.eventType
            && obj.event;
    }
    function isKernelCommandEnvelope(obj) {
        return obj.commandType
            && obj.command;
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.
    exports.LogLevel = void 0;
    (function (LogLevel) {
        LogLevel[LogLevel["Info"] = 0] = "Info";
        LogLevel[LogLevel["Warn"] = 1] = "Warn";
        LogLevel[LogLevel["Error"] = 2] = "Error";
        LogLevel[LogLevel["None"] = 3] = "None";
    })(exports.LogLevel || (exports.LogLevel = {}));
    class Logger {
        constructor(source, write) {
            this.source = source;
            this.write = write;
        }
        info(message) {
            this.write({ logLevel: exports.LogLevel.Info, source: this.source, message });
        }
        warn(message) {
            this.write({ logLevel: exports.LogLevel.Warn, source: this.source, message });
        }
        error(message) {
            this.write({ logLevel: exports.LogLevel.Error, source: this.source, message });
        }
        static configure(source, writer) {
            const logger = new Logger(source, writer);
            Logger._default = logger;
        }
        static get default() {
            if (Logger._default) {
                return Logger._default;
            }
            throw new Error('No logger has been configured for this context');
        }
    }
    Logger._default = new Logger('default', (_entry) => { });

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function isPromiseCompletionSource(obj) {
        return obj.promise
            && obj.resolve
            && obj.reject;
    }
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
                    if (isKernelCommandEnvelope(message)) {
                        this.commandHandler(message);
                    }
                    else if (isKernelEventEnvelope(message)) {
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
                Logger.default.info(`channel building promise awaiter`);
                this._waitingOnMessages = new PromiseCompletionSource();
                return this._waitingOnMessages.promise;
            }
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.
    class Guid {
        constructor(guid) {
            if (!guid) {
                throw new TypeError("Invalid argument; `value` has no value.");
            }
            this.value = Guid.EMPTY;
            if (guid && Guid.isGuid(guid)) {
                this.value = guid;
            }
        }
        static isGuid(guid) {
            const value = guid.toString();
            return guid && (guid instanceof Guid || Guid.validator.test(value));
        }
        static create() {
            return new Guid([Guid.gen(2), Guid.gen(1), Guid.gen(1), Guid.gen(1), Guid.gen(3)].join("-"));
        }
        static createEmpty() {
            return new Guid("emptyguid");
        }
        static parse(guid) {
            return new Guid(guid);
        }
        static raw() {
            return [Guid.gen(2), Guid.gen(1), Guid.gen(1), Guid.gen(1), Guid.gen(3)].join("-");
        }
        static gen(count) {
            let out = "";
            for (let i = 0; i < count; i++) {
                // tslint:disable-next-line:no-bitwise
                out += (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
            }
            return out;
        }
        equals(other) {
            // Comparing string `value` against provided `guid` will auto-call
            // toString on `guid` for comparison
            return Guid.isGuid(other) && this.value === other.toString();
        }
        isEmpty() {
            return this.value === Guid.EMPTY;
        }
        toString() {
            return this.value;
        }
        toJSON() {
            return {
                value: this.value,
            };
        }
    }
    Guid.validator = new RegExp("^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$", "i");
    Guid.EMPTY = "00000000-0000-0000-0000-000000000000";
    class TokenGenerator {
        constructor() {
            this._seed = Guid.create().toString();
            this._counter = 0;
        }
        GetNewToken() {
            this._counter++;
            return `${this._seed}::${this._counter}`;
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class KernelInvocationContext {
        constructor(kernelCommandInvocation) {
            this._childCommands = [];
            this._tokenGenerator = new TokenGenerator();
            this._eventObservers = new Map();
            this._isComplete = false;
            this.handlingKernel = null;
            this.completionSource = new PromiseCompletionSource();
            this._commandEnvelope = kernelCommandInvocation;
        }
        get promise() {
            return this.completionSource.promise;
        }
        static establish(kernelCommandInvocation) {
            let current = KernelInvocationContext._current;
            if (!current || current._isComplete) {
                KernelInvocationContext._current = new KernelInvocationContext(kernelCommandInvocation);
            }
            else {
                if (!areCommandsTheSame(kernelCommandInvocation, current._commandEnvelope)) {
                    const found = current._childCommands.includes(kernelCommandInvocation);
                    if (!found) {
                        current._childCommands.push(kernelCommandInvocation);
                    }
                }
            }
            return KernelInvocationContext._current;
        }
        static get current() { return this._current; }
        get command() { return this._commandEnvelope.command; }
        get commandEnvelope() { return this._commandEnvelope; }
        subscribeToKernelEvents(observer) {
            let subToken = this._tokenGenerator.GetNewToken();
            this._eventObservers.set(subToken, observer);
            return {
                dispose: () => {
                    this._eventObservers.delete(subToken);
                }
            };
        }
        complete(command) {
            if (command === this._commandEnvelope) {
                this._isComplete = true;
                let succeeded = {};
                let eventEnvelope = {
                    command: this._commandEnvelope,
                    eventType: CommandSucceededType,
                    event: succeeded
                };
                this.internalPublish(eventEnvelope);
                this.completionSource.resolve();
                // TODO: C# version has completion callbacks - do we need these?
                // if (!_events.IsDisposed)
                // {
                //     _events.OnCompleted();
                // }
            }
            else {
                let pos = this._childCommands.indexOf(command);
                delete this._childCommands[pos];
            }
        }
        fail(message) {
            // TODO:
            // The C# code accepts a message and/or an exception. Do we need to add support
            // for exceptions? (The TS CommandFailed interface doesn't have a place for it right now.)
            this._isComplete = true;
            let failed = { message: message !== null && message !== void 0 ? message : "Command Failed" };
            let eventEnvelope = {
                command: this._commandEnvelope,
                eventType: CommandFailedType,
                event: failed
            };
            this.internalPublish(eventEnvelope);
            this.completionSource.resolve();
        }
        publish(kernelEvent) {
            if (!this._isComplete) {
                this.internalPublish(kernelEvent);
            }
        }
        internalPublish(kernelEvent) {
            let command = kernelEvent.command;
            if (command === null ||
                areCommandsTheSame(command, this._commandEnvelope) ||
                this._childCommands.includes(command)) {
                this._eventObservers.forEach((observer) => {
                    observer(kernelEvent);
                });
            }
        }
        isParentOfCommand(commandEnvelope) {
            const childFound = this._childCommands.includes(commandEnvelope);
            return childFound;
        }
        dispose() {
            if (!this._isComplete) {
                this.complete(this._commandEnvelope);
            }
            KernelInvocationContext._current = null;
        }
    }
    KernelInvocationContext._current = null;
    function areCommandsTheSame(envelope1, envelope2) {
        return envelope1 === envelope2
            || (envelope1.commandType === envelope2.commandType && envelope1.token === envelope2.token);
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class KernelScheduler {
        constructor() {
            this.operationQueue = [];
        }
        runAsync(value, executor) {
            const operation = {
                value,
                executor,
                promiseCompletionSource: new PromiseCompletionSource(),
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
        executeNextCommand() {
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

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class Kernel {
        constructor(name, languageName, languageVersion) {
            this.name = name;
            this._commandHandlers = new Map();
            this._eventObservers = {};
            this._tokenGenerator = new TokenGenerator();
            this.rootKernel = this;
            this.parentKernel = null;
            this._scheduler = null;
            this._kernelInfo = {
                localName: name,
                languageName: languageName,
                aliases: [],
                languageVersion: languageVersion,
                supportedDirectives: [],
                supportedKernelCommands: []
            };
            this.registerCommandHandler({
                commandType: RequestKernelInfoType, handle: (invocation) => __awaiter(this, void 0, void 0, function* () {
                    yield this.handleRequestKernelInfo(invocation);
                })
            });
        }
        get kernelInfo() {
            return this._kernelInfo;
        }
        handleRequestKernelInfo(invocation) {
            return __awaiter(this, void 0, void 0, function* () {
                const eventEnvelope = {
                    eventType: KernelInfoProducedType,
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
                this._scheduler = (_b = (_a = this.parentKernel) === null || _a === void 0 ? void 0 : _a.getScheduler()) !== null && _b !== void 0 ? _b : new KernelScheduler();
            }
            return this._scheduler;
        }
        ensureCommandTokenAndId(commandEnvelope) {
            var _a;
            if (!commandEnvelope.token) {
                let nextToken = this._tokenGenerator.GetNewToken();
                if ((_a = KernelInvocationContext.current) === null || _a === void 0 ? void 0 : _a.commandEnvelope) {
                    // a parent command exists, create a token hierarchy
                    nextToken = KernelInvocationContext.current.commandEnvelope.token;
                }
                commandEnvelope.token = nextToken;
            }
            if (!commandEnvelope.id) {
                commandEnvelope.id = Guid.create().toString();
            }
        }
        static get current() {
            if (KernelInvocationContext.current) {
                return KernelInvocationContext.current.handlingKernel;
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
                let context = KernelInvocationContext.establish(commandEnvelope);
                this.getScheduler().runAsync(commandEnvelope, (value) => this.executeCommand(value));
                return context.promise;
            });
        }
        executeCommand(commandEnvelope) {
            return __awaiter(this, void 0, void 0, function* () {
                let context = KernelInvocationContext.establish(commandEnvelope);
                let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);
                let contextEventsSubscription = null;
                if (isRootCommand) {
                    contextEventsSubscription = context.subscribeToKernelEvents(e => {
                        var _a;
                        const message = `kernel ${this.name} saw event ${e.eventType} with token ${(_a = e.command) === null || _a === void 0 ? void 0 : _a.token}`;
                        Logger.default.info(message);
                        return this.publishEvent(e);
                    });
                }
                try {
                    yield this.handleCommand(commandEnvelope);
                }
                catch (e) {
                    context.fail((e === null || e === void 0 ? void 0 : e.message) || JSON.stringify(e));
                }
                finally {
                    if (contextEventsSubscription) {
                        contextEventsSubscription.dispose();
                    }
                }
            });
        }
        getCommandHandler(commandType) {
            return this._commandHandlers.get(commandType);
        }
        handleCommand(commandEnvelope) {
            return new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
                let context = KernelInvocationContext.establish(commandEnvelope); //?
                context.handlingKernel = this;
                let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);
                let handler = this.getCommandHandler(commandEnvelope.commandType);
                if (handler) {
                    try {
                        Logger.default.info(`kernel ${this.name} about to handle command: ${JSON.stringify(commandEnvelope)}`);
                        yield handler.handle({ commandEnvelope: commandEnvelope, context });
                        context.complete(commandEnvelope);
                        if (isRootCommand) {
                            context.dispose();
                        }
                        Logger.default.info(`kernel ${this.name} done handling command: ${JSON.stringify(commandEnvelope)}`);
                        resolve();
                    }
                    catch (e) {
                        context.fail((e === null || e === void 0 ? void 0 : e.message) || JSON.stringify(e));
                        if (isRootCommand) {
                            context.dispose();
                        }
                        reject(e);
                    }
                }
                else {
                    if (isRootCommand) {
                        context.dispose();
                    }
                    reject(new Error(`No handler found for command type ${commandEnvelope.commandType}`));
                }
            }));
        }
        subscribeToKernelEvents(observer) {
            let subToken = this._tokenGenerator.GetNewToken();
            this._eventObservers[subToken] = observer;
            return {
                dispose: () => { delete this._eventObservers[subToken]; }
            };
        }
        canHandle(commandEnvelope) {
            if (commandEnvelope.command.targetKernelName && commandEnvelope.command.targetKernelName !== this.name) {
                return false;
            }
            if (commandEnvelope.command.destinationUri) {
                if (this.kernelInfo.uri !== commandEnvelope.command.destinationUri) {
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
            this._commandHandlers.set(handler.commandType, handler);
            this._kernelInfo.supportedKernelCommands = Array.from(this._commandHandlers.keys()).map(commandName => ({ name: commandName }));
        }
        getHandlingKernel(commandEnvelope) {
            var _a;
            let targetKernelName = (_a = commandEnvelope.command.targetKernelName) !== null && _a !== void 0 ? _a : this.name;
            return targetKernelName === this.name ? this : undefined;
        }
        publishEvent(kernelEvent) {
            let keys = Object.keys(this._eventObservers);
            for (let subToken of keys) {
                let observer = this._eventObservers[subToken];
                observer(kernelEvent);
            }
        }
    }
    function submitCommandAndGetResult(kernel, commandEnvelope, expectedEventType) {
        return __awaiter(this, void 0, void 0, function* () {
            let completionSource = new PromiseCompletionSource();
            let handled = false;
            let disposable = kernel.subscribeToKernelEvents(eventEnvelope => {
                var _a, _b;
                if (((_a = eventEnvelope.command) === null || _a === void 0 ? void 0 : _a.token) === commandEnvelope.token) {
                    switch (eventEnvelope.eventType) {
                        case CommandFailedType:
                            if (!handled) {
                                handled = true;
                                let err = eventEnvelope.event; //?
                                completionSource.reject(err);
                            }
                            break;
                        case CommandSucceededType:
                            if (areCommandsTheSame(eventEnvelope.command, commandEnvelope)
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

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class CompositeKernel extends Kernel {
        constructor(name) {
            super(name);
            this._host = null;
            this._namesTokernelMap = new Map();
            this._kernelToNamesMap = new Map();
        }
        get childKernels() {
            return [...this._kernelToNamesMap.keys()];
        }
        get host() {
            return this._host;
        }
        set host(host) {
            this._host = host;
            if (this._host) {
                this._host.addKernelInfo(this, { localName: this.name.toLowerCase(), aliases: [], supportedDirectives: [], supportedKernelCommands: [] });
                for (let kernel of this.childKernels) {
                    let aliases = [];
                    for (let name of this._kernelToNamesMap.get(kernel)) {
                        if (name !== kernel.name) {
                            aliases.push(name.toLowerCase());
                        }
                    }
                    this._host.addKernelInfo(kernel, { localName: kernel.name.toLowerCase(), aliases: [...aliases], supportedDirectives: [], supportedKernelCommands: [] });
                }
            }
        }
        handleRequestKernelInfo(invocation) {
            return __awaiter(this, void 0, void 0, function* () {
                for (let kernel of this.childKernels) {
                    if (kernel.supportsCommand(invocation.commandEnvelope.commandType)) {
                        yield kernel.handleCommand({ command: {}, commandType: RequestKernelInfoType });
                    }
                }
            });
        }
        add(kernel, aliases) {
            var _a;
            if (!kernel) {
                throw new Error("kernel cannot be null or undefined");
            }
            if (!this.defaultKernelName) {
                // default to first kernel
                this.defaultKernelName = kernel.name;
            }
            kernel.parentKernel = this;
            kernel.rootKernel = this.rootKernel;
            kernel.subscribeToKernelEvents(event => {
                this.publishEvent(event);
            });
            this._namesTokernelMap.set(kernel.name.toLowerCase(), kernel);
            let kernelNames = new Set();
            kernelNames.add(kernel.name);
            if (aliases) {
                aliases.forEach(alias => {
                    this._namesTokernelMap.set(alias.toLowerCase(), kernel);
                    kernelNames.add(alias.toLowerCase());
                });
                kernel.kernelInfo.aliases = aliases;
            }
            this._kernelToNamesMap.set(kernel, kernelNames);
            (_a = this.host) === null || _a === void 0 ? void 0 : _a.addKernelInfo(kernel, kernel.kernelInfo);
        }
        findKernelByName(kernelName) {
            if (kernelName.toLowerCase() === this.name.toLowerCase()) {
                return this;
            }
            return this._namesTokernelMap.get(kernelName.toLowerCase());
        }
        findKernelByUri(uri) {
            const kernels = Array.from(this._kernelToNamesMap.keys());
            for (let kernel of kernels) {
                if (kernel.kernelInfo.uri === uri) {
                    return kernel;
                }
            }
            for (let kernel of kernels) {
                if (kernel.kernelInfo.remoteUri === uri) {
                    return kernel;
                }
            }
            return undefined;
        }
        handleCommand(commandEnvelope) {
            let kernel = commandEnvelope.command.targetKernelName === this.name
                ? this
                : this.getHandlingKernel(commandEnvelope);
            if (kernel === this) {
                return super.handleCommand(commandEnvelope);
            }
            else if (kernel) {
                return kernel.handleCommand(commandEnvelope);
            }
            return Promise.reject(new Error("Kernel not found: " + commandEnvelope.command.targetKernelName));
        }
        getHandlingKernel(commandEnvelope) {
            var _a, _b;
            if (commandEnvelope.command.destinationUri) {
                let kernel = this.findKernelByUri(commandEnvelope.command.destinationUri);
                if (kernel) {
                    return kernel;
                }
            }
            if (!commandEnvelope.command.targetKernelName) {
                if (super.canHandle(commandEnvelope)) {
                    return this;
                }
            }
            let targetKernelName = (_b = (_a = commandEnvelope.command.targetKernelName) !== null && _a !== void 0 ? _a : this.defaultKernelName) !== null && _b !== void 0 ? _b : this.name;
            let kernel = this.findKernelByName(targetKernelName);
            return kernel;
        }
    }

    class ConsoleCapture {
        constructor(kernelInvocationContext) {
            this.kernelInvocationContext = kernelInvocationContext;
            this.originalConsole = console;
            console = this;
        }
        assert(value, message, ...optionalParams) {
            this.originalConsole.assert(value, message, optionalParams);
        }
        clear() {
            this.originalConsole.clear();
        }
        count(label) {
            this.originalConsole.count(label);
        }
        countReset(label) {
            this.originalConsole.countReset(label);
        }
        debug(message, ...optionalParams) {
            this.originalConsole.debug(message, optionalParams);
        }
        dir(obj, options) {
            this.originalConsole.dir(obj, options);
        }
        dirxml(...data) {
            this.originalConsole.dirxml(data);
        }
        error(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.error, ...[message, ...optionalParams]);
        }
        group(...label) {
            this.originalConsole.group(label);
        }
        groupCollapsed(...label) {
            this.originalConsole.groupCollapsed(label);
        }
        groupEnd() {
            this.originalConsole.groupEnd();
        }
        info(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.info, ...[message, ...optionalParams]);
        }
        log(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.log, ...[message, ...optionalParams]);
        }
        table(tabularData, properties) {
            this.originalConsole.table(tabularData, properties);
        }
        time(label) {
            this.originalConsole.time(label);
        }
        timeEnd(label) {
            this.originalConsole.timeEnd(label);
        }
        timeLog(label, ...data) {
            this.originalConsole.timeLog(label, data);
        }
        timeStamp(label) {
            this.originalConsole.timeStamp(label);
        }
        trace(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.trace, ...[message, ...optionalParams]);
        }
        warn(message, ...optionalParams) {
            this.originalConsole.warn(message, optionalParams);
        }
        profile(label) {
            this.originalConsole.profile(label);
        }
        profileEnd(label) {
            this.originalConsole.profileEnd(label);
        }
        dispose() {
            console = this.originalConsole;
        }
        redirectAndPublish(target, ...args) {
            target(...args);
            this.publishArgsAsEvents(...args);
        }
        publishArgsAsEvents(...args) {
            for (const arg of args) {
                let mimeType;
                let value;
                if (typeof arg !== 'object' && !Array.isArray(arg)) {
                    mimeType = 'text/plain';
                    value = arg === null || arg === void 0 ? void 0 : arg.toString();
                }
                else {
                    mimeType = 'application/json';
                    value = JSON.stringify(arg);
                }
                const displayedValue = {
                    formattedValues: [
                        {
                            mimeType,
                            value,
                        }
                    ]
                };
                const eventEnvelope = {
                    eventType: DisplayedValueProducedType,
                    event: displayedValue,
                    command: this.kernelInvocationContext.commandEnvelope
                };
                this.kernelInvocationContext.publish(eventEnvelope);
            }
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class HtmlKernel extends Kernel {
        constructor(kernelName, htmlFragmentProcessor, languageName, languageVersion) {
            super(kernelName !== null && kernelName !== void 0 ? kernelName : "html", languageName !== null && languageName !== void 0 ? languageName : "HTML");
            this.htmlFragmentProcessor = htmlFragmentProcessor;
            if (!this.htmlFragmentProcessor) {
                this.htmlFragmentProcessor = domHtmlFragmentProcessor;
            }
            this.registerCommandHandler({ commandType: SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
        }
        handleSubmitCode(invocation) {
            return __awaiter(this, void 0, void 0, function* () {
                const submitCode = invocation.commandEnvelope.command;
                const code = submitCode.code;
                invocation.context.publish({ eventType: CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });
                if (!this.htmlFragmentProcessor) {
                    throw new Error("No HTML fragment processor registered");
                }
                try {
                    yield this.htmlFragmentProcessor(code);
                }
                catch (e) {
                    throw e; //?
                }
            });
        }
    }
    function domHtmlFragmentProcessor(htmlFragment, configuration) {
        var _a, _b, _c, _d;
        const factory = (_a = configuration === null || configuration === void 0 ? void 0 : configuration.containerFactory) !== null && _a !== void 0 ? _a : (() => document.createElement("div"));
        const elementToObserve = (_b = configuration === null || configuration === void 0 ? void 0 : configuration.elementToObserve) !== null && _b !== void 0 ? _b : (() => document.body);
        const addToDom = (_c = configuration === null || configuration === void 0 ? void 0 : configuration.addToDom) !== null && _c !== void 0 ? _c : ((element) => document.body.appendChild(element));
        const mutationObserverFactory = (_d = configuration === null || configuration === void 0 ? void 0 : configuration.mutationObserverFactory) !== null && _d !== void 0 ? _d : (callback => new MutationObserver(callback));
        let container = factory();
        if (!container.id) {
            container.id = "html_kernel_container" + Math.floor(Math.random() * 1000000);
        }
        container.innerHTML = htmlFragment;
        const completionPromise = new PromiseCompletionSource();
        const mutationObserver = mutationObserverFactory((mutations, observer) => {
            for (const mutation of mutations) {
                if (mutation.type === "childList") {
                    const nodes = Array.from(mutation.addedNodes);
                    for (const addedNode of nodes) {
                        const element = addedNode;
                        element.id; //?
                        container.id; //?
                        if ((element === null || element === void 0 ? void 0 : element.id) === container.id) { //?
                            completionPromise.resolve();
                            mutationObserver.disconnect();
                            return;
                        }
                    }
                }
            }
        });
        mutationObserver.observe(elementToObserve(), { childList: true, subtree: true });
        addToDom(container);
        return completionPromise.promise;
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class JavascriptKernel extends Kernel {
        constructor(name) {
            super(name !== null && name !== void 0 ? name : "javascript", "Javascript");
            this.suppressedLocals = new Set(this.allLocalVariableNames());
            this.registerCommandHandler({ commandType: SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
            this.registerCommandHandler({ commandType: RequestValueInfosType, handle: invocation => this.handleRequestValueInfos(invocation) });
            this.registerCommandHandler({ commandType: RequestValueType, handle: invocation => this.handleRequestValue(invocation) });
        }
        handleSubmitCode(invocation) {
            return __awaiter(this, void 0, void 0, function* () {
                const submitCode = invocation.commandEnvelope.command;
                const code = submitCode.code;
                invocation.context.publish({ eventType: CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });
                let capture = new ConsoleCapture(invocation.context);
                let result = undefined;
                try {
                    const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
                    const evaluator = AsyncFunction("console", code);
                    result = yield evaluator(capture);
                    if (result !== undefined) {
                        const formattedValue = formatValue(result, 'application/json');
                        const event = {
                            formattedValues: [formattedValue]
                        };
                        invocation.context.publish({ eventType: ReturnValueProducedType, event, command: invocation.commandEnvelope });
                    }
                }
                catch (e) {
                    capture.dispose();
                    capture = undefined;
                    throw e; //?
                }
                finally {
                    if (capture) {
                        capture.dispose();
                    }
                }
            });
        }
        handleRequestValueInfos(invocation) {
            const valueInfos = this.allLocalVariableNames().filter(v => !this.suppressedLocals.has(v)).map(v => ({ name: v }));
            const event = {
                valueInfos
            };
            invocation.context.publish({ eventType: ValueInfosProducedType, event, command: invocation.commandEnvelope });
            return Promise.resolve();
        }
        handleRequestValue(invocation) {
            const requestValue = invocation.commandEnvelope.command;
            const rawValue = this.getLocalVariable(requestValue.name);
            const formattedValue = formatValue(rawValue, requestValue.mimeType || 'application/json');
            Logger.default.info(`returning ${JSON.stringify(formattedValue)} for ${requestValue.name}`);
            const event = {
                name: requestValue.name,
                formattedValue
            };
            invocation.context.publish({ eventType: ValueProducedType, event, command: invocation.commandEnvelope });
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
                        Logger.default.error(`error getting value for ${key} : ${e}`);
                    }
                }
            }
            catch (e) {
                Logger.default.error(`error scanning globla variables : ${e}`);
            }
            return result;
        }
        getLocalVariable(name) {
            return globalThis[name];
        }
    }
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

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class ProxyKernel extends Kernel {
        constructor(name, channel) {
            super(name);
            this.name = name;
            this.channel = channel;
        }
        getCommandHandler(commandType) {
            return {
                commandType,
                handle: (invocation) => {
                    return this._commandHandler(invocation);
                }
            };
        }
        _commandHandler(commandInvocation) {
            var _a, _b, _c, _d;
            var _e, _f;
            return __awaiter(this, void 0, void 0, function* () {
                const token = commandInvocation.commandEnvelope.token;
                const completionSource = new PromiseCompletionSource();
                let sub = this.channel.subscribeToKernelEvents((envelope) => {
                    Logger.default.info(`proxy ${this.name} got event ${JSON.stringify(envelope)}`);
                    if (envelope.command.token === token) {
                        switch (envelope.eventType) {
                            case CommandFailedType:
                            case CommandSucceededType:
                                if (envelope.command.id === commandInvocation.commandEnvelope.id) {
                                    completionSource.resolve(envelope);
                                }
                                else {
                                    commandInvocation.context.publish(envelope);
                                }
                                break;
                            default:
                                commandInvocation.context.publish(envelope);
                                break;
                        }
                    }
                });
                try {
                    if (!commandInvocation.commandEnvelope.command.destinationUri || !commandInvocation.commandEnvelope.command.originUri) {
                        const kernelInfo = (_b = (_a = this.parentKernel) === null || _a === void 0 ? void 0 : _a.host) === null || _b === void 0 ? void 0 : _b.tryGetKernelInfo(this);
                        if (kernelInfo) {
                            (_c = (_e = commandInvocation.commandEnvelope.command).originUri) !== null && _c !== void 0 ? _c : (_e.originUri = kernelInfo.uri);
                            (_d = (_f = commandInvocation.commandEnvelope.command).destinationUri) !== null && _d !== void 0 ? _d : (_f.destinationUri = kernelInfo.remoteUri);
                        }
                    }
                    this.channel.submitCommand(commandInvocation.commandEnvelope);
                    Logger.default.info(`proxy ${this.name} about to await with token ${token}`);
                    const enventEnvelope = yield completionSource.promise;
                    if (enventEnvelope.eventType === CommandFailedType) {
                        commandInvocation.context.fail(enventEnvelope.event.message);
                    }
                    Logger.default.info(`proxy ${this.name} done awaiting with token ${token}`);
                }
                catch (e) {
                    commandInvocation.context.fail(e.message);
                }
                finally {
                    sub.dispose();
                }
            });
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class KernelHost {
        constructor(_kernel, _channel, hostUri) {
            this._kernel = _kernel;
            this._channel = _channel;
            this._remoteUriToKernel = new Map();
            this._uriToKernel = new Map();
            this._kernelToKernelInfo = new Map();
            this._uri = hostUri || "kernel://vscode";
            this._kernel.host = this;
            this._scheduler = new KernelScheduler();
        }
        tryGetKernelByRemoteUri(remoteUri) {
            return this._remoteUriToKernel.get(remoteUri);
        }
        trygetKernelByOriginUri(originUri) {
            return this._uriToKernel.get(originUri);
        }
        tryGetKernelInfo(kernel) {
            return this._kernelToKernelInfo.get(kernel);
        }
        addKernelInfo(kernel, kernelInfo) {
            kernelInfo.uri = `${this._uri}/${kernel.name}`;
            this._kernelToKernelInfo.set(kernel, kernelInfo);
            this._uriToKernel.set(kernelInfo.uri, kernel);
        }
        getKernel(kernelCommandEnvelope) {
            if (kernelCommandEnvelope.command.destinationUri) {
                let fromDestinationUri = this._uriToKernel.get(kernelCommandEnvelope.command.destinationUri.toLowerCase());
                if (fromDestinationUri) {
                    Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.command.destinationUri}`);
                    return fromDestinationUri;
                }
                fromDestinationUri = this._remoteUriToKernel.get(kernelCommandEnvelope.command.destinationUri.toLowerCase());
                if (fromDestinationUri) {
                    Logger.default.info(`Kernel ${fromDestinationUri.name} found for destination uri ${kernelCommandEnvelope.command.destinationUri}`);
                    return fromDestinationUri;
                }
            }
            if (kernelCommandEnvelope.command.originUri) {
                let fromOriginUri = this._uriToKernel.get(kernelCommandEnvelope.command.originUri.toLowerCase());
                if (fromOriginUri) {
                    Logger.default.info(`Kernel ${fromOriginUri.name} found for origin uri ${kernelCommandEnvelope.command.originUri}`);
                    return fromOriginUri;
                }
            }
            Logger.default.info(`Using Kernel ${this._kernel.name}`);
            return this._kernel;
        }
        registerRemoteUriForProxy(proxyLocalKernelName, remoteUri) {
            const kernel = this._kernel.findKernelByName(proxyLocalKernelName);
            if (!kernel) {
                throw new Error(`Kernel ${proxyLocalKernelName} is not a proxy kernel`);
            }
            const kernelinfo = this._kernelToKernelInfo.get(kernel);
            if (!kernelinfo) {
                throw new Error("kernelinfo not found");
            }
            if (kernelinfo === null || kernelinfo === void 0 ? void 0 : kernelinfo.remoteUri) {
                Logger.default.info(`Removing remote uri ${kernelinfo.remoteUri} for proxy kernel ${kernelinfo.localName}`);
                this._remoteUriToKernel.delete(kernelinfo.remoteUri.toLowerCase());
            }
            kernelinfo.remoteUri = remoteUri;
            if (kernel) {
                Logger.default.info(`Registering remote uri ${remoteUri} for proxy kernel ${kernelinfo.localName}`);
                this._remoteUriToKernel.set(remoteUri.toLowerCase(), kernel);
            }
        }
        createProxyKernelOnDefaultConnector(kernelInfo) {
            const proxyKernel = new ProxyKernel(kernelInfo.localName, this._channel);
            this._kernel.add(proxyKernel, kernelInfo.aliases);
            if (kernelInfo.remoteUri) {
                this.registerRemoteUriForProxy(proxyKernel.name, kernelInfo.remoteUri);
            }
            return proxyKernel;
        }
        connect() {
            this._channel.setCommandHandler((kernelCommandEnvelope) => {
                // fire and forget this one
                this._scheduler.runAsync(kernelCommandEnvelope, commandEnvelope => {
                    const kernel = this.getKernel(commandEnvelope);
                    return kernel.send(commandEnvelope);
                });
                return Promise.resolve();
            });
            this._kernel.subscribeToKernelEvents(e => {
                this._channel.publishKernelEvent(e);
            });
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function setup(global) {
        global = global || window;
        let compositeKernel = new CompositeKernel("browser");
        const jsKernel = new JavascriptKernel();
        const htmlKernel = new HtmlKernel();
        compositeKernel.add(jsKernel, ["js"]);
        compositeKernel.add(htmlKernel);
        compositeKernel.subscribeToKernelEvents(envelope => {
            global === null || global === void 0 ? void 0 : global.publishCommandOrEvent(envelope);
        });
        if (global) {
            global.sendKernelCommand = (kernelCommandEnvelope) => {
                compositeKernel.send(kernelCommandEnvelope);
            };
        }
    }

    exports.AddPackageType = AddPackageType;
    exports.AssemblyProducedType = AssemblyProducedType;
    exports.CancelType = CancelType;
    exports.ChangeWorkingDirectoryType = ChangeWorkingDirectoryType;
    exports.CodeSubmissionReceivedType = CodeSubmissionReceivedType;
    exports.CommandAndEventReceiver = CommandAndEventReceiver;
    exports.CommandCancelledType = CommandCancelledType;
    exports.CommandFailedType = CommandFailedType;
    exports.CommandSucceededType = CommandSucceededType;
    exports.CompileProjectType = CompileProjectType;
    exports.CompleteCodeSubmissionReceivedType = CompleteCodeSubmissionReceivedType;
    exports.CompletionsProducedType = CompletionsProducedType;
    exports.CompositeKernel = CompositeKernel;
    exports.ConsoleCapture = ConsoleCapture;
    exports.DiagnosticLogEntryProducedType = DiagnosticLogEntryProducedType;
    exports.DiagnosticsProducedType = DiagnosticsProducedType;
    exports.DisplayErrorType = DisplayErrorType;
    exports.DisplayValueType = DisplayValueType;
    exports.DisplayedValueProducedType = DisplayedValueProducedType;
    exports.DisplayedValueUpdatedType = DisplayedValueUpdatedType;
    exports.DocumentOpenedType = DocumentOpenedType;
    exports.ErrorProducedType = ErrorProducedType;
    exports.GenericChannel = GenericChannel;
    exports.Guid = Guid;
    exports.HoverTextProducedType = HoverTextProducedType;
    exports.HtmlKernel = HtmlKernel;
    exports.IncompleteCodeSubmissionReceivedType = IncompleteCodeSubmissionReceivedType;
    exports.InputProducedType = InputProducedType;
    exports.JavascriptKernel = JavascriptKernel;
    exports.Kernel = Kernel;
    exports.KernelExtensionLoadedType = KernelExtensionLoadedType;
    exports.KernelHost = KernelHost;
    exports.KernelInfoProducedType = KernelInfoProducedType;
    exports.KernelInvocationContext = KernelInvocationContext;
    exports.KernelReadyType = KernelReadyType;
    exports.KernelScheduler = KernelScheduler;
    exports.Logger = Logger;
    exports.OpenDocumentType = OpenDocumentType;
    exports.OpenProjectType = OpenProjectType;
    exports.PackageAddedType = PackageAddedType;
    exports.ProjectOpenedType = ProjectOpenedType;
    exports.PromiseCompletionSource = PromiseCompletionSource;
    exports.ProxyKernel = ProxyKernel;
    exports.QuitType = QuitType;
    exports.RequestCompletionsType = RequestCompletionsType;
    exports.RequestDiagnosticsType = RequestDiagnosticsType;
    exports.RequestHoverTextType = RequestHoverTextType;
    exports.RequestInputType = RequestInputType;
    exports.RequestKernelInfoType = RequestKernelInfoType;
    exports.RequestSignatureHelpType = RequestSignatureHelpType;
    exports.RequestValueInfosType = RequestValueInfosType;
    exports.RequestValueType = RequestValueType;
    exports.ReturnValueProducedType = ReturnValueProducedType;
    exports.SendEditableCodeType = SendEditableCodeType;
    exports.SignatureHelpProducedType = SignatureHelpProducedType;
    exports.StandardErrorValueProducedType = StandardErrorValueProducedType;
    exports.StandardOutputValueProducedType = StandardOutputValueProducedType;
    exports.SubmitCodeType = SubmitCodeType;
    exports.TokenGenerator = TokenGenerator;
    exports.UpdateDisplayedValueType = UpdateDisplayedValueType;
    exports.ValueInfosProducedType = ValueInfosProducedType;
    exports.ValueProducedType = ValueProducedType;
    exports.WorkingDirectoryChangedType = WorkingDirectoryChangedType;
    exports.areCommandsTheSame = areCommandsTheSame;
    exports.domHtmlFragmentProcessor = domHtmlFragmentProcessor;
    exports.formatValue = formatValue;
    exports.isKernelCommandEnvelope = isKernelCommandEnvelope;
    exports.isKernelEventEnvelope = isKernelEventEnvelope;
    exports.isPromiseCompletionSource = isPromiseCompletionSource;
    exports.setup = setup;
    exports.submitCommandAndGetResult = submitCommandAndGetResult;

    Object.defineProperty(exports, '__esModule', { value: true });

}));
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiZG90bmV0LWludGVyYWN0aXZlLmpzIiwic291cmNlcyI6WyIuLi9zcmMvY29udHJhY3RzLnRzIiwiLi4vc3JjL3V0aWxpdGllcy50cyIsIi4uL3NyYy9sb2dnZXIudHMiLCIuLi9zcmMvZ2VuZXJpY0NoYW5uZWwudHMiLCIuLi9zcmMvdG9rZW5HZW5lcmF0b3IudHMiLCIuLi9zcmMva2VybmVsSW52b2NhdGlvbkNvbnRleHQudHMiLCIuLi9zcmMva2VybmVsU2NoZWR1bGVyLnRzIiwiLi4vc3JjL2tlcm5lbC50cyIsIi4uL3NyYy9jb21wb3NpdGVLZXJuZWwudHMiLCIuLi9zcmMvY29uc29sZUNhcHR1cmUudHMiLCIuLi9zcmMvaHRtbEtlcm5lbC50cyIsIi4uL3NyYy9qYXZhc2NyaXB0S2VybmVsLnRzIiwiLi4vc3JjL3Byb3h5S2VybmVsLnRzIiwiLi4vc3JjL2tlcm5lbEhvc3QudHMiLCIuLi9zcmMvc2V0dXAudHMiXSwic291cmNlc0NvbnRlbnQiOlsiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbi8vIEdlbmVyYXRlZCBUeXBlU2NyaXB0IGludGVyZmFjZXMgYW5kIHR5cGVzLlxyXG5cclxuLy8gLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIEtlcm5lbCBDb21tYW5kc1xyXG5cclxuZXhwb3J0IGNvbnN0IEFkZFBhY2thZ2VUeXBlID0gXCJBZGRQYWNrYWdlXCI7XHJcbmV4cG9ydCBjb25zdCBDYW5jZWxUeXBlID0gXCJDYW5jZWxcIjtcclxuZXhwb3J0IGNvbnN0IENoYW5nZVdvcmtpbmdEaXJlY3RvcnlUeXBlID0gXCJDaGFuZ2VXb3JraW5nRGlyZWN0b3J5XCI7XHJcbmV4cG9ydCBjb25zdCBDb21waWxlUHJvamVjdFR5cGUgPSBcIkNvbXBpbGVQcm9qZWN0XCI7XHJcbmV4cG9ydCBjb25zdCBEaXNwbGF5RXJyb3JUeXBlID0gXCJEaXNwbGF5RXJyb3JcIjtcclxuZXhwb3J0IGNvbnN0IERpc3BsYXlWYWx1ZVR5cGUgPSBcIkRpc3BsYXlWYWx1ZVwiO1xyXG5leHBvcnQgY29uc3QgT3BlbkRvY3VtZW50VHlwZSA9IFwiT3BlbkRvY3VtZW50XCI7XHJcbmV4cG9ydCBjb25zdCBPcGVuUHJvamVjdFR5cGUgPSBcIk9wZW5Qcm9qZWN0XCI7XHJcbmV4cG9ydCBjb25zdCBRdWl0VHlwZSA9IFwiUXVpdFwiO1xyXG5leHBvcnQgY29uc3QgUmVxdWVzdENvbXBsZXRpb25zVHlwZSA9IFwiUmVxdWVzdENvbXBsZXRpb25zXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0RGlhZ25vc3RpY3NUeXBlID0gXCJSZXF1ZXN0RGlhZ25vc3RpY3NcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RIb3ZlclRleHRUeXBlID0gXCJSZXF1ZXN0SG92ZXJUZXh0XCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0SW5wdXRUeXBlID0gXCJSZXF1ZXN0SW5wdXRcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RLZXJuZWxJbmZvVHlwZSA9IFwiUmVxdWVzdEtlcm5lbEluZm9cIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RTaWduYXR1cmVIZWxwVHlwZSA9IFwiUmVxdWVzdFNpZ25hdHVyZUhlbHBcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RWYWx1ZVR5cGUgPSBcIlJlcXVlc3RWYWx1ZVwiO1xyXG5leHBvcnQgY29uc3QgUmVxdWVzdFZhbHVlSW5mb3NUeXBlID0gXCJSZXF1ZXN0VmFsdWVJbmZvc1wiO1xyXG5leHBvcnQgY29uc3QgU2VuZEVkaXRhYmxlQ29kZVR5cGUgPSBcIlNlbmRFZGl0YWJsZUNvZGVcIjtcclxuZXhwb3J0IGNvbnN0IFN1Ym1pdENvZGVUeXBlID0gXCJTdWJtaXRDb2RlXCI7XHJcbmV4cG9ydCBjb25zdCBVcGRhdGVEaXNwbGF5ZWRWYWx1ZVR5cGUgPSBcIlVwZGF0ZURpc3BsYXllZFZhbHVlXCI7XHJcblxyXG5leHBvcnQgdHlwZSBLZXJuZWxDb21tYW5kVHlwZSA9XHJcbiAgICAgIHR5cGVvZiBBZGRQYWNrYWdlVHlwZVxyXG4gICAgfCB0eXBlb2YgQ2FuY2VsVHlwZVxyXG4gICAgfCB0eXBlb2YgQ2hhbmdlV29ya2luZ0RpcmVjdG9yeVR5cGVcclxuICAgIHwgdHlwZW9mIENvbXBpbGVQcm9qZWN0VHlwZVxyXG4gICAgfCB0eXBlb2YgRGlzcGxheUVycm9yVHlwZVxyXG4gICAgfCB0eXBlb2YgRGlzcGxheVZhbHVlVHlwZVxyXG4gICAgfCB0eXBlb2YgT3BlbkRvY3VtZW50VHlwZVxyXG4gICAgfCB0eXBlb2YgT3BlblByb2plY3RUeXBlXHJcbiAgICB8IHR5cGVvZiBRdWl0VHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdENvbXBsZXRpb25zVHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdERpYWdub3N0aWNzVHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdEhvdmVyVGV4dFR5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RJbnB1dFR5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RLZXJuZWxJbmZvVHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdFNpZ25hdHVyZUhlbHBUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0VmFsdWVUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0VmFsdWVJbmZvc1R5cGVcclxuICAgIHwgdHlwZW9mIFNlbmRFZGl0YWJsZUNvZGVUeXBlXHJcbiAgICB8IHR5cGVvZiBTdWJtaXRDb2RlVHlwZVxyXG4gICAgfCB0eXBlb2YgVXBkYXRlRGlzcGxheWVkVmFsdWVUeXBlO1xyXG5cclxuZXhwb3J0IGludGVyZmFjZSBBZGRQYWNrYWdlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBwYWNrYWdlUmVmZXJlbmNlOiBQYWNrYWdlUmVmZXJlbmNlO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmQge1xyXG4gICAgdGFyZ2V0S2VybmVsTmFtZT86IHN0cmluZztcclxuICAgIG9yaWdpblVyaT86IHN0cmluZztcclxuICAgIGRlc3RpbmF0aW9uVXJpPzogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENhbmNlbCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENoYW5nZVdvcmtpbmdEaXJlY3RvcnkgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIHdvcmtpbmdEaXJlY3Rvcnk6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb21waWxlUHJvamVjdCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlFcnJvciBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlWYWx1ZSBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgZm9ybWF0dGVkVmFsdWU6IEZvcm1hdHRlZFZhbHVlO1xyXG4gICAgdmFsdWVJZDogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE9wZW5Eb2N1bWVudCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcmVsYXRpdmVGaWxlUGF0aDogc3RyaW5nO1xyXG4gICAgcmVnaW9uTmFtZT86IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBPcGVuUHJvamVjdCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcHJvamVjdDogUHJvamVjdDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBRdWl0IGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdENvbXBsZXRpb25zIGV4dGVuZHMgTGFuZ3VhZ2VTZXJ2aWNlQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTGFuZ3VhZ2VTZXJ2aWNlQ29tbWFuZCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG4gICAgbGluZVBvc2l0aW9uOiBMaW5lUG9zaXRpb247XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdERpYWdub3N0aWNzIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdEhvdmVyVGV4dCBleHRlbmRzIExhbmd1YWdlU2VydmljZUNvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RJbnB1dCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcHJvbXB0OiBzdHJpbmc7XHJcbiAgICBpc1Bhc3N3b3JkOiBib29sZWFuO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RLZXJuZWxJbmZvIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdFNpZ25hdHVyZUhlbHAgZXh0ZW5kcyBMYW5ndWFnZVNlcnZpY2VDb21tYW5kIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBSZXF1ZXN0VmFsdWUgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIG5hbWU6IHN0cmluZztcclxuICAgIG1pbWVUeXBlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdFZhbHVlSW5mb3MgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTZW5kRWRpdGFibGVDb2RlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBsYW5ndWFnZTogc3RyaW5nO1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFN1Ym1pdENvZGUgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIGNvZGU6IHN0cmluZztcclxuICAgIHN1Ym1pc3Npb25UeXBlPzogU3VibWlzc2lvblR5cGU7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgVXBkYXRlRGlzcGxheWVkVmFsdWUgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIGZvcm1hdHRlZFZhbHVlOiBGb3JtYXR0ZWRWYWx1ZTtcclxuICAgIHZhbHVlSWQ6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlzcGxheUVsZW1lbnQgZXh0ZW5kcyBJbnRlcmFjdGl2ZURvY3VtZW50T3V0cHV0RWxlbWVudCB7XHJcbiAgICBkYXRhOiB7IFtrZXk6IHN0cmluZ106IGFueTsgfTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJbnRlcmFjdGl2ZURvY3VtZW50T3V0cHV0RWxlbWVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgVGV4dEVsZW1lbnQgZXh0ZW5kcyBJbnRlcmFjdGl2ZURvY3VtZW50T3V0cHV0RWxlbWVudCB7XHJcbiAgICB0ZXh0OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRXJyb3JFbGVtZW50IGV4dGVuZHMgSW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQge1xyXG4gICAgZXJyb3JOYW1lOiBzdHJpbmc7XHJcbiAgICBlcnJvclZhbHVlOiBzdHJpbmc7XHJcbiAgICBzdGFja1RyYWNlOiBBcnJheTxzdHJpbmc+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rUGFyc2VSZXF1ZXN0IGV4dGVuZHMgTm90ZWJvb2tQYXJzZU9yU2VyaWFsaXplUmVxdWVzdCB7XHJcbiAgICB0eXBlOiBSZXF1ZXN0VHlwZTtcclxuICAgIHJhd0RhdGE6IFVpbnQ4QXJyYXk7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tQYXJzZU9yU2VyaWFsaXplUmVxdWVzdCB7XHJcbiAgICB0eXBlOiBSZXF1ZXN0VHlwZTtcclxuICAgIGlkOiBzdHJpbmc7XHJcbiAgICBzZXJpYWxpemF0aW9uVHlwZTogRG9jdW1lbnRTZXJpYWxpemF0aW9uVHlwZTtcclxuICAgIGRlZmF1bHRMYW5ndWFnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rU2VyaWFsaXplUmVxdWVzdCBleHRlbmRzIE5vdGVib29rUGFyc2VPclNlcmlhbGl6ZVJlcXVlc3Qge1xyXG4gICAgdHlwZTogUmVxdWVzdFR5cGU7XHJcbiAgICBuZXdMaW5lOiBzdHJpbmc7XHJcbiAgICBkb2N1bWVudDogSW50ZXJhY3RpdmVEb2N1bWVudDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va1BhcnNlUmVzcG9uc2UgZXh0ZW5kcyBOb3RlYm9va1BhcnNlclNlcnZlclJlc3BvbnNlIHtcclxuICAgIGRvY3VtZW50OiBJbnRlcmFjdGl2ZURvY3VtZW50O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rUGFyc2VyU2VydmVyUmVzcG9uc2Uge1xyXG4gICAgaWQ6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va1NlcmlhbGl6ZVJlc3BvbnNlIGV4dGVuZHMgTm90ZWJvb2tQYXJzZXJTZXJ2ZXJSZXNwb25zZSB7XHJcbiAgICByYXdEYXRhOiBVaW50OEFycmF5O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rRXJyb3JSZXNwb25zZSBleHRlbmRzIE5vdGVib29rUGFyc2VyU2VydmVyUmVzcG9uc2Uge1xyXG4gICAgZXJyb3JNZXNzYWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbi8vIC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSBLZXJuZWwgZXZlbnRzXHJcblxyXG5leHBvcnQgY29uc3QgQXNzZW1ibHlQcm9kdWNlZFR5cGUgPSBcIkFzc2VtYmx5UHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IENvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlID0gXCJDb2RlU3VibWlzc2lvblJlY2VpdmVkXCI7XHJcbmV4cG9ydCBjb25zdCBDb21tYW5kQ2FuY2VsbGVkVHlwZSA9IFwiQ29tbWFuZENhbmNlbGxlZFwiO1xyXG5leHBvcnQgY29uc3QgQ29tbWFuZEZhaWxlZFR5cGUgPSBcIkNvbW1hbmRGYWlsZWRcIjtcclxuZXhwb3J0IGNvbnN0IENvbW1hbmRTdWNjZWVkZWRUeXBlID0gXCJDb21tYW5kU3VjY2VlZGVkXCI7XHJcbmV4cG9ydCBjb25zdCBDb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlID0gXCJDb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRcIjtcclxuZXhwb3J0IGNvbnN0IENvbXBsZXRpb25zUHJvZHVjZWRUeXBlID0gXCJDb21wbGV0aW9uc1Byb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBEaWFnbm9zdGljTG9nRW50cnlQcm9kdWNlZFR5cGUgPSBcIkRpYWdub3N0aWNMb2dFbnRyeVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBEaWFnbm9zdGljc1Byb2R1Y2VkVHlwZSA9IFwiRGlhZ25vc3RpY3NQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgRGlzcGxheWVkVmFsdWVQcm9kdWNlZFR5cGUgPSBcIkRpc3BsYXllZFZhbHVlUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IERpc3BsYXllZFZhbHVlVXBkYXRlZFR5cGUgPSBcIkRpc3BsYXllZFZhbHVlVXBkYXRlZFwiO1xyXG5leHBvcnQgY29uc3QgRG9jdW1lbnRPcGVuZWRUeXBlID0gXCJEb2N1bWVudE9wZW5lZFwiO1xyXG5leHBvcnQgY29uc3QgRXJyb3JQcm9kdWNlZFR5cGUgPSBcIkVycm9yUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IEhvdmVyVGV4dFByb2R1Y2VkVHlwZSA9IFwiSG92ZXJUZXh0UHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IEluY29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZSA9IFwiSW5jb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRcIjtcclxuZXhwb3J0IGNvbnN0IElucHV0UHJvZHVjZWRUeXBlID0gXCJJbnB1dFByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBLZXJuZWxFeHRlbnNpb25Mb2FkZWRUeXBlID0gXCJLZXJuZWxFeHRlbnNpb25Mb2FkZWRcIjtcclxuZXhwb3J0IGNvbnN0IEtlcm5lbEluZm9Qcm9kdWNlZFR5cGUgPSBcIktlcm5lbEluZm9Qcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgS2VybmVsUmVhZHlUeXBlID0gXCJLZXJuZWxSZWFkeVwiO1xyXG5leHBvcnQgY29uc3QgUGFja2FnZUFkZGVkVHlwZSA9IFwiUGFja2FnZUFkZGVkXCI7XHJcbmV4cG9ydCBjb25zdCBQcm9qZWN0T3BlbmVkVHlwZSA9IFwiUHJvamVjdE9wZW5lZFwiO1xyXG5leHBvcnQgY29uc3QgUmV0dXJuVmFsdWVQcm9kdWNlZFR5cGUgPSBcIlJldHVyblZhbHVlUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IFNpZ25hdHVyZUhlbHBQcm9kdWNlZFR5cGUgPSBcIlNpZ25hdHVyZUhlbHBQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgU3RhbmRhcmRFcnJvclZhbHVlUHJvZHVjZWRUeXBlID0gXCJTdGFuZGFyZEVycm9yVmFsdWVQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgU3RhbmRhcmRPdXRwdXRWYWx1ZVByb2R1Y2VkVHlwZSA9IFwiU3RhbmRhcmRPdXRwdXRWYWx1ZVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBWYWx1ZUluZm9zUHJvZHVjZWRUeXBlID0gXCJWYWx1ZUluZm9zUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IFZhbHVlUHJvZHVjZWRUeXBlID0gXCJWYWx1ZVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBXb3JraW5nRGlyZWN0b3J5Q2hhbmdlZFR5cGUgPSBcIldvcmtpbmdEaXJlY3RvcnlDaGFuZ2VkXCI7XHJcblxyXG5leHBvcnQgdHlwZSBLZXJuZWxFdmVudFR5cGUgPVxyXG4gICAgICB0eXBlb2YgQXNzZW1ibHlQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIENvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBDb21tYW5kQ2FuY2VsbGVkVHlwZVxyXG4gICAgfCB0eXBlb2YgQ29tbWFuZEZhaWxlZFR5cGVcclxuICAgIHwgdHlwZW9mIENvbW1hbmRTdWNjZWVkZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBDb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBDb21wbGV0aW9uc1Byb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgRGlhZ25vc3RpY0xvZ0VudHJ5UHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBEaWFnbm9zdGljc1Byb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgRGlzcGxheWVkVmFsdWVQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIERpc3BsYXllZFZhbHVlVXBkYXRlZFR5cGVcclxuICAgIHwgdHlwZW9mIERvY3VtZW50T3BlbmVkVHlwZVxyXG4gICAgfCB0eXBlb2YgRXJyb3JQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIEhvdmVyVGV4dFByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgSW5jb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBJbnB1dFByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgS2VybmVsRXh0ZW5zaW9uTG9hZGVkVHlwZVxyXG4gICAgfCB0eXBlb2YgS2VybmVsSW5mb1Byb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgS2VybmVsUmVhZHlUeXBlXHJcbiAgICB8IHR5cGVvZiBQYWNrYWdlQWRkZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBQcm9qZWN0T3BlbmVkVHlwZVxyXG4gICAgfCB0eXBlb2YgUmV0dXJuVmFsdWVQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIFNpZ25hdHVyZUhlbHBQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIFN0YW5kYXJkRXJyb3JWYWx1ZVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgU3RhbmRhcmRPdXRwdXRWYWx1ZVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgVmFsdWVJbmZvc1Byb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgVmFsdWVQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIFdvcmtpbmdEaXJlY3RvcnlDaGFuZ2VkVHlwZTtcclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQXNzZW1ibHlQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIGFzc2VtYmx5OiBCYXNlNjRFbmNvZGVkQXNzZW1ibHk7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29kZVN1Ym1pc3Npb25SZWNlaXZlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIGNvZGU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb21tYW5kQ2FuY2VsbGVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbW1hbmRGYWlsZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBtZXNzYWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tbWFuZFN1Y2NlZWRlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tcGxldGlvbnNQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIGxpbmVQb3NpdGlvblNwYW4/OiBMaW5lUG9zaXRpb25TcGFuO1xyXG4gICAgY29tcGxldGlvbnM6IEFycmF5PENvbXBsZXRpb25JdGVtPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaWFnbm9zdGljTG9nRW50cnlQcm9kdWNlZCBleHRlbmRzIERpYWdub3N0aWNFdmVudCB7XHJcbiAgICBtZXNzYWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlhZ25vc3RpY0V2ZW50IGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpYWdub3N0aWNzUHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBkaWFnbm9zdGljczogQXJyYXk8RGlhZ25vc3RpYz47XHJcbiAgICBmb3JtYXR0ZWREaWFnbm9zdGljczogQXJyYXk8Rm9ybWF0dGVkVmFsdWU+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXllZFZhbHVlUHJvZHVjZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlFdmVudCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIGZvcm1hdHRlZFZhbHVlczogQXJyYXk8Rm9ybWF0dGVkVmFsdWU+O1xyXG4gICAgdmFsdWVJZD86IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5ZWRWYWx1ZVVwZGF0ZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERvY3VtZW50T3BlbmVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgcmVsYXRpdmVGaWxlUGF0aDogc3RyaW5nO1xyXG4gICAgcmVnaW9uTmFtZT86IHN0cmluZztcclxuICAgIGNvbnRlbnQ6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBFcnJvclByb2R1Y2VkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxuICAgIG1lc3NhZ2U6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBIb3ZlclRleHRQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIGNvbnRlbnQ6IEFycmF5PEZvcm1hdHRlZFZhbHVlPjtcclxuICAgIGxpbmVQb3NpdGlvblNwYW4/OiBMaW5lUG9zaXRpb25TcGFuO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEluY29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIElucHV0UHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICB2YWx1ZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbEV4dGVuc2lvbkxvYWRlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxJbmZvUHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBrZXJuZWxJbmZvOiBLZXJuZWxJbmZvO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbFJlYWR5IGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFBhY2thZ2VBZGRlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHBhY2thZ2VSZWZlcmVuY2U6IFJlc29sdmVkUGFja2FnZVJlZmVyZW5jZTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQcm9qZWN0T3BlbmVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgcHJvamVjdEl0ZW1zOiBBcnJheTxQcm9qZWN0SXRlbT47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmV0dXJuVmFsdWVQcm9kdWNlZCBleHRlbmRzIERpc3BsYXlFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgU2lnbmF0dXJlSGVscFByb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgc2lnbmF0dXJlczogQXJyYXk8U2lnbmF0dXJlSW5mb3JtYXRpb24+O1xyXG4gICAgYWN0aXZlU2lnbmF0dXJlSW5kZXg6IG51bWJlcjtcclxuICAgIGFjdGl2ZVBhcmFtZXRlckluZGV4OiBudW1iZXI7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgU3RhbmRhcmRFcnJvclZhbHVlUHJvZHVjZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFN0YW5kYXJkT3V0cHV0VmFsdWVQcm9kdWNlZCBleHRlbmRzIERpc3BsYXlFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgVmFsdWVJbmZvc1Byb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgdmFsdWVJbmZvczogQXJyYXk8S2VybmVsVmFsdWVJbmZvPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBWYWx1ZVByb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgbmFtZTogc3RyaW5nO1xyXG4gICAgZm9ybWF0dGVkVmFsdWU6IEZvcm1hdHRlZFZhbHVlO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFdvcmtpbmdEaXJlY3RvcnlDaGFuZ2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgd29ya2luZ0RpcmVjdG9yeTogc3RyaW5nO1xyXG59XHJcblxyXG4vLyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gUmVxdWlyZWQgVHlwZXNcclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQmFzZTY0RW5jb2RlZEFzc2VtYmx5IHtcclxuICAgIHZhbHVlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tcGxldGlvbkl0ZW0ge1xyXG4gICAgZGlzcGxheVRleHQ6IHN0cmluZztcclxuICAgIGtpbmQ6IHN0cmluZztcclxuICAgIGZpbHRlclRleHQ6IHN0cmluZztcclxuICAgIHNvcnRUZXh0OiBzdHJpbmc7XHJcbiAgICBpbnNlcnRUZXh0OiBzdHJpbmc7XHJcbiAgICBpbnNlcnRUZXh0Rm9ybWF0PzogSW5zZXJ0VGV4dEZvcm1hdDtcclxuICAgIGRvY3VtZW50YXRpb246IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGVudW0gSW5zZXJ0VGV4dEZvcm1hdCB7XHJcbiAgICBQbGFpblRleHQgPSBcInBsYWludGV4dFwiLFxyXG4gICAgU25pcHBldCA9IFwic25pcHBldFwiLFxyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpYWdub3N0aWMge1xyXG4gICAgbGluZVBvc2l0aW9uU3BhbjogTGluZVBvc2l0aW9uU3BhbjtcclxuICAgIHNldmVyaXR5OiBEaWFnbm9zdGljU2V2ZXJpdHk7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbiAgICBtZXNzYWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBlbnVtIERpYWdub3N0aWNTZXZlcml0eSB7XHJcbiAgICBIaWRkZW4gPSBcImhpZGRlblwiLFxyXG4gICAgSW5mbyA9IFwiaW5mb1wiLFxyXG4gICAgV2FybmluZyA9IFwid2FybmluZ1wiLFxyXG4gICAgRXJyb3IgPSBcImVycm9yXCIsXHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTGluZVBvc2l0aW9uU3BhbiB7XHJcbiAgICBzdGFydDogTGluZVBvc2l0aW9uO1xyXG4gICAgZW5kOiBMaW5lUG9zaXRpb247XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTGluZVBvc2l0aW9uIHtcclxuICAgIGxpbmU6IG51bWJlcjtcclxuICAgIGNoYXJhY3RlcjogbnVtYmVyO1xyXG59XHJcblxyXG5leHBvcnQgZW51bSBEb2N1bWVudFNlcmlhbGl6YXRpb25UeXBlIHtcclxuICAgIERpYiA9IFwiZGliXCIsXHJcbiAgICBJcHluYiA9IFwiaXB5bmJcIixcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBGb3JtYXR0ZWRWYWx1ZSB7XHJcbiAgICBtaW1lVHlwZTogc3RyaW5nO1xyXG4gICAgdmFsdWU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJbnRlcmFjdGl2ZURvY3VtZW50IHtcclxuICAgIGVsZW1lbnRzOiBBcnJheTxJbnRlcmFjdGl2ZURvY3VtZW50RWxlbWVudD47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSW50ZXJhY3RpdmVEb2N1bWVudEVsZW1lbnQge1xyXG4gICAgbGFuZ3VhZ2U6IHN0cmluZztcclxuICAgIGNvbnRlbnRzOiBzdHJpbmc7XHJcbiAgICBvdXRwdXRzOiBBcnJheTxJbnRlcmFjdGl2ZURvY3VtZW50T3V0cHV0RWxlbWVudD47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsSW5mbyB7XHJcbiAgICBhbGlhc2VzOiBBcnJheTxzdHJpbmc+O1xyXG4gICAgbGFuZ3VhZ2VOYW1lPzogc3RyaW5nO1xyXG4gICAgbGFuZ3VhZ2VWZXJzaW9uPzogc3RyaW5nO1xyXG4gICAgbG9jYWxOYW1lOiBzdHJpbmc7XHJcbiAgICB1cmk/OiBzdHJpbmc7XHJcbiAgICByZW1vdGVVcmk/OiBzdHJpbmc7XHJcbiAgICBzdXBwb3J0ZWRLZXJuZWxDb21tYW5kczogQXJyYXk8S2VybmVsQ29tbWFuZEluZm8+O1xyXG4gICAgc3VwcG9ydGVkRGlyZWN0aXZlczogQXJyYXk8S2VybmVsRGlyZWN0aXZlSW5mbz47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsQ29tbWFuZEluZm8ge1xyXG4gICAgbmFtZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbERpcmVjdGl2ZUluZm8ge1xyXG4gICAgbmFtZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbFZhbHVlSW5mbyB7XHJcbiAgICBuYW1lOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUGFja2FnZVJlZmVyZW5jZSB7XHJcbiAgICBwYWNrYWdlTmFtZTogc3RyaW5nO1xyXG4gICAgcGFja2FnZVZlcnNpb246IHN0cmluZztcclxuICAgIGlzUGFja2FnZVZlcnNpb25TcGVjaWZpZWQ6IGJvb2xlYW47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUHJvamVjdCB7XHJcbiAgICBmaWxlczogQXJyYXk8UHJvamVjdEZpbGU+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFByb2plY3RGaWxlIHtcclxuICAgIHJlbGF0aXZlRmlsZVBhdGg6IHN0cmluZztcclxuICAgIGNvbnRlbnQ6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQcm9qZWN0SXRlbSB7XHJcbiAgICByZWxhdGl2ZUZpbGVQYXRoOiBzdHJpbmc7XHJcbiAgICByZWdpb25OYW1lczogQXJyYXk8c3RyaW5nPjtcclxuICAgIHJlZ2lvbnNDb250ZW50OiB7IFtrZXk6IHN0cmluZ106IHN0cmluZzsgfTtcclxufVxyXG5cclxuZXhwb3J0IGVudW0gUmVxdWVzdFR5cGUge1xyXG4gICAgUGFyc2UgPSBcInBhcnNlXCIsXHJcbiAgICBTZXJpYWxpemUgPSBcInNlcmlhbGl6ZVwiLFxyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlc29sdmVkUGFja2FnZVJlZmVyZW5jZSBleHRlbmRzIFBhY2thZ2VSZWZlcmVuY2Uge1xyXG4gICAgYXNzZW1ibHlQYXRoczogQXJyYXk8c3RyaW5nPjtcclxuICAgIHByb2JpbmdQYXRoczogQXJyYXk8c3RyaW5nPjtcclxuICAgIHBhY2thZ2VSb290OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgU2lnbmF0dXJlSW5mb3JtYXRpb24ge1xyXG4gICAgbGFiZWw6IHN0cmluZztcclxuICAgIGRvY3VtZW50YXRpb246IEZvcm1hdHRlZFZhbHVlO1xyXG4gICAgcGFyYW1ldGVyczogQXJyYXk8UGFyYW1ldGVySW5mb3JtYXRpb24+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFBhcmFtZXRlckluZm9ybWF0aW9uIHtcclxuICAgIGxhYmVsOiBzdHJpbmc7XHJcbiAgICBkb2N1bWVudGF0aW9uOiBGb3JtYXR0ZWRWYWx1ZTtcclxufVxyXG5cclxuZXhwb3J0IGVudW0gU3VibWlzc2lvblR5cGUge1xyXG4gICAgUnVuID0gXCJydW5cIixcclxuICAgIERpYWdub3NlID0gXCJkaWFnbm9zZVwiLFxyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbEV2ZW50RW52ZWxvcGUge1xyXG4gICAgZXZlbnRUeXBlOiBLZXJuZWxFdmVudFR5cGU7XHJcbiAgICBldmVudDogS2VybmVsRXZlbnQ7XHJcbiAgICBjb21tYW5kPzogS2VybmVsQ29tbWFuZEVudmVsb3BlO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmRFbnZlbG9wZSB7XHJcbiAgICB0b2tlbj86IHN0cmluZztcclxuICAgIGlkPzogc3RyaW5nO1xyXG4gICAgY29tbWFuZFR5cGU6IEtlcm5lbENvbW1hbmRUeXBlO1xyXG4gICAgY29tbWFuZDogS2VybmVsQ29tbWFuZDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxFdmVudEVudmVsb3BlT2JzZXJ2ZXIge1xyXG4gICAgKGV2ZW50RW52ZWxvcGU6IEtlcm5lbEV2ZW50RW52ZWxvcGUpOiB2b2lkO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmRFbnZlbG9wZUhhbmRsZXIge1xyXG4gICAgKGV2ZW50RW52ZWxvcGU6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlzcG9zYWJsZSB7XHJcbiAgICBkaXNwb3NlKCk6IHZvaWQ7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlzcG9zYWJsZVN1YnNjcmlwdGlvbiBleHRlbmRzIERpc3Bvc2FibGUge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmRBbmRFdmVudFNlbmRlciB7XHJcbiAgICBzdWJtaXRDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogS2VybmVsQ29tbWFuZEVudmVsb3BlKTogUHJvbWlzZTx2b2lkPjtcclxuICAgIHB1Ymxpc2hLZXJuZWxFdmVudChldmVudEVudmVsb3BlOiBLZXJuZWxFdmVudEVudmVsb3BlKTogUHJvbWlzZTx2b2lkPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciB7XHJcbiAgICBzdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhvYnNlcnZlcjogS2VybmVsRXZlbnRFbnZlbG9wZU9ic2VydmVyKTogRGlzcG9zYWJsZVN1YnNjcmlwdGlvbjtcclxuICAgIHNldENvbW1hbmRIYW5kbGVyKGhhbmRsZXI6IEtlcm5lbENvbW1hbmRFbnZlbG9wZUhhbmRsZXIpOiB2b2lkO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmRBbmRFdmVudENoYW5uZWwgZXh0ZW5kcyBLZXJuZWxDb21tYW5kQW5kRXZlbnRTZW5kZXIsIEtlcm5lbENvbW1hbmRBbmRFdmVudFJlY2VpdmVyLCBEaXNwb3NhYmxlIHtcclxufVxyXG5cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tICcuL2NvbnRyYWN0cyc7XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gaXNLZXJuZWxFdmVudEVudmVsb3BlKG9iajogYW55KTogb2JqIGlzIGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlIHtcclxuICAgIHJldHVybiBvYmouZXZlbnRUeXBlXHJcbiAgICAgICAgJiYgb2JqLmV2ZW50O1xyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gaXNLZXJuZWxDb21tYW5kRW52ZWxvcGUob2JqOiBhbnkpOiBvYmogaXMgY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB7XHJcbiAgICByZXR1cm4gb2JqLmNvbW1hbmRUeXBlXHJcbiAgICAgICAgJiYgb2JqLmNvbW1hbmQ7XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmV4cG9ydCBlbnVtIExvZ0xldmVsIHtcclxuICAgIEluZm8gPSAwLFxyXG4gICAgV2FybiA9IDEsXHJcbiAgICBFcnJvciA9IDIsXHJcbiAgICBOb25lID0gMyxcclxufVxyXG5cclxuZXhwb3J0IHR5cGUgTG9nRW50cnkgPSB7XHJcbiAgICBsb2dMZXZlbDogTG9nTGV2ZWw7XHJcbiAgICBzb3VyY2U6IHN0cmluZztcclxuICAgIG1lc3NhZ2U6IHN0cmluZztcclxufTtcclxuXHJcbmV4cG9ydCBjbGFzcyBMb2dnZXIge1xyXG5cclxuICAgIHByaXZhdGUgc3RhdGljIF9kZWZhdWx0OiBMb2dnZXIgPSBuZXcgTG9nZ2VyKCdkZWZhdWx0JywgKF9lbnRyeTogTG9nRW50cnkpID0+IHsgfSk7XHJcblxyXG4gICAgcHJpdmF0ZSBjb25zdHJ1Y3Rvcihwcml2YXRlIHJlYWRvbmx5IHNvdXJjZTogc3RyaW5nLCByZWFkb25seSB3cml0ZTogKGVudHJ5OiBMb2dFbnRyeSkgPT4gdm9pZCkge1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBpbmZvKG1lc3NhZ2U6IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMud3JpdGUoeyBsb2dMZXZlbDogTG9nTGV2ZWwuSW5mbywgc291cmNlOiB0aGlzLnNvdXJjZSwgbWVzc2FnZSB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgd2FybihtZXNzYWdlOiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLndyaXRlKHsgbG9nTGV2ZWw6IExvZ0xldmVsLldhcm4sIHNvdXJjZTogdGhpcy5zb3VyY2UsIG1lc3NhZ2UgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGVycm9yKG1lc3NhZ2U6IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMud3JpdGUoeyBsb2dMZXZlbDogTG9nTGV2ZWwuRXJyb3IsIHNvdXJjZTogdGhpcy5zb3VyY2UsIG1lc3NhZ2UgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBjb25maWd1cmUoc291cmNlOiBzdHJpbmcsIHdyaXRlcjogKGVudHJ5OiBMb2dFbnRyeSkgPT4gdm9pZCkge1xyXG4gICAgICAgIGNvbnN0IGxvZ2dlciA9IG5ldyBMb2dnZXIoc291cmNlLCB3cml0ZXIpO1xyXG4gICAgICAgIExvZ2dlci5fZGVmYXVsdCA9IGxvZ2dlcjtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIGdldCBkZWZhdWx0KCk6IExvZ2dlciB7XHJcbiAgICAgICAgaWYgKExvZ2dlci5fZGVmYXVsdCkge1xyXG4gICAgICAgICAgICByZXR1cm4gTG9nZ2VyLl9kZWZhdWx0O1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgdGhyb3cgbmV3IEVycm9yKCdObyBsb2dnZXIgaGFzIGJlZW4gY29uZmlndXJlZCBmb3IgdGhpcyBjb250ZXh0Jyk7XHJcbiAgICB9XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0ICogYXMgdXRpbGl0aWVzIGZyb20gXCIuL3V0aWxpdGllc1wiO1xyXG5pbXBvcnQgeyBMb2dnZXIgfSBmcm9tIFwiLi9sb2dnZXJcIjtcclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBpc1Byb21pc2VDb21wbGV0aW9uU291cmNlPFQ+KG9iajogYW55KTogb2JqIGlzIFByb21pc2VDb21wbGV0aW9uU291cmNlPFQ+IHtcclxuICAgIHJldHVybiBvYmoucHJvbWlzZVxyXG4gICAgICAgICYmIG9iai5yZXNvbHZlXHJcbiAgICAgICAgJiYgb2JqLnJlamVjdDtcclxufVxyXG5cclxuZXhwb3J0IGNsYXNzIFByb21pc2VDb21wbGV0aW9uU291cmNlPFQ+IHtcclxuICAgIHByaXZhdGUgX3Jlc29sdmU6ICh2YWx1ZTogVCkgPT4gdm9pZCA9ICgpID0+IHsgfTtcclxuICAgIHByaXZhdGUgX3JlamVjdDogKHJlYXNvbjogYW55KSA9PiB2b2lkID0gKCkgPT4geyB9O1xyXG4gICAgcmVhZG9ubHkgcHJvbWlzZTogUHJvbWlzZTxUPjtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcigpIHtcclxuICAgICAgICB0aGlzLnByb21pc2UgPSBuZXcgUHJvbWlzZTxUPigocmVzb2x2ZSwgcmVqZWN0KSA9PiB7XHJcbiAgICAgICAgICAgIHRoaXMuX3Jlc29sdmUgPSByZXNvbHZlO1xyXG4gICAgICAgICAgICB0aGlzLl9yZWplY3QgPSByZWplY3Q7XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgcmVzb2x2ZSh2YWx1ZTogVCkge1xyXG4gICAgICAgIHRoaXMuX3Jlc29sdmUodmFsdWUpO1xyXG4gICAgfVxyXG5cclxuICAgIHJlamVjdChyZWFzb246IGFueSkge1xyXG4gICAgICAgIHRoaXMuX3JlamVjdChyZWFzb24pO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgY2xhc3MgR2VuZXJpY0NoYW5uZWwgaW1wbGVtZW50cyBjb250cmFjdHMuS2VybmVsQ29tbWFuZEFuZEV2ZW50Q2hhbm5lbCB7XHJcblxyXG4gICAgcHJpdmF0ZSBzdGlsbFJ1bm5pbmc6IFByb21pc2VDb21wbGV0aW9uU291cmNlPG51bWJlcj47XHJcbiAgICBwcml2YXRlIGNvbW1hbmRIYW5kbGVyOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlSGFuZGxlciA9ICgpID0+IFByb21pc2UucmVzb2x2ZSgpO1xyXG4gICAgcHJpdmF0ZSBldmVudFN1YnNjcmliZXJzOiBBcnJheTxjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZU9ic2VydmVyPiA9IFtdO1xyXG5cclxuICAgIGNvbnN0cnVjdG9yKHByaXZhdGUgcmVhZG9ubHkgbWVzc2FnZVNlbmRlcjogKG1lc3NhZ2U6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSkgPT4gUHJvbWlzZTx2b2lkPiwgcHJpdmF0ZSByZWFkb25seSBtZXNzYWdlUmVjZWl2ZXI6ICgpID0+IFByb21pc2U8Y29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB8IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPikge1xyXG5cclxuICAgICAgICB0aGlzLnN0aWxsUnVubmluZyA9IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxudW1iZXI+KCk7XHJcbiAgICB9XHJcblxyXG4gICAgZGlzcG9zZSgpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLnN0b3AoKTtcclxuICAgIH1cclxuXHJcbiAgICBhc3luYyBydW4oKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgd2hpbGUgKHRydWUpIHtcclxuICAgICAgICAgICAgbGV0IG1lc3NhZ2UgPSBhd2FpdCBQcm9taXNlLnJhY2UoW3RoaXMubWVzc2FnZVJlY2VpdmVyKCksIHRoaXMuc3RpbGxSdW5uaW5nLnByb21pc2VdKTtcclxuICAgICAgICAgICAgaWYgKHR5cGVvZiBtZXNzYWdlID09PSAnbnVtYmVyJykge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIGlmICh1dGlsaXRpZXMuaXNLZXJuZWxDb21tYW5kRW52ZWxvcGUobWVzc2FnZSkpIHtcclxuICAgICAgICAgICAgICAgIHRoaXMuY29tbWFuZEhhbmRsZXIobWVzc2FnZSk7XHJcbiAgICAgICAgICAgIH0gZWxzZSBpZiAodXRpbGl0aWVzLmlzS2VybmVsRXZlbnRFbnZlbG9wZShtZXNzYWdlKSkge1xyXG4gICAgICAgICAgICAgICAgZm9yIChsZXQgaSA9IHRoaXMuZXZlbnRTdWJzY3JpYmVycy5sZW5ndGggLSAxOyBpID49IDA7IGktLSkge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuZXZlbnRTdWJzY3JpYmVyc1tpXShtZXNzYWdlKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBzdG9wKCkge1xyXG4gICAgICAgIHRoaXMuc3RpbGxSdW5uaW5nLnJlc29sdmUoLTEpO1xyXG4gICAgfVxyXG5cclxuXHJcbiAgICBzdWJtaXRDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLm1lc3NhZ2VTZW5kZXIoY29tbWFuZEVudmVsb3BlKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaXNoS2VybmVsRXZlbnQoZXZlbnRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICByZXR1cm4gdGhpcy5tZXNzYWdlU2VuZGVyKGV2ZW50RW52ZWxvcGUpO1xyXG4gICAgfVxyXG5cclxuICAgIHN1YnNjcmliZVRvS2VybmVsRXZlbnRzKG9ic2VydmVyOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZU9ic2VydmVyKTogY29udHJhY3RzLkRpc3Bvc2FibGVTdWJzY3JpcHRpb24ge1xyXG4gICAgICAgIHRoaXMuZXZlbnRTdWJzY3JpYmVycy5wdXNoKG9ic2VydmVyKTtcclxuICAgICAgICByZXR1cm4ge1xyXG4gICAgICAgICAgICBkaXNwb3NlOiAoKSA9PiB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBpID0gdGhpcy5ldmVudFN1YnNjcmliZXJzLmluZGV4T2Yob2JzZXJ2ZXIpO1xyXG4gICAgICAgICAgICAgICAgaWYgKGkgPj0gMCkge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuZXZlbnRTdWJzY3JpYmVycy5zcGxpY2UoaSwgMSk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9O1xyXG4gICAgfVxyXG5cclxuICAgIHNldENvbW1hbmRIYW5kbGVyKGhhbmRsZXI6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGVIYW5kbGVyKSB7XHJcbiAgICAgICAgdGhpcy5jb21tYW5kSGFuZGxlciA9IGhhbmRsZXI7XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBjbGFzcyBDb21tYW5kQW5kRXZlbnRSZWNlaXZlciB7XHJcbiAgICBwcml2YXRlIF93YWl0aW5nT25NZXNzYWdlczogUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U8Y29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB8IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPiB8IG51bGwgPSBudWxsO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfZW52ZWxvcGVRdWV1ZTogKGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSlbXSA9IFtdO1xyXG5cclxuICAgIHB1YmxpYyBkZWxlZ2F0ZShjb21tYW5kT3JFdmVudDogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB8IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlKSB7XHJcbiAgICAgICAgaWYgKHRoaXMuX3dhaXRpbmdPbk1lc3NhZ2VzKSB7XHJcbiAgICAgICAgICAgIGxldCBjYXB0dXJlZE1lc3NhZ2VXYWl0ZXIgPSB0aGlzLl93YWl0aW5nT25NZXNzYWdlcztcclxuICAgICAgICAgICAgdGhpcy5fd2FpdGluZ09uTWVzc2FnZXMgPSBudWxsO1xyXG5cclxuICAgICAgICAgICAgY2FwdHVyZWRNZXNzYWdlV2FpdGVyLnJlc29sdmUoY29tbWFuZE9yRXZlbnQpO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcblxyXG4gICAgICAgICAgICB0aGlzLl9lbnZlbG9wZVF1ZXVlLnB1c2goY29tbWFuZE9yRXZlbnQpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgcmVhZCgpOiBQcm9taXNlPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4ge1xyXG4gICAgICAgIGxldCBlbnZlbG9wZSA9IHRoaXMuX2VudmVsb3BlUXVldWUuc2hpZnQoKTtcclxuICAgICAgICBpZiAoZW52ZWxvcGUpIHtcclxuICAgICAgICAgICAgcmV0dXJuIFByb21pc2UucmVzb2x2ZTxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlIHwgY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGU+KGVudmVsb3BlKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgZWxzZSB7XHJcbiAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYGNoYW5uZWwgYnVpbGRpbmcgcHJvbWlzZSBhd2FpdGVyYCk7XHJcbiAgICAgICAgICAgIHRoaXMuX3dhaXRpbmdPbk1lc3NhZ2VzID0gbmV3IFByb21pc2VDb21wbGV0aW9uU291cmNlPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4oKTtcclxuICAgICAgICAgICAgcmV0dXJuIHRoaXMuX3dhaXRpbmdPbk1lc3NhZ2VzLnByb21pc2U7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgeyBLZXJuZWxDb21tYW5kRW52ZWxvcGUgfSBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuXHJcbmV4cG9ydCBjbGFzcyBHdWlkIHtcclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIHZhbGlkYXRvciA9IG5ldyBSZWdFeHAoXCJeW2EtejAtOV17OH0tW2EtejAtOV17NH0tW2EtejAtOV17NH0tW2EtejAtOV17NH0tW2EtejAtOV17MTJ9JFwiLCBcImlcIik7XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBFTVBUWSA9IFwiMDAwMDAwMDAtMDAwMC0wMDAwLTAwMDAtMDAwMDAwMDAwMDAwXCI7XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBpc0d1aWQoZ3VpZDogYW55KSB7XHJcbiAgICAgICAgY29uc3QgdmFsdWU6IHN0cmluZyA9IGd1aWQudG9TdHJpbmcoKTtcclxuICAgICAgICByZXR1cm4gZ3VpZCAmJiAoZ3VpZCBpbnN0YW5jZW9mIEd1aWQgfHwgR3VpZC52YWxpZGF0b3IudGVzdCh2YWx1ZSkpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgY3JlYXRlKCk6IEd1aWQge1xyXG4gICAgICAgIHJldHVybiBuZXcgR3VpZChbR3VpZC5nZW4oMiksIEd1aWQuZ2VuKDEpLCBHdWlkLmdlbigxKSwgR3VpZC5nZW4oMSksIEd1aWQuZ2VuKDMpXS5qb2luKFwiLVwiKSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBjcmVhdGVFbXB0eSgpOiBHdWlkIHtcclxuICAgICAgICByZXR1cm4gbmV3IEd1aWQoXCJlbXB0eWd1aWRcIik7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBwYXJzZShndWlkOiBzdHJpbmcpOiBHdWlkIHtcclxuICAgICAgICByZXR1cm4gbmV3IEd1aWQoZ3VpZCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyByYXcoKTogc3RyaW5nIHtcclxuICAgICAgICByZXR1cm4gW0d1aWQuZ2VuKDIpLCBHdWlkLmdlbigxKSwgR3VpZC5nZW4oMSksIEd1aWQuZ2VuKDEpLCBHdWlkLmdlbigzKV0uam9pbihcIi1cIik7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBzdGF0aWMgZ2VuKGNvdW50OiBudW1iZXIpIHtcclxuICAgICAgICBsZXQgb3V0OiBzdHJpbmcgPSBcIlwiO1xyXG4gICAgICAgIGZvciAobGV0IGk6IG51bWJlciA9IDA7IGkgPCBjb3VudDsgaSsrKSB7XHJcbiAgICAgICAgICAgIC8vIHRzbGludDpkaXNhYmxlLW5leHQtbGluZTpuby1iaXR3aXNlXHJcbiAgICAgICAgICAgIG91dCArPSAoKCgxICsgTWF0aC5yYW5kb20oKSkgKiAweDEwMDAwKSB8IDApLnRvU3RyaW5nKDE2KS5zdWJzdHJpbmcoMSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJldHVybiBvdXQ7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSB2YWx1ZTogc3RyaW5nO1xyXG5cclxuICAgIHByaXZhdGUgY29uc3RydWN0b3IoZ3VpZDogc3RyaW5nKSB7XHJcbiAgICAgICAgaWYgKCFndWlkKSB7IHRocm93IG5ldyBUeXBlRXJyb3IoXCJJbnZhbGlkIGFyZ3VtZW50OyBgdmFsdWVgIGhhcyBubyB2YWx1ZS5cIik7IH1cclxuXHJcbiAgICAgICAgdGhpcy52YWx1ZSA9IEd1aWQuRU1QVFk7XHJcblxyXG4gICAgICAgIGlmIChndWlkICYmIEd1aWQuaXNHdWlkKGd1aWQpKSB7XHJcbiAgICAgICAgICAgIHRoaXMudmFsdWUgPSBndWlkO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZXF1YWxzKG90aGVyOiBHdWlkKTogYm9vbGVhbiB7XHJcbiAgICAgICAgLy8gQ29tcGFyaW5nIHN0cmluZyBgdmFsdWVgIGFnYWluc3QgcHJvdmlkZWQgYGd1aWRgIHdpbGwgYXV0by1jYWxsXHJcbiAgICAgICAgLy8gdG9TdHJpbmcgb24gYGd1aWRgIGZvciBjb21wYXJpc29uXHJcbiAgICAgICAgcmV0dXJuIEd1aWQuaXNHdWlkKG90aGVyKSAmJiB0aGlzLnZhbHVlID09PSBvdGhlci50b1N0cmluZygpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBpc0VtcHR5KCk6IGJvb2xlYW4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLnZhbHVlID09PSBHdWlkLkVNUFRZO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0b1N0cmluZygpOiBzdHJpbmcge1xyXG4gICAgICAgIHJldHVybiB0aGlzLnZhbHVlO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0b0pTT04oKTogYW55IHtcclxuICAgICAgICByZXR1cm4ge1xyXG4gICAgICAgICAgICB2YWx1ZTogdGhpcy52YWx1ZSxcclxuICAgICAgICB9O1xyXG4gICAgfVxyXG59XHJcblxyXG5mdW5jdGlvbiBzZXRUb2tlbihjb21tYW5kRW52ZWxvcGU6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSkge1xyXG4gICAgaWYgKCFjb21tYW5kRW52ZWxvcGUudG9rZW4pIHtcclxuICAgICAgICBjb21tYW5kRW52ZWxvcGUudG9rZW4gPSBHdWlkLmNyZWF0ZSgpLnRvU3RyaW5nKCk7XHJcbiAgICB9XHJcblxyXG4gICAgLy9cclxufVxyXG5cclxuZXhwb3J0IGNsYXNzIFRva2VuR2VuZXJhdG9yIHtcclxuICAgIHByaXZhdGUgX3NlZWQ6IHN0cmluZztcclxuICAgIHByaXZhdGUgX2NvdW50ZXI6IG51bWJlcjtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcigpIHtcclxuICAgICAgICB0aGlzLl9zZWVkID0gR3VpZC5jcmVhdGUoKS50b1N0cmluZygpO1xyXG4gICAgICAgIHRoaXMuX2NvdW50ZXIgPSAwO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBHZXROZXdUb2tlbigpOiBzdHJpbmcge1xyXG4gICAgICAgIHRoaXMuX2NvdW50ZXIrKztcclxuICAgICAgICByZXR1cm4gYCR7dGhpcy5fc2VlZH06OiR7dGhpcy5fY291bnRlcn1gO1xyXG4gICAgfVxyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgeyBDb21tYW5kU3VjY2VlZGVkLCBDb21tYW5kU3VjY2VlZGVkVHlwZSwgQ29tbWFuZEZhaWxlZCwgQ29tbWFuZEZhaWxlZFR5cGUsIEtlcm5lbENvbW1hbmRFbnZlbG9wZSwgS2VybmVsQ29tbWFuZCwgS2VybmVsRXZlbnRFbnZlbG9wZSwgRGlzcG9zYWJsZSB9IGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBQcm9taXNlQ29tcGxldGlvblNvdXJjZSB9IGZyb20gXCIuL2dlbmVyaWNDaGFubmVsXCI7XHJcbmltcG9ydCB7IElLZXJuZWxFdmVudE9ic2VydmVyLCBLZXJuZWwgfSBmcm9tIFwiLi9rZXJuZWxcIjtcclxuaW1wb3J0IHsgVG9rZW5HZW5lcmF0b3IgfSBmcm9tIFwiLi90b2tlbkdlbmVyYXRvclwiO1xyXG5cclxuXHJcbmV4cG9ydCBjbGFzcyBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCBpbXBsZW1lbnRzIERpc3Bvc2FibGUge1xyXG4gICAgcHVibGljIGdldCBwcm9taXNlKCk6IHZvaWQgfCBQcm9taXNlTGlrZTx2b2lkPiB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuY29tcGxldGlvblNvdXJjZS5wcm9taXNlO1xyXG4gICAgfVxyXG4gICAgcHJpdmF0ZSBzdGF0aWMgX2N1cnJlbnQ6IEtlcm5lbEludm9jYXRpb25Db250ZXh0IHwgbnVsbCA9IG51bGw7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9jb21tYW5kRW52ZWxvcGU6IEtlcm5lbENvbW1hbmRFbnZlbG9wZTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX2NoaWxkQ29tbWFuZHM6IEtlcm5lbENvbW1hbmRFbnZlbG9wZVtdID0gW107XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF90b2tlbkdlbmVyYXRvcjogVG9rZW5HZW5lcmF0b3IgPSBuZXcgVG9rZW5HZW5lcmF0b3IoKTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX2V2ZW50T2JzZXJ2ZXJzOiBNYXA8c3RyaW5nLCBJS2VybmVsRXZlbnRPYnNlcnZlcj4gPSBuZXcgTWFwKCk7XHJcbiAgICBwcml2YXRlIF9pc0NvbXBsZXRlID0gZmFsc2U7XHJcbiAgICBwdWJsaWMgaGFuZGxpbmdLZXJuZWw6IEtlcm5lbCB8IG51bGwgPSBudWxsO1xyXG4gICAgcHJpdmF0ZSBjb21wbGV0aW9uU291cmNlID0gbmV3IFByb21pc2VDb21wbGV0aW9uU291cmNlPHZvaWQ+KCk7XHJcbiAgICBzdGF0aWMgZXN0YWJsaXNoKGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB7XHJcbiAgICAgICAgbGV0IGN1cnJlbnQgPSBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5fY3VycmVudDtcclxuICAgICAgICBpZiAoIWN1cnJlbnQgfHwgY3VycmVudC5faXNDb21wbGV0ZSkge1xyXG4gICAgICAgICAgICBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5fY3VycmVudCA9IG5ldyBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dChrZXJuZWxDb21tYW5kSW52b2NhdGlvbik7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgaWYgKCFhcmVDb21tYW5kc1RoZVNhbWUoa2VybmVsQ29tbWFuZEludm9jYXRpb24sIGN1cnJlbnQuX2NvbW1hbmRFbnZlbG9wZSkpIHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGZvdW5kID0gY3VycmVudC5fY2hpbGRDb21tYW5kcy5pbmNsdWRlcyhrZXJuZWxDb21tYW5kSW52b2NhdGlvbik7XHJcbiAgICAgICAgICAgICAgICBpZiAoIWZvdW5kKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgY3VycmVudC5fY2hpbGRDb21tYW5kcy5wdXNoKGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIEtlcm5lbEludm9jYXRpb25Db250ZXh0Ll9jdXJyZW50ITtcclxuICAgIH1cclxuXHJcbiAgICBzdGF0aWMgZ2V0IGN1cnJlbnQoKTogS2VybmVsSW52b2NhdGlvbkNvbnRleHQgfCBudWxsIHsgcmV0dXJuIHRoaXMuX2N1cnJlbnQ7IH1cclxuICAgIGdldCBjb21tYW5kKCk6IEtlcm5lbENvbW1hbmQgeyByZXR1cm4gdGhpcy5fY29tbWFuZEVudmVsb3BlLmNvbW1hbmQ7IH1cclxuICAgIGdldCBjb21tYW5kRW52ZWxvcGUoKTogS2VybmVsQ29tbWFuZEVudmVsb3BlIHsgcmV0dXJuIHRoaXMuX2NvbW1hbmRFbnZlbG9wZTsgfVxyXG4gICAgY29uc3RydWN0b3Ioa2VybmVsQ29tbWFuZEludm9jYXRpb246IEtlcm5lbENvbW1hbmRFbnZlbG9wZSkge1xyXG4gICAgICAgIHRoaXMuX2NvbW1hbmRFbnZlbG9wZSA9IGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uO1xyXG4gICAgfVxyXG5cclxuICAgIHN1YnNjcmliZVRvS2VybmVsRXZlbnRzKG9ic2VydmVyOiBJS2VybmVsRXZlbnRPYnNlcnZlcikge1xyXG4gICAgICAgIGxldCBzdWJUb2tlbiA9IHRoaXMuX3Rva2VuR2VuZXJhdG9yLkdldE5ld1Rva2VuKCk7XHJcbiAgICAgICAgdGhpcy5fZXZlbnRPYnNlcnZlcnMuc2V0KHN1YlRva2VuLCBvYnNlcnZlcik7XHJcbiAgICAgICAgcmV0dXJuIHtcclxuICAgICAgICAgICAgZGlzcG9zZTogKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgdGhpcy5fZXZlbnRPYnNlcnZlcnMuZGVsZXRlKHN1YlRva2VuKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH07XHJcbiAgICB9XHJcbiAgICBjb21wbGV0ZShjb21tYW5kOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpIHtcclxuICAgICAgICBpZiAoY29tbWFuZCA9PT0gdGhpcy5fY29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgICAgIHRoaXMuX2lzQ29tcGxldGUgPSB0cnVlO1xyXG4gICAgICAgICAgICBsZXQgc3VjY2VlZGVkOiBDb21tYW5kU3VjY2VlZGVkID0ge307XHJcbiAgICAgICAgICAgIGxldCBldmVudEVudmVsb3BlOiBLZXJuZWxFdmVudEVudmVsb3BlID0ge1xyXG4gICAgICAgICAgICAgICAgY29tbWFuZDogdGhpcy5fY29tbWFuZEVudmVsb3BlLFxyXG4gICAgICAgICAgICAgICAgZXZlbnRUeXBlOiBDb21tYW5kU3VjY2VlZGVkVHlwZSxcclxuICAgICAgICAgICAgICAgIGV2ZW50OiBzdWNjZWVkZWRcclxuICAgICAgICAgICAgfTtcclxuICAgICAgICAgICAgdGhpcy5pbnRlcm5hbFB1Ymxpc2goZXZlbnRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgIHRoaXMuY29tcGxldGlvblNvdXJjZS5yZXNvbHZlKCk7XHJcbiAgICAgICAgICAgIC8vIFRPRE86IEMjIHZlcnNpb24gaGFzIGNvbXBsZXRpb24gY2FsbGJhY2tzIC0gZG8gd2UgbmVlZCB0aGVzZT9cclxuICAgICAgICAgICAgLy8gaWYgKCFfZXZlbnRzLklzRGlzcG9zZWQpXHJcbiAgICAgICAgICAgIC8vIHtcclxuICAgICAgICAgICAgLy8gICAgIF9ldmVudHMuT25Db21wbGV0ZWQoKTtcclxuICAgICAgICAgICAgLy8gfVxyXG5cclxuICAgICAgICB9XHJcbiAgICAgICAgZWxzZSB7XHJcbiAgICAgICAgICAgIGxldCBwb3MgPSB0aGlzLl9jaGlsZENvbW1hbmRzLmluZGV4T2YoY29tbWFuZCk7XHJcbiAgICAgICAgICAgIGRlbGV0ZSB0aGlzLl9jaGlsZENvbW1hbmRzW3Bvc107XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGZhaWwobWVzc2FnZT86IHN0cmluZykge1xyXG4gICAgICAgIC8vIFRPRE86XHJcbiAgICAgICAgLy8gVGhlIEMjIGNvZGUgYWNjZXB0cyBhIG1lc3NhZ2UgYW5kL29yIGFuIGV4Y2VwdGlvbi4gRG8gd2UgbmVlZCB0byBhZGQgc3VwcG9ydFxyXG4gICAgICAgIC8vIGZvciBleGNlcHRpb25zPyAoVGhlIFRTIENvbW1hbmRGYWlsZWQgaW50ZXJmYWNlIGRvZXNuJ3QgaGF2ZSBhIHBsYWNlIGZvciBpdCByaWdodCBub3cuKVxyXG4gICAgICAgIHRoaXMuX2lzQ29tcGxldGUgPSB0cnVlO1xyXG4gICAgICAgIGxldCBmYWlsZWQ6IENvbW1hbmRGYWlsZWQgPSB7IG1lc3NhZ2U6IG1lc3NhZ2UgPz8gXCJDb21tYW5kIEZhaWxlZFwiIH07XHJcbiAgICAgICAgbGV0IGV2ZW50RW52ZWxvcGU6IEtlcm5lbEV2ZW50RW52ZWxvcGUgPSB7XHJcbiAgICAgICAgICAgIGNvbW1hbmQ6IHRoaXMuX2NvbW1hbmRFbnZlbG9wZSxcclxuICAgICAgICAgICAgZXZlbnRUeXBlOiBDb21tYW5kRmFpbGVkVHlwZSxcclxuICAgICAgICAgICAgZXZlbnQ6IGZhaWxlZFxyXG4gICAgICAgIH07XHJcblxyXG4gICAgICAgIHRoaXMuaW50ZXJuYWxQdWJsaXNoKGV2ZW50RW52ZWxvcGUpO1xyXG4gICAgICAgIHRoaXMuY29tcGxldGlvblNvdXJjZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGlzaChrZXJuZWxFdmVudDogS2VybmVsRXZlbnRFbnZlbG9wZSkge1xyXG4gICAgICAgIGlmICghdGhpcy5faXNDb21wbGV0ZSkge1xyXG4gICAgICAgICAgICB0aGlzLmludGVybmFsUHVibGlzaChrZXJuZWxFdmVudCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgaW50ZXJuYWxQdWJsaXNoKGtlcm5lbEV2ZW50OiBLZXJuZWxFdmVudEVudmVsb3BlKSB7XHJcbiAgICAgICAgbGV0IGNvbW1hbmQgPSBrZXJuZWxFdmVudC5jb21tYW5kO1xyXG4gICAgICAgIGlmIChjb21tYW5kID09PSBudWxsIHx8XHJcbiAgICAgICAgICAgIGFyZUNvbW1hbmRzVGhlU2FtZShjb21tYW5kISwgdGhpcy5fY29tbWFuZEVudmVsb3BlKSB8fFxyXG4gICAgICAgICAgICB0aGlzLl9jaGlsZENvbW1hbmRzLmluY2x1ZGVzKGNvbW1hbmQhKSkge1xyXG4gICAgICAgICAgICB0aGlzLl9ldmVudE9ic2VydmVycy5mb3JFYWNoKChvYnNlcnZlcikgPT4ge1xyXG4gICAgICAgICAgICAgICAgb2JzZXJ2ZXIoa2VybmVsRXZlbnQpO1xyXG4gICAgICAgICAgICB9KTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgaXNQYXJlbnRPZkNvbW1hbmQoY29tbWFuZEVudmVsb3BlOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBib29sZWFuIHtcclxuICAgICAgICBjb25zdCBjaGlsZEZvdW5kID0gdGhpcy5fY2hpbGRDb21tYW5kcy5pbmNsdWRlcyhjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIHJldHVybiBjaGlsZEZvdW5kO1xyXG4gICAgfVxyXG5cclxuICAgIGRpc3Bvc2UoKSB7XHJcbiAgICAgICAgaWYgKCF0aGlzLl9pc0NvbXBsZXRlKSB7XHJcbiAgICAgICAgICAgIHRoaXMuY29tcGxldGUodGhpcy5fY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgS2VybmVsSW52b2NhdGlvbkNvbnRleHQuX2N1cnJlbnQgPSBudWxsO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gYXJlQ29tbWFuZHNUaGVTYW1lKGVudmVsb3BlMTogS2VybmVsQ29tbWFuZEVudmVsb3BlLCBlbnZlbG9wZTI6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSk6IGJvb2xlYW4ge1xyXG4gICAgcmV0dXJuIGVudmVsb3BlMSA9PT0gZW52ZWxvcGUyXHJcbiAgICAgICAgfHwgKGVudmVsb3BlMS5jb21tYW5kVHlwZSA9PT0gZW52ZWxvcGUyLmNvbW1hbmRUeXBlICYmIGVudmVsb3BlMS50b2tlbiA9PT0gZW52ZWxvcGUyLnRva2VuKTtcclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UgfSBmcm9tIFwiLi9nZW5lcmljQ2hhbm5lbFwiO1xyXG5cclxuaW50ZXJmYWNlIFNjaGVkdWxlck9wZXJhdGlvbjxUPiB7XHJcbiAgICB2YWx1ZTogVDtcclxuICAgIGV4ZWN1dG9yOiAodmFsdWU6IFQpID0+IFByb21pc2U8dm9pZD47XHJcbiAgICBwcm9taXNlQ29tcGxldGlvblNvdXJjZTogUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U8dm9pZD47XHJcbn1cclxuXHJcbmV4cG9ydCBjbGFzcyBLZXJuZWxTY2hlZHVsZXI8VD4ge1xyXG4gICAgcHJpdmF0ZSBvcGVyYXRpb25RdWV1ZTogQXJyYXk8U2NoZWR1bGVyT3BlcmF0aW9uPFQ+PiA9IFtdO1xyXG4gICAgcHJpdmF0ZSBpbkZsaWdodE9wZXJhdGlvbj86IFNjaGVkdWxlck9wZXJhdGlvbjxUPjtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcigpIHtcclxuICAgIH1cclxuXHJcbiAgICBydW5Bc3luYyh2YWx1ZTogVCwgZXhlY3V0b3I6ICh2YWx1ZTogVCkgPT4gUHJvbWlzZTx2b2lkPik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IG9wZXJhdGlvbiA9IHtcclxuICAgICAgICAgICAgdmFsdWUsXHJcbiAgICAgICAgICAgIGV4ZWN1dG9yLFxyXG4gICAgICAgICAgICBwcm9taXNlQ29tcGxldGlvblNvdXJjZTogbmV3IFByb21pc2VDb21wbGV0aW9uU291cmNlPHZvaWQ+KCksXHJcbiAgICAgICAgfTtcclxuXHJcbiAgICAgICAgaWYgKHRoaXMuaW5GbGlnaHRPcGVyYXRpb24pIHtcclxuICAgICAgICAgICAgLy8gaW52b2tlIGltbWVkaWF0ZWx5XHJcbiAgICAgICAgICAgIHJldHVybiBvcGVyYXRpb24uZXhlY3V0b3Iob3BlcmF0aW9uLnZhbHVlKVxyXG4gICAgICAgICAgICAgICAgLnRoZW4oKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIG9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5yZXNvbHZlKCk7XHJcbiAgICAgICAgICAgICAgICB9KVxyXG4gICAgICAgICAgICAgICAgLmNhdGNoKGUgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIG9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5yZWplY3QoZSk7XHJcbiAgICAgICAgICAgICAgICB9KTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHRoaXMub3BlcmF0aW9uUXVldWUucHVzaChvcGVyYXRpb24pO1xyXG4gICAgICAgIGlmICh0aGlzLm9wZXJhdGlvblF1ZXVlLmxlbmd0aCA9PT0gMSkge1xyXG4gICAgICAgICAgICB0aGlzLmV4ZWN1dGVOZXh0Q29tbWFuZCgpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIG9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5wcm9taXNlO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgZXhlY3V0ZU5leHRDb21tYW5kKCk6IHZvaWQge1xyXG4gICAgICAgIGNvbnN0IG5leHRPcGVyYXRpb24gPSB0aGlzLm9wZXJhdGlvblF1ZXVlLmxlbmd0aCA+IDAgPyB0aGlzLm9wZXJhdGlvblF1ZXVlWzBdIDogdW5kZWZpbmVkO1xyXG4gICAgICAgIGlmIChuZXh0T3BlcmF0aW9uKSB7XHJcbiAgICAgICAgICAgIHRoaXMuaW5GbGlnaHRPcGVyYXRpb24gPSBuZXh0T3BlcmF0aW9uO1xyXG4gICAgICAgICAgICBuZXh0T3BlcmF0aW9uLmV4ZWN1dG9yKG5leHRPcGVyYXRpb24udmFsdWUpXHJcbiAgICAgICAgICAgICAgICAudGhlbigoKSA9PiB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5pbkZsaWdodE9wZXJhdGlvbiA9IHVuZGVmaW5lZDtcclxuICAgICAgICAgICAgICAgICAgICBuZXh0T3BlcmF0aW9uLnByb21pc2VDb21wbGV0aW9uU291cmNlLnJlc29sdmUoKTtcclxuICAgICAgICAgICAgICAgIH0pXHJcbiAgICAgICAgICAgICAgICAuY2F0Y2goZSA9PiB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5pbkZsaWdodE9wZXJhdGlvbiA9IHVuZGVmaW5lZDtcclxuICAgICAgICAgICAgICAgICAgICBuZXh0T3BlcmF0aW9uLnByb21pc2VDb21wbGV0aW9uU291cmNlLnJlamVjdChlKTtcclxuICAgICAgICAgICAgICAgIH0pXHJcbiAgICAgICAgICAgICAgICAuZmluYWxseSgoKSA9PiB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5vcGVyYXRpb25RdWV1ZS5zaGlmdCgpO1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuZXhlY3V0ZU5leHRDb21tYW5kKCk7XHJcbiAgICAgICAgICAgICAgICB9KTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCB7IGFyZUNvbW1hbmRzVGhlU2FtZSwgS2VybmVsSW52b2NhdGlvbkNvbnRleHQgfSBmcm9tIFwiLi9rZXJuZWxJbnZvY2F0aW9uQ29udGV4dFwiO1xyXG5pbXBvcnQgeyBHdWlkLCBUb2tlbkdlbmVyYXRvciB9IGZyb20gXCIuL3Rva2VuR2VuZXJhdG9yXCI7XHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IENvbXBvc2l0ZUtlcm5lbCB9IGZyb20gXCIuL2NvbXBvc2l0ZUtlcm5lbFwiO1xyXG5pbXBvcnQgeyBLZXJuZWxTY2hlZHVsZXIgfSBmcm9tIFwiLi9rZXJuZWxTY2hlZHVsZXJcIjtcclxuaW1wb3J0IHsgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UgfSBmcm9tIFwiLi9nZW5lcmljQ2hhbm5lbFwiO1xyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJS2VybmVsQ29tbWFuZEludm9jYXRpb24ge1xyXG4gICAgY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlO1xyXG4gICAgY29udGV4dDogS2VybmVsSW52b2NhdGlvbkNvbnRleHQ7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSUtlcm5lbENvbW1hbmRIYW5kbGVyIHtcclxuICAgIGNvbW1hbmRUeXBlOiBzdHJpbmc7XHJcbiAgICBoYW5kbGU6IChjb21tYW5kSW52b2NhdGlvbjogSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uKSA9PiBQcm9taXNlPHZvaWQ+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIElLZXJuZWxFdmVudE9ic2VydmVyIHtcclxuICAgIChrZXJuZWxFdmVudDogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpOiB2b2lkO1xyXG59XHJcblxyXG5leHBvcnQgY2xhc3MgS2VybmVsIHtcclxuICAgIHByaXZhdGUgX2tlcm5lbEluZm86IGNvbnRyYWN0cy5LZXJuZWxJbmZvO1xyXG5cclxuICAgIHByaXZhdGUgX2NvbW1hbmRIYW5kbGVycyA9IG5ldyBNYXA8c3RyaW5nLCBJS2VybmVsQ29tbWFuZEhhbmRsZXI+KCk7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9ldmVudE9ic2VydmVyczogeyBbdG9rZW46IHN0cmluZ106IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlT2JzZXJ2ZXIgfSA9IHt9O1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfdG9rZW5HZW5lcmF0b3I6IFRva2VuR2VuZXJhdG9yID0gbmV3IFRva2VuR2VuZXJhdG9yKCk7XHJcbiAgICBwdWJsaWMgcm9vdEtlcm5lbDogS2VybmVsID0gdGhpcztcclxuICAgIHB1YmxpYyBwYXJlbnRLZXJuZWw6IENvbXBvc2l0ZUtlcm5lbCB8IG51bGwgPSBudWxsO1xyXG4gICAgcHJpdmF0ZSBfc2NoZWR1bGVyPzogS2VybmVsU2NoZWR1bGVyPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGU+IHwgbnVsbCA9IG51bGw7XHJcblxyXG4gICAgcHVibGljIGdldCBrZXJuZWxJbmZvKCk6IGNvbnRyYWN0cy5LZXJuZWxJbmZvIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5fa2VybmVsSW5mbztcclxuICAgIH1cclxuXHJcbiAgICBjb25zdHJ1Y3RvcihyZWFkb25seSBuYW1lOiBzdHJpbmcsIGxhbmd1YWdlTmFtZT86IHN0cmluZywgbGFuZ3VhZ2VWZXJzaW9uPzogc3RyaW5nKSB7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsSW5mbyA9IHtcclxuICAgICAgICAgICAgbG9jYWxOYW1lOiBuYW1lLFxyXG4gICAgICAgICAgICBsYW5ndWFnZU5hbWU6IGxhbmd1YWdlTmFtZSxcclxuICAgICAgICAgICAgYWxpYXNlczogW10sXHJcbiAgICAgICAgICAgIGxhbmd1YWdlVmVyc2lvbjogbGFuZ3VhZ2VWZXJzaW9uLFxyXG4gICAgICAgICAgICBzdXBwb3J0ZWREaXJlY3RpdmVzOiBbXSxcclxuICAgICAgICAgICAgc3VwcG9ydGVkS2VybmVsQ29tbWFuZHM6IFtdXHJcbiAgICAgICAgfTtcclxuXHJcbiAgICAgICAgdGhpcy5yZWdpc3RlckNvbW1hbmRIYW5kbGVyKHtcclxuICAgICAgICAgICAgY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5SZXF1ZXN0S2VybmVsSW5mb1R5cGUsIGhhbmRsZTogYXN5bmMgaW52b2NhdGlvbiA9PiB7XHJcbiAgICAgICAgICAgICAgICBhd2FpdCB0aGlzLmhhbmRsZVJlcXVlc3RLZXJuZWxJbmZvKGludm9jYXRpb24pO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJvdGVjdGVkIGFzeW5jIGhhbmRsZVJlcXVlc3RLZXJuZWxJbmZvKGludm9jYXRpb246IElLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IGV2ZW50RW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlID0ge1xyXG4gICAgICAgICAgICBldmVudFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWRUeXBlLFxyXG4gICAgICAgICAgICBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSxcclxuICAgICAgICAgICAgZXZlbnQ6IDxjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkPnsga2VybmVsSW5mbzogdGhpcy5fa2VybmVsSW5mbyB9XHJcbiAgICAgICAgfTsvLz9cclxuXHJcbiAgICAgICAgaW52b2NhdGlvbi5jb250ZXh0LnB1Ymxpc2goZXZlbnRFbnZlbG9wZSk7XHJcbiAgICAgICAgcmV0dXJuIFByb21pc2UucmVzb2x2ZSgpO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgZ2V0U2NoZWR1bGVyKCk6IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPiB7XHJcbiAgICAgICAgaWYgKCF0aGlzLl9zY2hlZHVsZXIpIHtcclxuICAgICAgICAgICAgdGhpcy5fc2NoZWR1bGVyID0gdGhpcy5wYXJlbnRLZXJuZWw/LmdldFNjaGVkdWxlcigpID8/IG5ldyBLZXJuZWxTY2hlZHVsZXI8Y29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZT4oKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiB0aGlzLl9zY2hlZHVsZXI7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBlbnN1cmVDb21tYW5kVG9rZW5BbmRJZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpIHtcclxuICAgICAgICBjb21tYW5kRW52ZWxvcGU7Ly8/XHJcbiAgICAgICAgaWYgKCFjb21tYW5kRW52ZWxvcGUudG9rZW4pIHtcclxuICAgICAgICAgICAgbGV0IG5leHRUb2tlbiA9IHRoaXMuX3Rva2VuR2VuZXJhdG9yLkdldE5ld1Rva2VuKCk7XHJcbiAgICAgICAgICAgIGlmIChLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5jdXJyZW50Py5jb21tYW5kRW52ZWxvcGUpIHtcclxuICAgICAgICAgICAgICAgIC8vIGEgcGFyZW50IGNvbW1hbmQgZXhpc3RzLCBjcmVhdGUgYSB0b2tlbiBoaWVyYXJjaHlcclxuICAgICAgICAgICAgICAgIG5leHRUb2tlbiA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQuY29tbWFuZEVudmVsb3BlLnRva2VuITtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgY29tbWFuZEVudmVsb3BlLnRva2VuID0gbmV4dFRva2VuO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKCFjb21tYW5kRW52ZWxvcGUuaWQpIHtcclxuICAgICAgICAgICAgY29tbWFuZEVudmVsb3BlLmlkID0gR3VpZC5jcmVhdGUoKS50b1N0cmluZygpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBzdGF0aWMgZ2V0IGN1cnJlbnQoKTogS2VybmVsIHwgbnVsbCB7XHJcbiAgICAgICAgaWYgKEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQpIHtcclxuICAgICAgICAgICAgcmV0dXJuIEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQuaGFuZGxpbmdLZXJuZWw7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJldHVybiBudWxsO1xyXG4gICAgfVxyXG5cclxuICAgIHN0YXRpYyBnZXQgcm9vdCgpOiBLZXJuZWwgfCBudWxsIHtcclxuICAgICAgICBpZiAoS2VybmVsLmN1cnJlbnQpIHtcclxuICAgICAgICAgICAgcmV0dXJuIEtlcm5lbC5jdXJyZW50LnJvb3RLZXJuZWw7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJldHVybiBudWxsO1xyXG4gICAgfVxyXG5cclxuICAgIC8vIElzIGl0IHdvcnRoIHVzIGdvaW5nIHRvIGVmZm9ydHMgdG8gZW5zdXJlIHRoYXQgdGhlIFByb21pc2UgcmV0dXJuZWQgaGVyZSBhY2N1cmF0ZWx5IHJlZmxlY3RzXHJcbiAgICAvLyB0aGUgY29tbWFuZCdzIHByb2dyZXNzPyBUaGUgb25seSB0aGluZyB0aGF0IGFjdHVhbGx5IGNhbGxzIHRoaXMgaXMgdGhlIGtlcm5lbCBjaGFubmVsLCB0aHJvdWdoXHJcbiAgICAvLyB0aGUgY2FsbGJhY2sgc2V0IHVwIGJ5IGF0dGFjaEtlcm5lbFRvQ2hhbm5lbCwgYW5kIHRoZSBjYWxsYmFjayBpcyBleHBlY3RlZCB0byByZXR1cm4gdm9pZCwgc29cclxuICAgIC8vIG5vdGhpbmcgaXMgZXZlciBnb2luZyB0byBsb29rIGF0IHRoZSBwcm9taXNlIHdlIHJldHVybiBoZXJlLlxyXG4gICAgYXN5bmMgc2VuZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICB0aGlzLmVuc3VyZUNvbW1hbmRUb2tlbkFuZElkKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgbGV0IGNvbnRleHQgPSBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5lc3RhYmxpc2goY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICB0aGlzLmdldFNjaGVkdWxlcigpLnJ1bkFzeW5jKGNvbW1hbmRFbnZlbG9wZSwgKHZhbHVlKSA9PiB0aGlzLmV4ZWN1dGVDb21tYW5kKHZhbHVlKSk7XHJcbiAgICAgICAgcmV0dXJuIGNvbnRleHQucHJvbWlzZTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGFzeW5jIGV4ZWN1dGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGxldCBjb250ZXh0ID0gS2VybmVsSW52b2NhdGlvbkNvbnRleHQuZXN0YWJsaXNoKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgbGV0IGlzUm9vdENvbW1hbmQgPSBhcmVDb21tYW5kc1RoZVNhbWUoY29udGV4dC5jb21tYW5kRW52ZWxvcGUsIGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgbGV0IGNvbnRleHRFdmVudHNTdWJzY3JpcHRpb246IGNvbnRyYWN0cy5EaXNwb3NhYmxlIHwgbnVsbCA9IG51bGw7XHJcbiAgICAgICAgaWYgKGlzUm9vdENvbW1hbmQpIHtcclxuICAgICAgICAgICAgY29udGV4dEV2ZW50c1N1YnNjcmlwdGlvbiA9IGNvbnRleHQuc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMoZSA9PiB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBtZXNzYWdlID0gYGtlcm5lbCAke3RoaXMubmFtZX0gc2F3IGV2ZW50ICR7ZS5ldmVudFR5cGV9IHdpdGggdG9rZW4gJHtlLmNvbW1hbmQ/LnRva2VufWA7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKG1lc3NhZ2UpO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXMucHVibGlzaEV2ZW50KGUpO1xyXG4gICAgICAgICAgICB9KTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHRyeSB7XHJcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuaGFuZGxlQ29tbWFuZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIH1cclxuICAgICAgICBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICBjb250ZXh0LmZhaWwoKDxhbnk+ZSk/Lm1lc3NhZ2UgfHwgSlNPTi5zdHJpbmdpZnkoZSkpO1xyXG4gICAgICAgIH1cclxuICAgICAgICBmaW5hbGx5IHtcclxuICAgICAgICAgICAgaWYgKGNvbnRleHRFdmVudHNTdWJzY3JpcHRpb24pIHtcclxuICAgICAgICAgICAgICAgIGNvbnRleHRFdmVudHNTdWJzY3JpcHRpb24uZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGdldENvbW1hbmRIYW5kbGVyKGNvbW1hbmRUeXBlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZFR5cGUpOiBJS2VybmVsQ29tbWFuZEhhbmRsZXIgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9jb21tYW5kSGFuZGxlcnMuZ2V0KGNvbW1hbmRUeXBlKTtcclxuICAgIH1cclxuXHJcbiAgICBoYW5kbGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIHJldHVybiBuZXcgUHJvbWlzZTx2b2lkPihhc3luYyAocmVzb2x2ZSwgcmVqZWN0KSA9PiB7XHJcbiAgICAgICAgICAgIGxldCBjb250ZXh0ID0gS2VybmVsSW52b2NhdGlvbkNvbnRleHQuZXN0YWJsaXNoKGNvbW1hbmRFbnZlbG9wZSk7Ly8/XHJcbiAgICAgICAgICAgIGNvbnRleHQuaGFuZGxpbmdLZXJuZWwgPSB0aGlzO1xyXG4gICAgICAgICAgICBsZXQgaXNSb290Q29tbWFuZCA9IGFyZUNvbW1hbmRzVGhlU2FtZShjb250ZXh0LmNvbW1hbmRFbnZlbG9wZSwgY29tbWFuZEVudmVsb3BlKTtcclxuXHJcbiAgICAgICAgICAgIGxldCBoYW5kbGVyID0gdGhpcy5nZXRDb21tYW5kSGFuZGxlcihjb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGUpO1xyXG4gICAgICAgICAgICBpZiAoaGFuZGxlcikge1xyXG4gICAgICAgICAgICAgICAgdHJ5IHtcclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWwgJHt0aGlzLm5hbWV9IGFib3V0IHRvIGhhbmRsZSBjb21tYW5kOiAke0pTT04uc3RyaW5naWZ5KGNvbW1hbmRFbnZlbG9wZSl9YCk7XHJcbiAgICAgICAgICAgICAgICAgICAgYXdhaXQgaGFuZGxlci5oYW5kbGUoeyBjb21tYW5kRW52ZWxvcGU6IGNvbW1hbmRFbnZlbG9wZSwgY29udGV4dCB9KTtcclxuXHJcbiAgICAgICAgICAgICAgICAgICAgY29udGV4dC5jb21wbGV0ZShjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChpc1Jvb3RDb21tYW5kKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbnRleHQuZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhga2VybmVsICR7dGhpcy5uYW1lfSBkb25lIGhhbmRsaW5nIGNvbW1hbmQ6ICR7SlNPTi5zdHJpbmdpZnkoY29tbWFuZEVudmVsb3BlKX1gKTtcclxuICAgICAgICAgICAgICAgICAgICByZXNvbHZlKCk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnRleHQuZmFpbCgoPGFueT5lKT8ubWVzc2FnZSB8fCBKU09OLnN0cmluZ2lmeShlKSk7XHJcbiAgICAgICAgICAgICAgICAgICAgaWYgKGlzUm9vdENvbW1hbmQpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29udGV4dC5kaXNwb3NlKCk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgICAgICAgICByZWplY3QoZSk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICBpZiAoaXNSb290Q29tbWFuZCkge1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnRleHQuZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgICAgIHJlamVjdChuZXcgRXJyb3IoYE5vIGhhbmRsZXIgZm91bmQgZm9yIGNvbW1hbmQgdHlwZSAke2NvbW1hbmRFbnZlbG9wZS5jb21tYW5kVHlwZX1gKSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuICAgIH1cclxuXHJcbiAgICBzdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhvYnNlcnZlcjogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGVPYnNlcnZlcik6IGNvbnRyYWN0cy5EaXNwb3NhYmxlU3Vic2NyaXB0aW9uIHtcclxuICAgICAgICBsZXQgc3ViVG9rZW4gPSB0aGlzLl90b2tlbkdlbmVyYXRvci5HZXROZXdUb2tlbigpO1xyXG4gICAgICAgIHRoaXMuX2V2ZW50T2JzZXJ2ZXJzW3N1YlRva2VuXSA9IG9ic2VydmVyO1xyXG5cclxuICAgICAgICByZXR1cm4ge1xyXG4gICAgICAgICAgICBkaXNwb3NlOiAoKSA9PiB7IGRlbGV0ZSB0aGlzLl9ldmVudE9ic2VydmVyc1tzdWJUb2tlbl07IH1cclxuICAgICAgICB9O1xyXG4gICAgfVxyXG5cclxuICAgIHByb3RlY3RlZCBjYW5IYW5kbGUoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgaWYgKGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUgJiYgY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSAhPT0gdGhpcy5uYW1lKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBmYWxzZTtcclxuXHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBpZiAoY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25VcmkpIHtcclxuICAgICAgICAgICAgaWYgKHRoaXMua2VybmVsSW5mby51cmkgIT09IGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiB0aGlzLnN1cHBvcnRzQ29tbWFuZChjb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGUpO1xyXG4gICAgfVxyXG5cclxuICAgIHN1cHBvcnRzQ29tbWFuZChjb21tYW5kVHlwZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRUeXBlKTogYm9vbGVhbiB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2NvbW1hbmRIYW5kbGVycy5oYXMoY29tbWFuZFR5cGUpO1xyXG4gICAgfVxyXG5cclxuICAgIHJlZ2lzdGVyQ29tbWFuZEhhbmRsZXIoaGFuZGxlcjogSUtlcm5lbENvbW1hbmRIYW5kbGVyKTogdm9pZCB7XHJcbiAgICAgICAgLy8gV2hlbiBhIHJlZ2lzdHJhdGlvbiBhbHJlYWR5IGV4aXN0ZWQsIHdlIHdhbnQgdG8gb3ZlcndyaXRlIGl0IGJlY2F1c2Ugd2Ugd2FudCB1c2VycyB0b1xyXG4gICAgICAgIC8vIGJlIGFibGUgdG8gZGV2ZWxvcCBoYW5kbGVycyBpdGVyYXRpdmVseSwgYW5kIGl0IHdvdWxkIGJlIHVuaGVscGZ1bCBmb3IgaGFuZGxlciByZWdpc3RyYXRpb25cclxuICAgICAgICAvLyBmb3IgYW55IHBhcnRpY3VsYXIgY29tbWFuZCB0byBiZSBjdW11bGF0aXZlLlxyXG4gICAgICAgIHRoaXMuX2NvbW1hbmRIYW5kbGVycy5zZXQoaGFuZGxlci5jb21tYW5kVHlwZSwgaGFuZGxlcik7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsSW5mby5zdXBwb3J0ZWRLZXJuZWxDb21tYW5kcyA9IEFycmF5LmZyb20odGhpcy5fY29tbWFuZEhhbmRsZXJzLmtleXMoKSkubWFwKGNvbW1hbmROYW1lID0+ICh7IG5hbWU6IGNvbW1hbmROYW1lIH0pKTtcclxuICAgIH1cclxuXHJcbiAgICBnZXRIYW5kbGluZ0tlcm5lbChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBLZXJuZWwgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIGxldCB0YXJnZXRLZXJuZWxOYW1lID0gY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSA/PyB0aGlzLm5hbWU7XHJcbiAgICAgICAgcmV0dXJuIHRhcmdldEtlcm5lbE5hbWUgPT09IHRoaXMubmFtZSA/IHRoaXMgOiB1bmRlZmluZWQ7XHJcbiAgICB9XHJcblxyXG4gICAgcHJvdGVjdGVkIHB1Ymxpc2hFdmVudChrZXJuZWxFdmVudDogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpIHtcclxuICAgICAgICBsZXQga2V5cyA9IE9iamVjdC5rZXlzKHRoaXMuX2V2ZW50T2JzZXJ2ZXJzKTtcclxuICAgICAgICBmb3IgKGxldCBzdWJUb2tlbiBvZiBrZXlzKSB7XHJcbiAgICAgICAgICAgIGxldCBvYnNlcnZlciA9IHRoaXMuX2V2ZW50T2JzZXJ2ZXJzW3N1YlRva2VuXTtcclxuICAgICAgICAgICAgb2JzZXJ2ZXIoa2VybmVsRXZlbnQpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG5cclxuZXhwb3J0IGFzeW5jIGZ1bmN0aW9uIHN1Ym1pdENvbW1hbmRBbmRHZXRSZXN1bHQ8VEV2ZW50IGV4dGVuZHMgY29udHJhY3RzLktlcm5lbEV2ZW50PihrZXJuZWw6IEtlcm5lbCwgY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlLCBleHBlY3RlZEV2ZW50VHlwZTogY29udHJhY3RzLktlcm5lbEV2ZW50VHlwZSk6IFByb21pc2U8VEV2ZW50PiB7XHJcbiAgICBsZXQgY29tcGxldGlvblNvdXJjZSA9IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxURXZlbnQ+KCk7XHJcbiAgICBsZXQgaGFuZGxlZCA9IGZhbHNlO1xyXG4gICAgbGV0IGRpc3Bvc2FibGUgPSBrZXJuZWwuc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMoZXZlbnRFbnZlbG9wZSA9PiB7XHJcbiAgICAgICAgaWYgKGV2ZW50RW52ZWxvcGUuY29tbWFuZD8udG9rZW4gPT09IGNvbW1hbmRFbnZlbG9wZS50b2tlbikge1xyXG4gICAgICAgICAgICBzd2l0Y2ggKGV2ZW50RW52ZWxvcGUuZXZlbnRUeXBlKSB7XHJcbiAgICAgICAgICAgICAgICBjYXNlIGNvbnRyYWN0cy5Db21tYW5kRmFpbGVkVHlwZTpcclxuICAgICAgICAgICAgICAgICAgICBpZiAoIWhhbmRsZWQpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgaGFuZGxlZCA9IHRydWU7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGxldCBlcnIgPSA8Y29udHJhY3RzLkNvbW1hbmRGYWlsZWQ+ZXZlbnRFbnZlbG9wZS5ldmVudDsvLz9cclxuICAgICAgICAgICAgICAgICAgICAgICAgY29tcGxldGlvblNvdXJjZS5yZWplY3QoZXJyKTtcclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICBjYXNlIGNvbnRyYWN0cy5Db21tYW5kU3VjY2VlZGVkVHlwZTpcclxuICAgICAgICAgICAgICAgICAgICBpZiAoYXJlQ29tbWFuZHNUaGVTYW1lKGV2ZW50RW52ZWxvcGUuY29tbWFuZCEsIGNvbW1hbmRFbnZlbG9wZSlcclxuICAgICAgICAgICAgICAgICAgICAgICAgJiYgKGV2ZW50RW52ZWxvcGUuY29tbWFuZD8uaWQgPT09IGNvbW1hbmRFbnZlbG9wZS5pZCkpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKCFoYW5kbGVkKSB7Ly8/ICgkID8gZXZlbnRFbnZlbG9wZSA6IHt9KVxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgaGFuZGxlZCA9IHRydWU7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjb21wbGV0aW9uU291cmNlLnJlamVjdCgnQ29tbWFuZCB3YXMgaGFuZGxlZCBiZWZvcmUgcmVwb3J0aW5nIGV4cGVjdGVkIHJlc3VsdC4nKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICBkZWZhdWx0OlxyXG4gICAgICAgICAgICAgICAgICAgIGlmIChldmVudEVudmVsb3BlLmV2ZW50VHlwZSA9PT0gZXhwZWN0ZWRFdmVudFR5cGUpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgaGFuZGxlZCA9IHRydWU7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGxldCBldmVudCA9IDxURXZlbnQ+ZXZlbnRFbnZlbG9wZS5ldmVudDsvLz8gKCQgPyBldmVudEVudmVsb3BlIDoge30pXHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbXBsZXRpb25Tb3VyY2UucmVzb2x2ZShldmVudCk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIGJyZWFrO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfSk7XHJcblxyXG4gICAgdHJ5IHtcclxuICAgICAgICBhd2FpdCBrZXJuZWwuc2VuZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgfVxyXG4gICAgZmluYWxseSB7XHJcbiAgICAgICAgZGlzcG9zYWJsZS5kaXNwb3NlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcmV0dXJuIGNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBJS2VybmVsQ29tbWFuZEludm9jYXRpb24sIEtlcm5lbCB9IGZyb20gXCIuL2tlcm5lbFwiO1xyXG5pbXBvcnQgeyBLZXJuZWxIb3N0IH0gZnJvbSBcIi4va2VybmVsSG9zdFwiO1xyXG5cclxuZXhwb3J0IGNsYXNzIENvbXBvc2l0ZUtlcm5lbCBleHRlbmRzIEtlcm5lbCB7XHJcblxyXG5cclxuICAgIHByaXZhdGUgX2hvc3Q6IEtlcm5lbEhvc3QgfCBudWxsID0gbnVsbDtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX25hbWVzVG9rZXJuZWxNYXA6IE1hcDxzdHJpbmcsIEtlcm5lbD4gPSBuZXcgTWFwKCk7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9rZXJuZWxUb05hbWVzTWFwOiBNYXA8S2VybmVsLCBTZXQ8c3RyaW5nPj4gPSBuZXcgTWFwKCk7XHJcblxyXG4gICAgZGVmYXVsdEtlcm5lbE5hbWU6IHN0cmluZyB8IHVuZGVmaW5lZDtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcihuYW1lOiBzdHJpbmcpIHtcclxuICAgICAgICBzdXBlcihuYW1lKTtcclxuICAgIH1cclxuXHJcbiAgICBnZXQgY2hpbGRLZXJuZWxzKCkge1xyXG4gICAgICAgIHJldHVybiBbLi4udGhpcy5fa2VybmVsVG9OYW1lc01hcC5rZXlzKCldO1xyXG4gICAgfVxyXG5cclxuICAgIGdldCBob3N0KCk6IEtlcm5lbEhvc3QgfCBudWxsIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5faG9zdDtcclxuICAgIH1cclxuXHJcbiAgICBzZXQgaG9zdChob3N0OiBLZXJuZWxIb3N0IHwgbnVsbCkge1xyXG4gICAgICAgIHRoaXMuX2hvc3QgPSBob3N0O1xyXG4gICAgICAgIGlmICh0aGlzLl9ob3N0KSB7XHJcbiAgICAgICAgICAgIHRoaXMuX2hvc3QuYWRkS2VybmVsSW5mbyh0aGlzLCB7IGxvY2FsTmFtZTogdGhpcy5uYW1lLnRvTG93ZXJDYXNlKCksIGFsaWFzZXM6IFtdLCBzdXBwb3J0ZWREaXJlY3RpdmVzOiBbXSwgc3VwcG9ydGVkS2VybmVsQ29tbWFuZHM6IFtdIH0pO1xyXG5cclxuICAgICAgICAgICAgZm9yIChsZXQga2VybmVsIG9mIHRoaXMuY2hpbGRLZXJuZWxzKSB7XHJcbiAgICAgICAgICAgICAgICBsZXQgYWxpYXNlcyA9IFtdO1xyXG4gICAgICAgICAgICAgICAgZm9yIChsZXQgbmFtZSBvZiB0aGlzLl9rZXJuZWxUb05hbWVzTWFwLmdldChrZXJuZWwpISkge1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChuYW1lICE9PSBrZXJuZWwubmFtZSkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBhbGlhc2VzLnB1c2gobmFtZS50b0xvd2VyQ2FzZSgpKTtcclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICB0aGlzLl9ob3N0LmFkZEtlcm5lbEluZm8oa2VybmVsLCB7IGxvY2FsTmFtZToga2VybmVsLm5hbWUudG9Mb3dlckNhc2UoKSwgYWxpYXNlczogWy4uLmFsaWFzZXNdLCBzdXBwb3J0ZWREaXJlY3RpdmVzOiBbXSwgc3VwcG9ydGVkS2VybmVsQ29tbWFuZHM6IFtdIH0pO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHByb3RlY3RlZCBvdmVycmlkZSBhc3luYyBoYW5kbGVSZXF1ZXN0S2VybmVsSW5mbyhpbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBmb3IgKGxldCBrZXJuZWwgb2YgdGhpcy5jaGlsZEtlcm5lbHMpIHtcclxuICAgICAgICAgICAgaWYgKGtlcm5lbC5zdXBwb3J0c0NvbW1hbmQoaW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGUpKSB7XHJcbiAgICAgICAgICAgICAgICBhd2FpdCBrZXJuZWwuaGFuZGxlQ29tbWFuZCh7IGNvbW1hbmQ6IHt9LCBjb21tYW5kVHlwZTogY29udHJhY3RzLlJlcXVlc3RLZXJuZWxJbmZvVHlwZSB9KTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBhZGQoa2VybmVsOiBLZXJuZWwsIGFsaWFzZXM/OiBzdHJpbmdbXSkge1xyXG4gICAgICAgIGlmICgha2VybmVsKSB7XHJcbiAgICAgICAgICAgIHRocm93IG5ldyBFcnJvcihcImtlcm5lbCBjYW5ub3QgYmUgbnVsbCBvciB1bmRlZmluZWRcIik7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBpZiAoIXRoaXMuZGVmYXVsdEtlcm5lbE5hbWUpIHtcclxuICAgICAgICAgICAgLy8gZGVmYXVsdCB0byBmaXJzdCBrZXJuZWxcclxuICAgICAgICAgICAgdGhpcy5kZWZhdWx0S2VybmVsTmFtZSA9IGtlcm5lbC5uYW1lO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAga2VybmVsLnBhcmVudEtlcm5lbCA9IHRoaXM7XHJcbiAgICAgICAga2VybmVsLnJvb3RLZXJuZWwgPSB0aGlzLnJvb3RLZXJuZWw7XHJcbiAgICAgICAga2VybmVsLnN1YnNjcmliZVRvS2VybmVsRXZlbnRzKGV2ZW50ID0+IHtcclxuICAgICAgICAgICAgdGhpcy5wdWJsaXNoRXZlbnQoZXZlbnQpO1xyXG4gICAgICAgIH0pO1xyXG4gICAgICAgIHRoaXMuX25hbWVzVG9rZXJuZWxNYXAuc2V0KGtlcm5lbC5uYW1lLnRvTG93ZXJDYXNlKCksIGtlcm5lbCk7XHJcblxyXG4gICAgICAgIGxldCBrZXJuZWxOYW1lcyA9IG5ldyBTZXQ8c3RyaW5nPigpO1xyXG4gICAgICAgIGtlcm5lbE5hbWVzLmFkZChrZXJuZWwubmFtZSk7XHJcbiAgICAgICAgaWYgKGFsaWFzZXMpIHtcclxuICAgICAgICAgICAgYWxpYXNlcy5mb3JFYWNoKGFsaWFzID0+IHtcclxuICAgICAgICAgICAgICAgIHRoaXMuX25hbWVzVG9rZXJuZWxNYXAuc2V0KGFsaWFzLnRvTG93ZXJDYXNlKCksIGtlcm5lbCk7XHJcbiAgICAgICAgICAgICAgICBrZXJuZWxOYW1lcy5hZGQoYWxpYXMudG9Mb3dlckNhc2UoKSk7XHJcbiAgICAgICAgICAgIH0pO1xyXG5cclxuICAgICAgICAgICAga2VybmVsLmtlcm5lbEluZm8uYWxpYXNlcyA9IGFsaWFzZXM7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICB0aGlzLl9rZXJuZWxUb05hbWVzTWFwLnNldChrZXJuZWwsIGtlcm5lbE5hbWVzKTtcclxuXHJcbiAgICAgICAgdGhpcy5ob3N0Py5hZGRLZXJuZWxJbmZvKGtlcm5lbCwga2VybmVsLmtlcm5lbEluZm8pO1xyXG4gICAgfVxyXG5cclxuICAgIGZpbmRLZXJuZWxCeU5hbWUoa2VybmVsTmFtZTogc3RyaW5nKTogS2VybmVsIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICBpZiAoa2VybmVsTmFtZS50b0xvd2VyQ2FzZSgpID09PSB0aGlzLm5hbWUudG9Mb3dlckNhc2UoKSkge1xyXG4gICAgICAgICAgICByZXR1cm4gdGhpcztcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiB0aGlzLl9uYW1lc1Rva2VybmVsTWFwLmdldChrZXJuZWxOYW1lLnRvTG93ZXJDYXNlKCkpO1xyXG4gICAgfVxyXG5cclxuICAgIGZpbmRLZXJuZWxCeVVyaSh1cmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgY29uc3Qga2VybmVscyA9IEFycmF5LmZyb20odGhpcy5fa2VybmVsVG9OYW1lc01hcC5rZXlzKCkpO1xyXG4gICAgICAgIGZvciAobGV0IGtlcm5lbCBvZiBrZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIGlmIChrZXJuZWwua2VybmVsSW5mby51cmkgPT09IHVyaSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGtlcm5lbDtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgZm9yIChsZXQga2VybmVsIG9mIGtlcm5lbHMpIHtcclxuICAgICAgICAgICAgaWYgKGtlcm5lbC5rZXJuZWxJbmZvLnJlbW90ZVVyaSA9PT0gdXJpKSB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gdW5kZWZpbmVkO1xyXG4gICAgfVxyXG5cclxuICAgIG92ZXJyaWRlIGhhbmRsZUNvbW1hbmQoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKTogUHJvbWlzZTx2b2lkPiB7XHJcblxyXG4gICAgICAgIGxldCBrZXJuZWwgPSBjb21tYW5kRW52ZWxvcGUuY29tbWFuZC50YXJnZXRLZXJuZWxOYW1lID09PSB0aGlzLm5hbWVcclxuICAgICAgICAgICAgPyB0aGlzXHJcbiAgICAgICAgICAgIDogdGhpcy5nZXRIYW5kbGluZ0tlcm5lbChjb21tYW5kRW52ZWxvcGUpO1xyXG5cclxuICAgICAgICBpZiAoa2VybmVsID09PSB0aGlzKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBzdXBlci5oYW5kbGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgfSBlbHNlIGlmIChrZXJuZWwpIHtcclxuICAgICAgICAgICAgcmV0dXJuIGtlcm5lbC5oYW5kbGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gUHJvbWlzZS5yZWplY3QobmV3IEVycm9yKFwiS2VybmVsIG5vdCBmb3VuZDogXCIgKyBjb21tYW5kRW52ZWxvcGUuY29tbWFuZC50YXJnZXRLZXJuZWxOYW1lKSk7XHJcbiAgICB9XHJcblxyXG4gICAgb3ZlcnJpZGUgZ2V0SGFuZGxpbmdLZXJuZWwoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKTogS2VybmVsIHwgdW5kZWZpbmVkIHtcclxuXHJcbiAgICAgICAgaWYgKGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgIGxldCBrZXJuZWwgPSB0aGlzLmZpbmRLZXJuZWxCeVVyaShjb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSk7XHJcbiAgICAgICAgICAgIGlmIChrZXJuZWwpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybiBrZXJuZWw7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICAgICAgaWYgKCFjb21tYW5kRW52ZWxvcGUuY29tbWFuZC50YXJnZXRLZXJuZWxOYW1lKSB7XHJcbiAgICAgICAgICAgIGlmIChzdXBlci5jYW5IYW5kbGUoY29tbWFuZEVudmVsb3BlKSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXM7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGxldCB0YXJnZXRLZXJuZWxOYW1lID0gY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSA/PyB0aGlzLmRlZmF1bHRLZXJuZWxOYW1lID8/IHRoaXMubmFtZTtcclxuXHJcbiAgICAgICAgbGV0IGtlcm5lbCA9IHRoaXMuZmluZEtlcm5lbEJ5TmFtZSh0YXJnZXRLZXJuZWxOYW1lKTtcclxuICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgfVxyXG59IiwiaW1wb3J0IHsgSW5zcGVjdE9wdGlvbnMgfSBmcm9tIFwidXRpbFwiO1xyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSBcIi4vY29udHJhY3RzXCI7XHJcbmltcG9ydCB7IEtlcm5lbEludm9jYXRpb25Db250ZXh0IH0gZnJvbSBcIi4va2VybmVsSW52b2NhdGlvbkNvbnRleHRcIjtcclxuXHJcbmV4cG9ydCBjbGFzcyBDb25zb2xlQ2FwdHVyZSBpbXBsZW1lbnRzIGNvbnRyYWN0cy5EaXNwb3NhYmxlIHtcclxuICAgIHByaXZhdGUgb3JpZ2luYWxDb25zb2xlOiBDb25zb2xlO1xyXG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBrZXJuZWxJbnZvY2F0aW9uQ29udGV4dDogS2VybmVsSW52b2NhdGlvbkNvbnRleHQpIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZSA9IGNvbnNvbGU7XHJcbiAgICAgICAgY29uc29sZSA9IDxDb25zb2xlPjxhbnk+dGhpcztcclxuICAgIH1cclxuXHJcbiAgICBhc3NlcnQodmFsdWU6IGFueSwgbWVzc2FnZT86IHN0cmluZywgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuYXNzZXJ0KHZhbHVlLCBtZXNzYWdlLCBvcHRpb25hbFBhcmFtcyk7XHJcbiAgICB9XHJcbiAgICBjbGVhcigpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5jbGVhcigpO1xyXG4gICAgfVxyXG4gICAgY291bnQobGFiZWw/OiBhbnkpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5jb3VudChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBjb3VudFJlc2V0KGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuY291bnRSZXNldChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBkZWJ1ZyhtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5kZWJ1ZyhtZXNzYWdlLCBvcHRpb25hbFBhcmFtcyk7XHJcbiAgICB9XHJcbiAgICBkaXIob2JqOiBhbnksIG9wdGlvbnM/OiBJbnNwZWN0T3B0aW9ucyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmRpcihvYmosIG9wdGlvbnMpO1xyXG4gICAgfVxyXG4gICAgZGlyeG1sKC4uLmRhdGE6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuZGlyeG1sKGRhdGEpO1xyXG4gICAgfVxyXG4gICAgZXJyb3IobWVzc2FnZT86IGFueSwgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5yZWRpcmVjdEFuZFB1Ymxpc2godGhpcy5vcmlnaW5hbENvbnNvbGUuZXJyb3IsIC4uLlttZXNzYWdlLCAuLi5vcHRpb25hbFBhcmFtc10pO1xyXG4gICAgfVxyXG5cclxuICAgIGdyb3VwKC4uLmxhYmVsOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmdyb3VwKGxhYmVsKTtcclxuICAgIH1cclxuICAgIGdyb3VwQ29sbGFwc2VkKC4uLmxhYmVsOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmdyb3VwQ29sbGFwc2VkKGxhYmVsKTtcclxuICAgIH1cclxuICAgIGdyb3VwRW5kKCk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmdyb3VwRW5kKCk7XHJcbiAgICB9XHJcbiAgICBpbmZvKG1lc3NhZ2U/OiBhbnksIC4uLm9wdGlvbmFsUGFyYW1zOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMucmVkaXJlY3RBbmRQdWJsaXNoKHRoaXMub3JpZ2luYWxDb25zb2xlLmluZm8sIC4uLlttZXNzYWdlLCAuLi5vcHRpb25hbFBhcmFtc10pO1xyXG4gICAgfVxyXG4gICAgbG9nKG1lc3NhZ2U/OiBhbnksIC4uLm9wdGlvbmFsUGFyYW1zOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMucmVkaXJlY3RBbmRQdWJsaXNoKHRoaXMub3JpZ2luYWxDb25zb2xlLmxvZywgLi4uW21lc3NhZ2UsIC4uLm9wdGlvbmFsUGFyYW1zXSk7XHJcbiAgICB9XHJcblxyXG4gICAgdGFibGUodGFidWxhckRhdGE6IGFueSwgcHJvcGVydGllcz86IHN0cmluZ1tdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUudGFibGUodGFidWxhckRhdGEsIHByb3BlcnRpZXMpO1xyXG4gICAgfVxyXG4gICAgdGltZShsYWJlbD86IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnRpbWUobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgdGltZUVuZChsYWJlbD86IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnRpbWVFbmQobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgdGltZUxvZyhsYWJlbD86IHN0cmluZywgLi4uZGF0YTogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS50aW1lTG9nKGxhYmVsLCBkYXRhKTtcclxuICAgIH1cclxuICAgIHRpbWVTdGFtcChsYWJlbD86IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnRpbWVTdGFtcChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICB0cmFjZShtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLnJlZGlyZWN0QW5kUHVibGlzaCh0aGlzLm9yaWdpbmFsQ29uc29sZS50cmFjZSwgLi4uW21lc3NhZ2UsIC4uLm9wdGlvbmFsUGFyYW1zXSk7XHJcbiAgICB9XHJcbiAgICB3YXJuKG1lc3NhZ2U/OiBhbnksIC4uLm9wdGlvbmFsUGFyYW1zOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLndhcm4obWVzc2FnZSwgb3B0aW9uYWxQYXJhbXMpO1xyXG4gICAgfVxyXG5cclxuICAgIHByb2ZpbGUobGFiZWw/OiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5wcm9maWxlKGxhYmVsKTtcclxuICAgIH1cclxuICAgIHByb2ZpbGVFbmQobGFiZWw/OiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5wcm9maWxlRW5kKGxhYmVsKTtcclxuICAgIH1cclxuXHJcbiAgICBkaXNwb3NlKCk6IHZvaWQge1xyXG4gICAgICAgIGNvbnNvbGUgPSB0aGlzLm9yaWdpbmFsQ29uc29sZTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIHJlZGlyZWN0QW5kUHVibGlzaCh0YXJnZXQ6ICguLi5hcmdzOiBhbnlbXSkgPT4gdm9pZCwgLi4uYXJnczogYW55W10pIHtcclxuICAgICAgICB0YXJnZXQoLi4uYXJncyk7XHJcbiAgICAgICAgdGhpcy5wdWJsaXNoQXJnc0FzRXZlbnRzKC4uLmFyZ3MpO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgcHVibGlzaEFyZ3NBc0V2ZW50cyguLi5hcmdzOiBhbnlbXSkge1xyXG4gICAgICAgIGZvciAoY29uc3QgYXJnIG9mIGFyZ3MpIHtcclxuICAgICAgICAgICAgbGV0IG1pbWVUeXBlOiBzdHJpbmc7XHJcbiAgICAgICAgICAgIGxldCB2YWx1ZTogc3RyaW5nO1xyXG4gICAgICAgICAgICBpZiAodHlwZW9mIGFyZyAhPT0gJ29iamVjdCcgJiYgIUFycmF5LmlzQXJyYXkoYXJnKSkge1xyXG4gICAgICAgICAgICAgICAgbWltZVR5cGUgPSAndGV4dC9wbGFpbic7XHJcbiAgICAgICAgICAgICAgICB2YWx1ZSA9IGFyZz8udG9TdHJpbmcoKTtcclxuICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgIG1pbWVUeXBlID0gJ2FwcGxpY2F0aW9uL2pzb24nO1xyXG4gICAgICAgICAgICAgICAgdmFsdWUgPSBKU09OLnN0cmluZ2lmeShhcmcpO1xyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICBjb25zdCBkaXNwbGF5ZWRWYWx1ZTogY29udHJhY3RzLkRpc3BsYXllZFZhbHVlUHJvZHVjZWQgPSB7XHJcbiAgICAgICAgICAgICAgICBmb3JtYXR0ZWRWYWx1ZXM6IFtcclxuICAgICAgICAgICAgICAgICAgICB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIG1pbWVUeXBlLFxyXG4gICAgICAgICAgICAgICAgICAgICAgICB2YWx1ZSxcclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICBdXHJcbiAgICAgICAgICAgIH07XHJcbiAgICAgICAgICAgIGNvbnN0IGV2ZW50RW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlID0ge1xyXG4gICAgICAgICAgICAgICAgZXZlbnRUeXBlOiBjb250cmFjdHMuRGlzcGxheWVkVmFsdWVQcm9kdWNlZFR5cGUsXHJcbiAgICAgICAgICAgICAgICBldmVudDogZGlzcGxheWVkVmFsdWUsXHJcbiAgICAgICAgICAgICAgICBjb21tYW5kOiB0aGlzLmtlcm5lbEludm9jYXRpb25Db250ZXh0LmNvbW1hbmRFbnZlbG9wZVxyXG4gICAgICAgICAgICB9O1xyXG5cclxuICAgICAgICAgICAgdGhpcy5rZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5wdWJsaXNoKGV2ZW50RW52ZWxvcGUpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufSIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSBcIi4vY29udHJhY3RzXCI7XHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vZ2VuZXJpY0NoYW5uZWxcIjtcclxuaW1wb3J0ICogYXMga2VybmVsIGZyb20gXCIuL2tlcm5lbFwiO1xyXG5cclxuZXhwb3J0IGNsYXNzIEh0bWxLZXJuZWwgZXh0ZW5kcyBrZXJuZWwuS2VybmVsIHtcclxuICAgIGNvbnN0cnVjdG9yKGtlcm5lbE5hbWU/OiBzdHJpbmcsIHByaXZhdGUgcmVhZG9ubHkgaHRtbEZyYWdtZW50UHJvY2Vzc29yPzogKGh0bWxGcmFnbWVudDogc3RyaW5nKSA9PiBQcm9taXNlPHZvaWQ+LCBsYW5ndWFnZU5hbWU/OiBzdHJpbmcsIGxhbmd1YWdlVmVyc2lvbj86IHN0cmluZykge1xyXG4gICAgICAgIHN1cGVyKGtlcm5lbE5hbWUgPz8gXCJodG1sXCIsIGxhbmd1YWdlTmFtZSA/PyBcIkhUTUxcIik7XHJcbiAgICAgICAgaWYgKCF0aGlzLmh0bWxGcmFnbWVudFByb2Nlc3Nvcikge1xyXG4gICAgICAgICAgICB0aGlzLmh0bWxGcmFnbWVudFByb2Nlc3NvciA9IGRvbUh0bWxGcmFnbWVudFByb2Nlc3NvcjtcclxuICAgICAgICB9XHJcbiAgICAgICAgdGhpcy5yZWdpc3RlckNvbW1hbmRIYW5kbGVyKHsgY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5TdWJtaXRDb2RlVHlwZSwgaGFuZGxlOiBpbnZvY2F0aW9uID0+IHRoaXMuaGFuZGxlU3VibWl0Q29kZShpbnZvY2F0aW9uKSB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGFzeW5jIGhhbmRsZVN1Ym1pdENvZGUoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHN1Ym1pdENvZGUgPSA8Y29udHJhY3RzLlN1Ym1pdENvZGU+aW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZDtcclxuICAgICAgICBjb25zdCBjb2RlID0gc3VibWl0Q29kZS5jb2RlO1xyXG5cclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLkNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlLCBldmVudDogeyBjb2RlIH0sIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlIH0pO1xyXG5cclxuICAgICAgICBpZiAoIXRoaXMuaHRtbEZyYWdtZW50UHJvY2Vzc29yKSB7XHJcbiAgICAgICAgICAgIHRocm93IG5ldyBFcnJvcihcIk5vIEhUTUwgZnJhZ21lbnQgcHJvY2Vzc29yIHJlZ2lzdGVyZWRcIik7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICBhd2FpdCB0aGlzLmh0bWxGcmFnbWVudFByb2Nlc3Nvcihjb2RlKTtcclxuICAgICAgICB9IGNhdGNoIChlKSB7XHJcbiAgICAgICAgICAgIHRocm93IGU7Ly8/XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gZG9tSHRtbEZyYWdtZW50UHJvY2Vzc29yKGh0bWxGcmFnbWVudDogc3RyaW5nLCBjb25maWd1cmF0aW9uPzoge1xyXG4gICAgY29udGFpbmVyRmFjdG9yeT86ICgpID0+IEhUTUxEaXZFbGVtZW50LFxyXG4gICAgZWxlbWVudFRvT2JzZXJ2ZT86ICgpID0+IEhUTUxFbGVtZW50LFxyXG4gICAgYWRkVG9Eb20/OiAoZWxlbWVudDogSFRNTEVsZW1lbnQpID0+IHZvaWQsXHJcbiAgICBtdXRhdGlvbk9ic2VydmVyRmFjdG9yeT86IChjYWxsYmFjazogTXV0YXRpb25DYWxsYmFjaykgPT4gTXV0YXRpb25PYnNlcnZlclxyXG59KTogUHJvbWlzZTx2b2lkPiB7XHJcblxyXG4gICAgY29uc3QgZmFjdG9yeTogKCkgPT4gSFRNTERpdkVsZW1lbnQgPSBjb25maWd1cmF0aW9uPy5jb250YWluZXJGYWN0b3J5ID8/ICgoKSA9PiBkb2N1bWVudC5jcmVhdGVFbGVtZW50KFwiZGl2XCIpKTtcclxuICAgIGNvbnN0IGVsZW1lbnRUb09ic2VydmU6ICgpID0+IEhUTUxFbGVtZW50ID0gY29uZmlndXJhdGlvbj8uZWxlbWVudFRvT2JzZXJ2ZSA/PyAoKCkgPT4gZG9jdW1lbnQuYm9keSk7XHJcbiAgICBjb25zdCBhZGRUb0RvbTogKGVsZW1lbnQ6IEhUTUxFbGVtZW50KSA9PiB2b2lkID0gY29uZmlndXJhdGlvbj8uYWRkVG9Eb20gPz8gKChlbGVtZW50KSA9PiBkb2N1bWVudC5ib2R5LmFwcGVuZENoaWxkKGVsZW1lbnQpKTtcclxuICAgIGNvbnN0IG11dGF0aW9uT2JzZXJ2ZXJGYWN0b3J5ID0gY29uZmlndXJhdGlvbj8ubXV0YXRpb25PYnNlcnZlckZhY3RvcnkgPz8gKGNhbGxiYWNrID0+IG5ldyBNdXRhdGlvbk9ic2VydmVyKGNhbGxiYWNrKSk7XHJcblxyXG4gICAgbGV0IGNvbnRhaW5lciA9IGZhY3RvcnkoKTtcclxuXHJcbiAgICBpZiAoIWNvbnRhaW5lci5pZCkge1xyXG4gICAgICAgIGNvbnRhaW5lci5pZCA9IFwiaHRtbF9rZXJuZWxfY29udGFpbmVyXCIgKyBNYXRoLmZsb29yKE1hdGgucmFuZG9tKCkgKiAxMDAwMDAwKTtcclxuICAgIH1cclxuXHJcbiAgICBjb250YWluZXIuaW5uZXJIVE1MID0gaHRtbEZyYWdtZW50O1xyXG4gICAgY29uc3QgY29tcGxldGlvblByb21pc2UgPSBuZXcgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U8dm9pZD4oKTtcclxuICAgIGNvbnN0IG11dGF0aW9uT2JzZXJ2ZXIgPSBtdXRhdGlvbk9ic2VydmVyRmFjdG9yeSgobXV0YXRpb25zOiBNdXRhdGlvblJlY29yZFtdLCBvYnNlcnZlcjogTXV0YXRpb25PYnNlcnZlcikgPT4ge1xyXG5cclxuICAgICAgICBmb3IgKGNvbnN0IG11dGF0aW9uIG9mIG11dGF0aW9ucykge1xyXG4gICAgICAgICAgICBpZiAobXV0YXRpb24udHlwZSA9PT0gXCJjaGlsZExpc3RcIikge1xyXG5cclxuICAgICAgICAgICAgICAgIGNvbnN0IG5vZGVzID0gQXJyYXkuZnJvbShtdXRhdGlvbi5hZGRlZE5vZGVzKTtcclxuICAgICAgICAgICAgICAgIGZvciAoY29uc3QgYWRkZWROb2RlIG9mIG5vZGVzKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgZWxlbWVudCA9IGFkZGVkTm9kZSBhcyBIVE1MRGl2RWxlbWVudDtcclxuICAgICAgICAgICAgICAgICAgICBlbGVtZW50LmlkOy8vP1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnRhaW5lci5pZDsvLz9cclxuICAgICAgICAgICAgICAgICAgICBpZiAoZWxlbWVudD8uaWQgPT09IGNvbnRhaW5lci5pZCkgey8vP1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBjb21wbGV0aW9uUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIG11dGF0aW9uT2JzZXJ2ZXIuZGlzY29ubmVjdCgpO1xyXG5cclxuICAgICAgICAgICAgICAgICAgICAgICAgcmV0dXJuO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICB9KTtcclxuXHJcbiAgICBtdXRhdGlvbk9ic2VydmVyLm9ic2VydmUoZWxlbWVudFRvT2JzZXJ2ZSgpLCB7IGNoaWxkTGlzdDogdHJ1ZSwgc3VidHJlZTogdHJ1ZSB9KTtcclxuICAgIGFkZFRvRG9tKGNvbnRhaW5lcik7XHJcbiAgICByZXR1cm4gY29tcGxldGlvblByb21pc2UucHJvbWlzZTtcclxuXHJcbn0iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBDb25zb2xlQ2FwdHVyZSB9IGZyb20gXCIuL2NvbnNvbGVDYXB0dXJlXCI7XHJcbmltcG9ydCAqIGFzIGtlcm5lbCBmcm9tIFwiLi9rZXJuZWxcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcblxyXG5leHBvcnQgY2xhc3MgSmF2YXNjcmlwdEtlcm5lbCBleHRlbmRzIGtlcm5lbC5LZXJuZWwge1xyXG4gICAgcHJpdmF0ZSBzdXBwcmVzc2VkTG9jYWxzOiBTZXQ8c3RyaW5nPjtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcihuYW1lPzogc3RyaW5nKSB7XHJcbiAgICAgICAgc3VwZXIobmFtZSA/PyBcImphdmFzY3JpcHRcIiwgXCJKYXZhc2NyaXB0XCIpO1xyXG4gICAgICAgIHRoaXMuc3VwcHJlc3NlZExvY2FscyA9IG5ldyBTZXQ8c3RyaW5nPih0aGlzLmFsbExvY2FsVmFyaWFibGVOYW1lcygpKTtcclxuICAgICAgICB0aGlzLnJlZ2lzdGVyQ29tbWFuZEhhbmRsZXIoeyBjb21tYW5kVHlwZTogY29udHJhY3RzLlN1Ym1pdENvZGVUeXBlLCBoYW5kbGU6IGludm9jYXRpb24gPT4gdGhpcy5oYW5kbGVTdWJtaXRDb2RlKGludm9jYXRpb24pIH0pO1xyXG4gICAgICAgIHRoaXMucmVnaXN0ZXJDb21tYW5kSGFuZGxlcih7IGNvbW1hbmRUeXBlOiBjb250cmFjdHMuUmVxdWVzdFZhbHVlSW5mb3NUeXBlLCBoYW5kbGU6IGludm9jYXRpb24gPT4gdGhpcy5oYW5kbGVSZXF1ZXN0VmFsdWVJbmZvcyhpbnZvY2F0aW9uKSB9KTtcclxuICAgICAgICB0aGlzLnJlZ2lzdGVyQ29tbWFuZEhhbmRsZXIoeyBjb21tYW5kVHlwZTogY29udHJhY3RzLlJlcXVlc3RWYWx1ZVR5cGUsIGhhbmRsZTogaW52b2NhdGlvbiA9PiB0aGlzLmhhbmRsZVJlcXVlc3RWYWx1ZShpbnZvY2F0aW9uKSB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGFzeW5jIGhhbmRsZVN1Ym1pdENvZGUoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHN1Ym1pdENvZGUgPSA8Y29udHJhY3RzLlN1Ym1pdENvZGU+aW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZDtcclxuICAgICAgICBjb25zdCBjb2RlID0gc3VibWl0Q29kZS5jb2RlO1xyXG5cclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLkNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlLCBldmVudDogeyBjb2RlIH0sIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlIH0pO1xyXG5cclxuICAgICAgICBsZXQgY2FwdHVyZTogY29udHJhY3RzLkRpc3Bvc2FibGUgfCB1bmRlZmluZWQgPSBuZXcgQ29uc29sZUNhcHR1cmUoaW52b2NhdGlvbi5jb250ZXh0KTtcclxuICAgICAgICBsZXQgcmVzdWx0OiBhbnkgPSB1bmRlZmluZWQ7XHJcblxyXG4gICAgICAgIHRyeSB7XHJcbiAgICAgICAgICAgIGNvbnN0IEFzeW5jRnVuY3Rpb24gPSBldmFsKGBPYmplY3QuZ2V0UHJvdG90eXBlT2YoYXN5bmMgZnVuY3Rpb24oKXt9KS5jb25zdHJ1Y3RvcmApO1xyXG4gICAgICAgICAgICBjb25zdCBldmFsdWF0b3IgPSBBc3luY0Z1bmN0aW9uKFwiY29uc29sZVwiLCBjb2RlKTtcclxuICAgICAgICAgICAgcmVzdWx0ID0gYXdhaXQgZXZhbHVhdG9yKGNhcHR1cmUpO1xyXG4gICAgICAgICAgICBpZiAocmVzdWx0ICE9PSB1bmRlZmluZWQpIHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGZvcm1hdHRlZFZhbHVlID0gZm9ybWF0VmFsdWUocmVzdWx0LCAnYXBwbGljYXRpb24vanNvbicpO1xyXG4gICAgICAgICAgICAgICAgY29uc3QgZXZlbnQ6IGNvbnRyYWN0cy5SZXR1cm5WYWx1ZVByb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICAgICAgICAgIGZvcm1hdHRlZFZhbHVlczogW2Zvcm1hdHRlZFZhbHVlXVxyXG4gICAgICAgICAgICAgICAgfTtcclxuICAgICAgICAgICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKHsgZXZlbnRUeXBlOiBjb250cmFjdHMuUmV0dXJuVmFsdWVQcm9kdWNlZFR5cGUsIGV2ZW50LCBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSB9KTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0gY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgY2FwdHVyZS5kaXNwb3NlKCk7XHJcbiAgICAgICAgICAgIGNhcHR1cmUgPSB1bmRlZmluZWQ7XHJcblxyXG4gICAgICAgICAgICB0aHJvdyBlOy8vP1xyXG4gICAgICAgIH1cclxuICAgICAgICBmaW5hbGx5IHtcclxuICAgICAgICAgICAgaWYgKGNhcHR1cmUpIHtcclxuICAgICAgICAgICAgICAgIGNhcHR1cmUuZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgaGFuZGxlUmVxdWVzdFZhbHVlSW5mb3MoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHZhbHVlSW5mb3M6IGNvbnRyYWN0cy5LZXJuZWxWYWx1ZUluZm9bXSA9IHRoaXMuYWxsTG9jYWxWYXJpYWJsZU5hbWVzKCkuZmlsdGVyKHYgPT4gIXRoaXMuc3VwcHJlc3NlZExvY2Fscy5oYXModikpLm1hcCh2ID0+ICh7IG5hbWU6IHYgfSkpO1xyXG4gICAgICAgIGNvbnN0IGV2ZW50OiBjb250cmFjdHMuVmFsdWVJbmZvc1Byb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICB2YWx1ZUluZm9zXHJcbiAgICAgICAgfTtcclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLlZhbHVlSW5mb3NQcm9kdWNlZFR5cGUsIGV2ZW50LCBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSB9KTtcclxuICAgICAgICByZXR1cm4gUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBoYW5kbGVSZXF1ZXN0VmFsdWUoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHJlcXVlc3RWYWx1ZSA9IDxjb250cmFjdHMuUmVxdWVzdFZhbHVlPmludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQ7XHJcbiAgICAgICAgY29uc3QgcmF3VmFsdWUgPSB0aGlzLmdldExvY2FsVmFyaWFibGUocmVxdWVzdFZhbHVlLm5hbWUpO1xyXG4gICAgICAgIGNvbnN0IGZvcm1hdHRlZFZhbHVlID0gZm9ybWF0VmFsdWUocmF3VmFsdWUsIHJlcXVlc3RWYWx1ZS5taW1lVHlwZSB8fCAnYXBwbGljYXRpb24vanNvbicpO1xyXG4gICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHJldHVybmluZyAke0pTT04uc3RyaW5naWZ5KGZvcm1hdHRlZFZhbHVlKX0gZm9yICR7cmVxdWVzdFZhbHVlLm5hbWV9YCk7XHJcbiAgICAgICAgY29uc3QgZXZlbnQ6IGNvbnRyYWN0cy5WYWx1ZVByb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICBuYW1lOiByZXF1ZXN0VmFsdWUubmFtZSxcclxuICAgICAgICAgICAgZm9ybWF0dGVkVmFsdWVcclxuICAgICAgICB9O1xyXG4gICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKHsgZXZlbnRUeXBlOiBjb250cmFjdHMuVmFsdWVQcm9kdWNlZFR5cGUsIGV2ZW50LCBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSB9KTtcclxuICAgICAgICByZXR1cm4gUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBhbGxMb2NhbFZhcmlhYmxlTmFtZXMoKTogc3RyaW5nW10ge1xyXG4gICAgICAgIGNvbnN0IHJlc3VsdDogc3RyaW5nW10gPSBbXTtcclxuICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICBmb3IgKGNvbnN0IGtleSBpbiBnbG9iYWxUaGlzKSB7XHJcbiAgICAgICAgICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICAgICAgICAgIGlmICh0eXBlb2YgKDxhbnk+Z2xvYmFsVGhpcylba2V5XSAhPT0gJ2Z1bmN0aW9uJykge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICByZXN1bHQucHVzaChrZXkpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIH0gY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihgZXJyb3IgZ2V0dGluZyB2YWx1ZSBmb3IgJHtrZXl9IDogJHtlfWApO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihgZXJyb3Igc2Nhbm5pbmcgZ2xvYmxhIHZhcmlhYmxlcyA6ICR7ZX1gKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiByZXN1bHQ7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBnZXRMb2NhbFZhcmlhYmxlKG5hbWU6IHN0cmluZyk6IGFueSB7XHJcbiAgICAgICAgcmV0dXJuICg8YW55Pmdsb2JhbFRoaXMpW25hbWVdO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gZm9ybWF0VmFsdWUoYXJnOiBhbnksIG1pbWVUeXBlOiBzdHJpbmcpOiBjb250cmFjdHMuRm9ybWF0dGVkVmFsdWUge1xyXG4gICAgbGV0IHZhbHVlOiBzdHJpbmc7XHJcblxyXG4gICAgc3dpdGNoIChtaW1lVHlwZSkge1xyXG4gICAgICAgIGNhc2UgJ3RleHQvcGxhaW4nOlxyXG4gICAgICAgICAgICB2YWx1ZSA9IGFyZz8udG9TdHJpbmcoKSB8fCAndW5kZWZpbmVkJztcclxuICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgY2FzZSAnYXBwbGljYXRpb24vanNvbic6XHJcbiAgICAgICAgICAgIHZhbHVlID0gSlNPTi5zdHJpbmdpZnkoYXJnKTtcclxuICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgZGVmYXVsdDpcclxuICAgICAgICAgICAgdGhyb3cgbmV3IEVycm9yKGB1bnN1cHBvcnRlZCBtaW1lIHR5cGU6ICR7bWltZVR5cGV9YCk7XHJcbiAgICB9XHJcblxyXG4gICAgcmV0dXJuIHtcclxuICAgICAgICBtaW1lVHlwZSxcclxuICAgICAgICB2YWx1ZSxcclxuICAgIH07XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vZ2VuZXJpY0NoYW5uZWxcIjtcclxuaW1wb3J0IHsgSUtlcm5lbENvbW1hbmRIYW5kbGVyLCBJS2VybmVsQ29tbWFuZEludm9jYXRpb24sIEtlcm5lbCB9IGZyb20gXCIuL2tlcm5lbFwiO1xyXG5cclxuZXhwb3J0IGNsYXNzIFByb3h5S2VybmVsIGV4dGVuZHMgS2VybmVsIHtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcihvdmVycmlkZSByZWFkb25seSBuYW1lOiBzdHJpbmcsIHByaXZhdGUgcmVhZG9ubHkgY2hhbm5lbDogY29udHJhY3RzLktlcm5lbENvbW1hbmRBbmRFdmVudENoYW5uZWwpIHtcclxuICAgICAgICBzdXBlcihuYW1lKTtcclxuICAgIH1cclxuICAgIG92ZXJyaWRlIGdldENvbW1hbmRIYW5kbGVyKGNvbW1hbmRUeXBlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZFR5cGUpOiBJS2VybmVsQ29tbWFuZEhhbmRsZXIgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB7XHJcbiAgICAgICAgICAgIGNvbW1hbmRUeXBlLFxyXG4gICAgICAgICAgICBoYW5kbGU6IChpbnZvY2F0aW9uKSA9PiB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4gdGhpcy5fY29tbWFuZEhhbmRsZXIoaW52b2NhdGlvbik7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9O1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgYXN5bmMgX2NvbW1hbmRIYW5kbGVyKGNvbW1hbmRJbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCB0b2tlbiA9IGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS50b2tlbjtcclxuICAgICAgICBjb25zdCBjb21wbGV0aW9uU291cmNlID0gbmV3IFByb21pc2VDb21wbGV0aW9uU291cmNlPGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPigpO1xyXG4gICAgICAgIGxldCBzdWIgPSB0aGlzLmNoYW5uZWwuc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMoKGVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSkgPT4ge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBwcm94eSAke3RoaXMubmFtZX0gZ290IGV2ZW50ICR7SlNPTi5zdHJpbmdpZnkoZW52ZWxvcGUpfWApO1xyXG4gICAgICAgICAgICBpZiAoZW52ZWxvcGUuY29tbWFuZCEudG9rZW4gPT09IHRva2VuKSB7XHJcbiAgICAgICAgICAgICAgICBzd2l0Y2ggKGVudmVsb3BlLmV2ZW50VHlwZSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGNhc2UgY29udHJhY3RzLkNvbW1hbmRGYWlsZWRUeXBlOlxyXG4gICAgICAgICAgICAgICAgICAgIGNhc2UgY29udHJhY3RzLkNvbW1hbmRTdWNjZWVkZWRUeXBlOlxyXG4gICAgICAgICAgICAgICAgICAgICAgICBpZiAoZW52ZWxvcGUuY29tbWFuZCEuaWQgPT09IGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5pZCkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY29tcGxldGlvblNvdXJjZS5yZXNvbHZlKGVudmVsb3BlKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNvbW1hbmRJbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaChlbnZlbG9wZSk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICAgICAgZGVmYXVsdDpcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29tbWFuZEludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKGVudmVsb3BlKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgdHJ5IHtcclxuICAgICAgICAgICAgaWYgKCFjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSB8fCAhY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQub3JpZ2luVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBrZXJuZWxJbmZvID0gdGhpcy5wYXJlbnRLZXJuZWw/Lmhvc3Q/LnRyeUdldEtlcm5lbEluZm8odGhpcyk7XHJcbiAgICAgICAgICAgICAgICBpZiAoa2VybmVsSW5mbykge1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLm9yaWdpblVyaSA/Pz0ga2VybmVsSW5mby51cmk7XHJcbiAgICAgICAgICAgICAgICAgICAgY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25VcmkgPz89IGtlcm5lbEluZm8ucmVtb3RlVXJpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICB0aGlzLmNoYW5uZWwuc3VibWl0Q29tbWFuZChjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBwcm94eSAke3RoaXMubmFtZX0gYWJvdXQgdG8gYXdhaXQgd2l0aCB0b2tlbiAke3Rva2VufWApO1xyXG4gICAgICAgICAgICBjb25zdCBlbnZlbnRFbnZlbG9wZSA9IGF3YWl0IGNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxuICAgICAgICAgICAgaWYgKGVudmVudEVudmVsb3BlLmV2ZW50VHlwZSA9PT0gY29udHJhY3RzLkNvbW1hbmRGYWlsZWRUeXBlKSB7XHJcbiAgICAgICAgICAgICAgICBjb21tYW5kSW52b2NhdGlvbi5jb250ZXh0LmZhaWwoKDxjb250cmFjdHMuQ29tbWFuZEZhaWxlZD5lbnZlbnRFbnZlbG9wZS5ldmVudCkubWVzc2FnZSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgcHJveHkgJHt0aGlzLm5hbWV9IGRvbmUgYXdhaXRpbmcgd2l0aCB0b2tlbiAke3Rva2VufWApO1xyXG4gICAgICAgIH1cclxuICAgICAgICBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICBjb21tYW5kSW52b2NhdGlvbi5jb250ZXh0LmZhaWwoKDxhbnk+ZSkubWVzc2FnZSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGZpbmFsbHkge1xyXG4gICAgICAgICAgICBzdWIuZGlzcG9zZSgpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgQ29tcG9zaXRlS2VybmVsIH0gZnJvbSAnLi9jb21wb3NpdGVLZXJuZWwnO1xyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSAnLi9jb250cmFjdHMnO1xyXG5pbXBvcnQgeyBLZXJuZWwgfSBmcm9tICcuL2tlcm5lbCc7XHJcbmltcG9ydCB7IFByb3h5S2VybmVsIH0gZnJvbSAnLi9wcm94eUtlcm5lbCc7XHJcbmltcG9ydCB7IExvZ2dlciB9IGZyb20gJy4vbG9nZ2VyJztcclxuaW1wb3J0IHsgS2VybmVsU2NoZWR1bGVyIH0gZnJvbSAnLi9rZXJuZWxTY2hlZHVsZXInO1xyXG5cclxuZXhwb3J0IGNsYXNzIEtlcm5lbEhvc3Qge1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfcmVtb3RlVXJpVG9LZXJuZWwgPSBuZXcgTWFwPHN0cmluZywgS2VybmVsPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfdXJpVG9LZXJuZWwgPSBuZXcgTWFwPHN0cmluZywgS2VybmVsPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfa2VybmVsVG9LZXJuZWxJbmZvID0gbmV3IE1hcDxLZXJuZWwsIGNvbnRyYWN0cy5LZXJuZWxJbmZvPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfdXJpOiBzdHJpbmc7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9zY2hlZHVsZXI6IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPjtcclxuXHJcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIHJlYWRvbmx5IF9rZXJuZWw6IENvbXBvc2l0ZUtlcm5lbCwgcHJpdmF0ZSByZWFkb25seSBfY2hhbm5lbDogY29udHJhY3RzLktlcm5lbENvbW1hbmRBbmRFdmVudENoYW5uZWwsIGhvc3RVcmk6IHN0cmluZykge1xyXG4gICAgICAgIHRoaXMuX3VyaSA9IGhvc3RVcmkgfHwgXCJrZXJuZWw6Ly92c2NvZGVcIjtcclxuICAgICAgICB0aGlzLl9rZXJuZWwuaG9zdCA9IHRoaXM7XHJcbiAgICAgICAgdGhpcy5fc2NoZWR1bGVyID0gbmV3IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPigpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlHZXRLZXJuZWxCeVJlbW90ZVVyaShyZW1vdGVVcmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX3JlbW90ZVVyaVRvS2VybmVsLmdldChyZW1vdGVVcmkpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlnZXRLZXJuZWxCeU9yaWdpblVyaShvcmlnaW5Vcmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX3VyaVRvS2VybmVsLmdldChvcmlnaW5VcmkpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlHZXRLZXJuZWxJbmZvKGtlcm5lbDogS2VybmVsKTogY29udHJhY3RzLktlcm5lbEluZm8gfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9rZXJuZWxUb0tlcm5lbEluZm8uZ2V0KGtlcm5lbCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGFkZEtlcm5lbEluZm8oa2VybmVsOiBLZXJuZWwsIGtlcm5lbEluZm86IGNvbnRyYWN0cy5LZXJuZWxJbmZvKSB7XHJcblxyXG4gICAgICAgIGtlcm5lbEluZm8udXJpID0gYCR7dGhpcy5fdXJpfS8ke2tlcm5lbC5uYW1lfWA7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsVG9LZXJuZWxJbmZvLnNldChrZXJuZWwsIGtlcm5lbEluZm8pO1xyXG4gICAgICAgIHRoaXMuX3VyaVRvS2VybmVsLnNldChrZXJuZWxJbmZvLnVyaSwga2VybmVsKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZ2V0S2VybmVsKGtlcm5lbENvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IEtlcm5lbCB7XHJcblxyXG4gICAgICAgIGlmIChrZXJuZWxDb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSkge1xyXG4gICAgICAgICAgICBsZXQgZnJvbURlc3RpbmF0aW9uVXJpID0gdGhpcy5fdXJpVG9LZXJuZWwuZ2V0KGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpLnRvTG93ZXJDYXNlKCkpO1xyXG4gICAgICAgICAgICBpZiAoZnJvbURlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBLZXJuZWwgJHtmcm9tRGVzdGluYXRpb25VcmkubmFtZX0gZm91bmQgZm9yIGRlc3RpbmF0aW9uIHVyaSAke2tlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpfWApO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZyb21EZXN0aW5hdGlvblVyaTtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgZnJvbURlc3RpbmF0aW9uVXJpID0gdGhpcy5fcmVtb3RlVXJpVG9LZXJuZWwuZ2V0KGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpLnRvTG93ZXJDYXNlKCkpO1xyXG4gICAgICAgICAgICBpZiAoZnJvbURlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBLZXJuZWwgJHtmcm9tRGVzdGluYXRpb25VcmkubmFtZX0gZm91bmQgZm9yIGRlc3RpbmF0aW9uIHVyaSAke2tlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpfWApO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZyb21EZXN0aW5hdGlvblVyaTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLm9yaWdpblVyaSkge1xyXG4gICAgICAgICAgICBsZXQgZnJvbU9yaWdpblVyaSA9IHRoaXMuX3VyaVRvS2VybmVsLmdldChrZXJuZWxDb21tYW5kRW52ZWxvcGUuY29tbWFuZC5vcmlnaW5VcmkudG9Mb3dlckNhc2UoKSk7XHJcbiAgICAgICAgICAgIGlmIChmcm9tT3JpZ2luVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBLZXJuZWwgJHtmcm9tT3JpZ2luVXJpLm5hbWV9IGZvdW5kIGZvciBvcmlnaW4gdXJpICR7a2VybmVsQ29tbWFuZEVudmVsb3BlLmNvbW1hbmQub3JpZ2luVXJpfWApO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZyb21PcmlnaW5Vcmk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYFVzaW5nIEtlcm5lbCAke3RoaXMuX2tlcm5lbC5uYW1lfWApO1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9rZXJuZWw7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHJlZ2lzdGVyUmVtb3RlVXJpRm9yUHJveHkocHJveHlMb2NhbEtlcm5lbE5hbWU6IHN0cmluZywgcmVtb3RlVXJpOiBzdHJpbmcpIHtcclxuICAgICAgICBjb25zdCBrZXJuZWwgPSB0aGlzLl9rZXJuZWwuZmluZEtlcm5lbEJ5TmFtZShwcm94eUxvY2FsS2VybmVsTmFtZSk7XHJcbiAgICAgICAgaWYgKCEoa2VybmVsIGFzIFByb3h5S2VybmVsKSkge1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoYEtlcm5lbCAke3Byb3h5TG9jYWxLZXJuZWxOYW1lfSBpcyBub3QgYSBwcm94eSBrZXJuZWxgKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGNvbnN0IGtlcm5lbGluZm8gPSB0aGlzLl9rZXJuZWxUb0tlcm5lbEluZm8uZ2V0KGtlcm5lbCEpO1xyXG5cclxuICAgICAgICBpZiAoIWtlcm5lbGluZm8pIHtcclxuICAgICAgICAgICAgdGhyb3cgbmV3IEVycm9yKFwia2VybmVsaW5mbyBub3QgZm91bmRcIik7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGlmIChrZXJuZWxpbmZvPy5yZW1vdGVVcmkpIHtcclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgUmVtb3ZpbmcgcmVtb3RlIHVyaSAke2tlcm5lbGluZm8ucmVtb3RlVXJpfSBmb3IgcHJveHkga2VybmVsICR7a2VybmVsaW5mby5sb2NhbE5hbWV9YCk7XHJcbiAgICAgICAgICAgIHRoaXMuX3JlbW90ZVVyaVRvS2VybmVsLmRlbGV0ZShrZXJuZWxpbmZvLnJlbW90ZVVyaS50b0xvd2VyQ2FzZSgpKTtcclxuICAgICAgICB9XHJcbiAgICAgICAga2VybmVsaW5mby5yZW1vdGVVcmkgPSByZW1vdGVVcmk7XHJcblxyXG4gICAgICAgIGlmIChrZXJuZWwpIHtcclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgUmVnaXN0ZXJpbmcgcmVtb3RlIHVyaSAke3JlbW90ZVVyaX0gZm9yIHByb3h5IGtlcm5lbCAke2tlcm5lbGluZm8ubG9jYWxOYW1lfWApO1xyXG4gICAgICAgICAgICB0aGlzLl9yZW1vdGVVcmlUb0tlcm5lbC5zZXQocmVtb3RlVXJpLnRvTG93ZXJDYXNlKCksIGtlcm5lbCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjcmVhdGVQcm94eUtlcm5lbE9uRGVmYXVsdENvbm5lY3RvcihrZXJuZWxJbmZvOiBjb250cmFjdHMuS2VybmVsSW5mbyk6IFByb3h5S2VybmVsIHtcclxuICAgICAgICBjb25zdCBwcm94eUtlcm5lbCA9IG5ldyBQcm94eUtlcm5lbChrZXJuZWxJbmZvLmxvY2FsTmFtZSwgdGhpcy5fY2hhbm5lbCk7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsLmFkZChwcm94eUtlcm5lbCwga2VybmVsSW5mby5hbGlhc2VzKTtcclxuICAgICAgICBpZiAoa2VybmVsSW5mby5yZW1vdGVVcmkpIHtcclxuICAgICAgICAgICAgdGhpcy5yZWdpc3RlclJlbW90ZVVyaUZvclByb3h5KHByb3h5S2VybmVsLm5hbWUsIGtlcm5lbEluZm8ucmVtb3RlVXJpKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIHByb3h5S2VybmVsO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjb25uZWN0KCkge1xyXG4gICAgICAgIHRoaXMuX2NoYW5uZWwuc2V0Q29tbWFuZEhhbmRsZXIoKGtlcm5lbENvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSkgPT4ge1xyXG4gICAgICAgICAgICAvLyBmaXJlIGFuZCBmb3JnZXQgdGhpcyBvbmVcclxuICAgICAgICAgICAgdGhpcy5fc2NoZWR1bGVyLnJ1bkFzeW5jKGtlcm5lbENvbW1hbmRFbnZlbG9wZSwgY29tbWFuZEVudmVsb3BlID0+IHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGtlcm5lbCA9IHRoaXMuZ2V0S2VybmVsKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4ga2VybmVsLnNlbmQoY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgICAgIHJldHVybiBQcm9taXNlLnJlc29sdmUoKTtcclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgdGhpcy5fa2VybmVsLnN1YnNjcmliZVRvS2VybmVsRXZlbnRzKGUgPT4ge1xyXG4gICAgICAgICAgICB0aGlzLl9jaGFubmVsLnB1Ymxpc2hLZXJuZWxFdmVudChlKTtcclxuICAgICAgICB9KTtcclxuICAgIH1cclxufSIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgeyBDb21wb3NpdGVLZXJuZWwgfSBmcm9tIFwiLi9jb21wb3NpdGVLZXJuZWxcIjtcclxuaW1wb3J0IHsgSmF2YXNjcmlwdEtlcm5lbCB9IGZyb20gXCIuL2phdmFzY3JpcHRLZXJuZWxcIjtcclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBIdG1sS2VybmVsIH0gZnJvbSBcIi4vaHRtbEtlcm5lbFwiO1xyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIHNldHVwKGdsb2JhbD86IGFueSkge1xyXG5cclxuICAgIGdsb2JhbCA9IGdsb2JhbCB8fCB3aW5kb3c7XHJcbiAgICBsZXQgY29tcG9zaXRlS2VybmVsID0gbmV3IENvbXBvc2l0ZUtlcm5lbChcImJyb3dzZXJcIik7XHJcblxyXG4gICAgY29uc3QganNLZXJuZWwgPSBuZXcgSmF2YXNjcmlwdEtlcm5lbCgpO1xyXG4gICAgY29uc3QgaHRtbEtlcm5lbCA9IG5ldyBIdG1sS2VybmVsKCk7XHJcblxyXG4gICAgY29tcG9zaXRlS2VybmVsLmFkZChqc0tlcm5lbCwgW1wianNcIl0pO1xyXG4gICAgY29tcG9zaXRlS2VybmVsLmFkZChodG1sS2VybmVsKTtcclxuXHJcbiAgICBjb21wb3NpdGVLZXJuZWwuc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMoZW52ZWxvcGUgPT4ge1xyXG4gICAgICAgIGdsb2JhbD8ucHVibGlzaENvbW1hbmRPckV2ZW50KGVudmVsb3BlKTtcclxuICAgIH0pO1xyXG5cclxuICAgIGlmIChnbG9iYWwpIHtcclxuICAgICAgICBnbG9iYWwuc2VuZEtlcm5lbENvbW1hbmQgPSAoa2VybmVsQ29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKSA9PiB7XHJcbiAgICAgICAgICAgIGNvbXBvc2l0ZUtlcm5lbC5zZW5kKGtlcm5lbENvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxufSJdLCJuYW1lcyI6WyJJbnNlcnRUZXh0Rm9ybWF0IiwiRGlhZ25vc3RpY1NldmVyaXR5IiwiRG9jdW1lbnRTZXJpYWxpemF0aW9uVHlwZSIsIlJlcXVlc3RUeXBlIiwiU3VibWlzc2lvblR5cGUiLCJMb2dMZXZlbCIsInV0aWxpdGllcy5pc0tlcm5lbENvbW1hbmRFbnZlbG9wZSIsInV0aWxpdGllcy5pc0tlcm5lbEV2ZW50RW52ZWxvcGUiLCJjb250cmFjdHMuUmVxdWVzdEtlcm5lbEluZm9UeXBlIiwiY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZFR5cGUiLCJjb250cmFjdHMuQ29tbWFuZEZhaWxlZFR5cGUiLCJjb250cmFjdHMuQ29tbWFuZFN1Y2NlZWRlZFR5cGUiLCJjb250cmFjdHMuRGlzcGxheWVkVmFsdWVQcm9kdWNlZFR5cGUiLCJrZXJuZWwuS2VybmVsIiwiY29udHJhY3RzLlN1Ym1pdENvZGVUeXBlIiwiY29udHJhY3RzLkNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlIiwiY29udHJhY3RzLlJlcXVlc3RWYWx1ZUluZm9zVHlwZSIsImNvbnRyYWN0cy5SZXF1ZXN0VmFsdWVUeXBlIiwiY29udHJhY3RzLlJldHVyblZhbHVlUHJvZHVjZWRUeXBlIiwiY29udHJhY3RzLlZhbHVlSW5mb3NQcm9kdWNlZFR5cGUiLCJjb250cmFjdHMuVmFsdWVQcm9kdWNlZFR5cGUiXSwibWFwcGluZ3MiOiI7Ozs7Ozs7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

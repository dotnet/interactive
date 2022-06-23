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

    function setup() {
        let compositeKernel = new CompositeKernel("browser");
        const jsKernel = new JavascriptKernel();
        compositeKernel.add(jsKernel, ["js"]);
        // @ts-ignore
        if (publishCommandOrEvent) {
            compositeKernel.subscribeToKernelEvents(envelope => {
                // @ts-ignore
                publishCommandOrEvent(envelope);
            });
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
    exports.formatValue = formatValue;
    exports.isKernelCommandEnvelope = isKernelCommandEnvelope;
    exports.isKernelEventEnvelope = isKernelEventEnvelope;
    exports.isPromiseCompletionSource = isPromiseCompletionSource;
    exports.setup = setup;
    exports.submitCommandAndGetResult = submitCommandAndGetResult;

    Object.defineProperty(exports, '__esModule', { value: true });

}));
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiZG90bmV0LWludGVyYWN0aXZlLmpzIiwic291cmNlcyI6WyIuLi9zcmMvY29udHJhY3RzLnRzIiwiLi4vc3JjL3V0aWxpdGllcy50cyIsIi4uL3NyYy9sb2dnZXIudHMiLCIuLi9zcmMvZ2VuZXJpY0NoYW5uZWwudHMiLCIuLi9zcmMvdG9rZW5HZW5lcmF0b3IudHMiLCIuLi9zcmMva2VybmVsSW52b2NhdGlvbkNvbnRleHQudHMiLCIuLi9zcmMva2VybmVsU2NoZWR1bGVyLnRzIiwiLi4vc3JjL2tlcm5lbC50cyIsIi4uL3NyYy9jb21wb3NpdGVLZXJuZWwudHMiLCIuLi9zcmMvY29uc29sZUNhcHR1cmUudHMiLCIuLi9zcmMvamF2YXNjcmlwdEtlcm5lbC50cyIsIi4uL3NyYy9wcm94eUtlcm5lbC50cyIsIi4uL3NyYy9rZXJuZWxIb3N0LnRzIiwiLi4vc3JjL3NldHVwLnRzIl0sInNvdXJjZXNDb250ZW50IjpbIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG4vLyBHZW5lcmF0ZWQgVHlwZVNjcmlwdCBpbnRlcmZhY2VzIGFuZCB0eXBlcy5cclxuXHJcbi8vIC0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSBLZXJuZWwgQ29tbWFuZHNcclxuXHJcbmV4cG9ydCBjb25zdCBBZGRQYWNrYWdlVHlwZSA9IFwiQWRkUGFja2FnZVwiO1xyXG5leHBvcnQgY29uc3QgQ2FuY2VsVHlwZSA9IFwiQ2FuY2VsXCI7XHJcbmV4cG9ydCBjb25zdCBDaGFuZ2VXb3JraW5nRGlyZWN0b3J5VHlwZSA9IFwiQ2hhbmdlV29ya2luZ0RpcmVjdG9yeVwiO1xyXG5leHBvcnQgY29uc3QgQ29tcGlsZVByb2plY3RUeXBlID0gXCJDb21waWxlUHJvamVjdFwiO1xyXG5leHBvcnQgY29uc3QgRGlzcGxheUVycm9yVHlwZSA9IFwiRGlzcGxheUVycm9yXCI7XHJcbmV4cG9ydCBjb25zdCBEaXNwbGF5VmFsdWVUeXBlID0gXCJEaXNwbGF5VmFsdWVcIjtcclxuZXhwb3J0IGNvbnN0IE9wZW5Eb2N1bWVudFR5cGUgPSBcIk9wZW5Eb2N1bWVudFwiO1xyXG5leHBvcnQgY29uc3QgT3BlblByb2plY3RUeXBlID0gXCJPcGVuUHJvamVjdFwiO1xyXG5leHBvcnQgY29uc3QgUXVpdFR5cGUgPSBcIlF1aXRcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RDb21wbGV0aW9uc1R5cGUgPSBcIlJlcXVlc3RDb21wbGV0aW9uc1wiO1xyXG5leHBvcnQgY29uc3QgUmVxdWVzdERpYWdub3N0aWNzVHlwZSA9IFwiUmVxdWVzdERpYWdub3N0aWNzXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0SG92ZXJUZXh0VHlwZSA9IFwiUmVxdWVzdEhvdmVyVGV4dFwiO1xyXG5leHBvcnQgY29uc3QgUmVxdWVzdElucHV0VHlwZSA9IFwiUmVxdWVzdElucHV0XCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0S2VybmVsSW5mb1R5cGUgPSBcIlJlcXVlc3RLZXJuZWxJbmZvXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0U2lnbmF0dXJlSGVscFR5cGUgPSBcIlJlcXVlc3RTaWduYXR1cmVIZWxwXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0VmFsdWVUeXBlID0gXCJSZXF1ZXN0VmFsdWVcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RWYWx1ZUluZm9zVHlwZSA9IFwiUmVxdWVzdFZhbHVlSW5mb3NcIjtcclxuZXhwb3J0IGNvbnN0IFNlbmRFZGl0YWJsZUNvZGVUeXBlID0gXCJTZW5kRWRpdGFibGVDb2RlXCI7XHJcbmV4cG9ydCBjb25zdCBTdWJtaXRDb2RlVHlwZSA9IFwiU3VibWl0Q29kZVwiO1xyXG5leHBvcnQgY29uc3QgVXBkYXRlRGlzcGxheWVkVmFsdWVUeXBlID0gXCJVcGRhdGVEaXNwbGF5ZWRWYWx1ZVwiO1xyXG5cclxuZXhwb3J0IHR5cGUgS2VybmVsQ29tbWFuZFR5cGUgPVxyXG4gICAgICB0eXBlb2YgQWRkUGFja2FnZVR5cGVcclxuICAgIHwgdHlwZW9mIENhbmNlbFR5cGVcclxuICAgIHwgdHlwZW9mIENoYW5nZVdvcmtpbmdEaXJlY3RvcnlUeXBlXHJcbiAgICB8IHR5cGVvZiBDb21waWxlUHJvamVjdFR5cGVcclxuICAgIHwgdHlwZW9mIERpc3BsYXlFcnJvclR5cGVcclxuICAgIHwgdHlwZW9mIERpc3BsYXlWYWx1ZVR5cGVcclxuICAgIHwgdHlwZW9mIE9wZW5Eb2N1bWVudFR5cGVcclxuICAgIHwgdHlwZW9mIE9wZW5Qcm9qZWN0VHlwZVxyXG4gICAgfCB0eXBlb2YgUXVpdFR5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RDb21wbGV0aW9uc1R5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3REaWFnbm9zdGljc1R5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RIb3ZlclRleHRUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0SW5wdXRUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0S2VybmVsSW5mb1R5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RTaWduYXR1cmVIZWxwVHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdFZhbHVlVHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdFZhbHVlSW5mb3NUeXBlXHJcbiAgICB8IHR5cGVvZiBTZW5kRWRpdGFibGVDb2RlVHlwZVxyXG4gICAgfCB0eXBlb2YgU3VibWl0Q29kZVR5cGVcclxuICAgIHwgdHlwZW9mIFVwZGF0ZURpc3BsYXllZFZhbHVlVHlwZTtcclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQWRkUGFja2FnZSBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcGFja2FnZVJlZmVyZW5jZTogUGFja2FnZVJlZmVyZW5jZTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kIHtcclxuICAgIHRhcmdldEtlcm5lbE5hbWU/OiBzdHJpbmc7XHJcbiAgICBvcmlnaW5Vcmk/OiBzdHJpbmc7XHJcbiAgICBkZXN0aW5hdGlvblVyaT86IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDYW5jZWwgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDaGFuZ2VXb3JraW5nRGlyZWN0b3J5IGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICB3b3JraW5nRGlyZWN0b3J5OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tcGlsZVByb2plY3QgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5RXJyb3IgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIG1lc3NhZ2U6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5VmFsdWUgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIGZvcm1hdHRlZFZhbHVlOiBGb3JtYXR0ZWRWYWx1ZTtcclxuICAgIHZhbHVlSWQ6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBPcGVuRG9jdW1lbnQgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIHJlbGF0aXZlRmlsZVBhdGg6IHN0cmluZztcclxuICAgIHJlZ2lvbk5hbWU/OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgT3BlblByb2plY3QgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIHByb2plY3Q6IFByb2plY3Q7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUXVpdCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RDb21wbGV0aW9ucyBleHRlbmRzIExhbmd1YWdlU2VydmljZUNvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIExhbmd1YWdlU2VydmljZUNvbW1hbmQgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIGNvZGU6IHN0cmluZztcclxuICAgIGxpbmVQb3NpdGlvbjogTGluZVBvc2l0aW9uO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3REaWFnbm9zdGljcyBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RIb3ZlclRleHQgZXh0ZW5kcyBMYW5ndWFnZVNlcnZpY2VDb21tYW5kIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBSZXF1ZXN0SW5wdXQgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIHByb21wdDogc3RyaW5nO1xyXG4gICAgaXNQYXNzd29yZDogYm9vbGVhbjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBSZXF1ZXN0S2VybmVsSW5mbyBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RTaWduYXR1cmVIZWxwIGV4dGVuZHMgTGFuZ3VhZ2VTZXJ2aWNlQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdFZhbHVlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBuYW1lOiBzdHJpbmc7XHJcbiAgICBtaW1lVHlwZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RWYWx1ZUluZm9zIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgU2VuZEVkaXRhYmxlQ29kZSBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgbGFuZ3VhZ2U6IHN0cmluZztcclxuICAgIGNvZGU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTdWJtaXRDb2RlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbiAgICBzdWJtaXNzaW9uVHlwZT86IFN1Ym1pc3Npb25UeXBlO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFVwZGF0ZURpc3BsYXllZFZhbHVlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBmb3JtYXR0ZWRWYWx1ZTogRm9ybWF0dGVkVmFsdWU7XHJcbiAgICB2YWx1ZUlkOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsRXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlFbGVtZW50IGV4dGVuZHMgSW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQge1xyXG4gICAgZGF0YTogeyBba2V5OiBzdHJpbmddOiBhbnk7IH07XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFRleHRFbGVtZW50IGV4dGVuZHMgSW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQge1xyXG4gICAgdGV4dDogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEVycm9yRWxlbWVudCBleHRlbmRzIEludGVyYWN0aXZlRG9jdW1lbnRPdXRwdXRFbGVtZW50IHtcclxuICAgIGVycm9yTmFtZTogc3RyaW5nO1xyXG4gICAgZXJyb3JWYWx1ZTogc3RyaW5nO1xyXG4gICAgc3RhY2tUcmFjZTogQXJyYXk8c3RyaW5nPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va1BhcnNlUmVxdWVzdCBleHRlbmRzIE5vdGVib29rUGFyc2VPclNlcmlhbGl6ZVJlcXVlc3Qge1xyXG4gICAgdHlwZTogUmVxdWVzdFR5cGU7XHJcbiAgICByYXdEYXRhOiBVaW50OEFycmF5O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rUGFyc2VPclNlcmlhbGl6ZVJlcXVlc3Qge1xyXG4gICAgdHlwZTogUmVxdWVzdFR5cGU7XHJcbiAgICBpZDogc3RyaW5nO1xyXG4gICAgc2VyaWFsaXphdGlvblR5cGU6IERvY3VtZW50U2VyaWFsaXphdGlvblR5cGU7XHJcbiAgICBkZWZhdWx0TGFuZ3VhZ2U6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va1NlcmlhbGl6ZVJlcXVlc3QgZXh0ZW5kcyBOb3RlYm9va1BhcnNlT3JTZXJpYWxpemVSZXF1ZXN0IHtcclxuICAgIHR5cGU6IFJlcXVlc3RUeXBlO1xyXG4gICAgbmV3TGluZTogc3RyaW5nO1xyXG4gICAgZG9jdW1lbnQ6IEludGVyYWN0aXZlRG9jdW1lbnQ7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tQYXJzZVJlc3BvbnNlIGV4dGVuZHMgTm90ZWJvb2tQYXJzZXJTZXJ2ZXJSZXNwb25zZSB7XHJcbiAgICBkb2N1bWVudDogSW50ZXJhY3RpdmVEb2N1bWVudDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va1BhcnNlclNlcnZlclJlc3BvbnNlIHtcclxuICAgIGlkOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tTZXJpYWxpemVSZXNwb25zZSBleHRlbmRzIE5vdGVib29rUGFyc2VyU2VydmVyUmVzcG9uc2Uge1xyXG4gICAgcmF3RGF0YTogVWludDhBcnJheTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va0Vycm9yUmVzcG9uc2UgZXh0ZW5kcyBOb3RlYm9va1BhcnNlclNlcnZlclJlc3BvbnNlIHtcclxuICAgIGVycm9yTWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG4vLyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gS2VybmVsIGV2ZW50c1xyXG5cclxuZXhwb3J0IGNvbnN0IEFzc2VtYmx5UHJvZHVjZWRUeXBlID0gXCJBc3NlbWJseVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBDb2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZSA9IFwiQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFwiO1xyXG5leHBvcnQgY29uc3QgQ29tbWFuZENhbmNlbGxlZFR5cGUgPSBcIkNvbW1hbmRDYW5jZWxsZWRcIjtcclxuZXhwb3J0IGNvbnN0IENvbW1hbmRGYWlsZWRUeXBlID0gXCJDb21tYW5kRmFpbGVkXCI7XHJcbmV4cG9ydCBjb25zdCBDb21tYW5kU3VjY2VlZGVkVHlwZSA9IFwiQ29tbWFuZFN1Y2NlZWRlZFwiO1xyXG5leHBvcnQgY29uc3QgQ29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZSA9IFwiQ29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkXCI7XHJcbmV4cG9ydCBjb25zdCBDb21wbGV0aW9uc1Byb2R1Y2VkVHlwZSA9IFwiQ29tcGxldGlvbnNQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgRGlhZ25vc3RpY0xvZ0VudHJ5UHJvZHVjZWRUeXBlID0gXCJEaWFnbm9zdGljTG9nRW50cnlQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgRGlhZ25vc3RpY3NQcm9kdWNlZFR5cGUgPSBcIkRpYWdub3N0aWNzUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IERpc3BsYXllZFZhbHVlUHJvZHVjZWRUeXBlID0gXCJEaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBEaXNwbGF5ZWRWYWx1ZVVwZGF0ZWRUeXBlID0gXCJEaXNwbGF5ZWRWYWx1ZVVwZGF0ZWRcIjtcclxuZXhwb3J0IGNvbnN0IERvY3VtZW50T3BlbmVkVHlwZSA9IFwiRG9jdW1lbnRPcGVuZWRcIjtcclxuZXhwb3J0IGNvbnN0IEVycm9yUHJvZHVjZWRUeXBlID0gXCJFcnJvclByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBIb3ZlclRleHRQcm9kdWNlZFR5cGUgPSBcIkhvdmVyVGV4dFByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBJbmNvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGUgPSBcIkluY29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkXCI7XHJcbmV4cG9ydCBjb25zdCBJbnB1dFByb2R1Y2VkVHlwZSA9IFwiSW5wdXRQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgS2VybmVsRXh0ZW5zaW9uTG9hZGVkVHlwZSA9IFwiS2VybmVsRXh0ZW5zaW9uTG9hZGVkXCI7XHJcbmV4cG9ydCBjb25zdCBLZXJuZWxJbmZvUHJvZHVjZWRUeXBlID0gXCJLZXJuZWxJbmZvUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IEtlcm5lbFJlYWR5VHlwZSA9IFwiS2VybmVsUmVhZHlcIjtcclxuZXhwb3J0IGNvbnN0IFBhY2thZ2VBZGRlZFR5cGUgPSBcIlBhY2thZ2VBZGRlZFwiO1xyXG5leHBvcnQgY29uc3QgUHJvamVjdE9wZW5lZFR5cGUgPSBcIlByb2plY3RPcGVuZWRcIjtcclxuZXhwb3J0IGNvbnN0IFJldHVyblZhbHVlUHJvZHVjZWRUeXBlID0gXCJSZXR1cm5WYWx1ZVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBTaWduYXR1cmVIZWxwUHJvZHVjZWRUeXBlID0gXCJTaWduYXR1cmVIZWxwUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IFN0YW5kYXJkRXJyb3JWYWx1ZVByb2R1Y2VkVHlwZSA9IFwiU3RhbmRhcmRFcnJvclZhbHVlUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IFN0YW5kYXJkT3V0cHV0VmFsdWVQcm9kdWNlZFR5cGUgPSBcIlN0YW5kYXJkT3V0cHV0VmFsdWVQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgVmFsdWVJbmZvc1Byb2R1Y2VkVHlwZSA9IFwiVmFsdWVJbmZvc1Byb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBWYWx1ZVByb2R1Y2VkVHlwZSA9IFwiVmFsdWVQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgV29ya2luZ0RpcmVjdG9yeUNoYW5nZWRUeXBlID0gXCJXb3JraW5nRGlyZWN0b3J5Q2hhbmdlZFwiO1xyXG5cclxuZXhwb3J0IHR5cGUgS2VybmVsRXZlbnRUeXBlID1cclxuICAgICAgdHlwZW9mIEFzc2VtYmx5UHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBDb2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZVxyXG4gICAgfCB0eXBlb2YgQ29tbWFuZENhbmNlbGxlZFR5cGVcclxuICAgIHwgdHlwZW9mIENvbW1hbmRGYWlsZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBDb21tYW5kU3VjY2VlZGVkVHlwZVxyXG4gICAgfCB0eXBlb2YgQ29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZVxyXG4gICAgfCB0eXBlb2YgQ29tcGxldGlvbnNQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIERpYWdub3N0aWNMb2dFbnRyeVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgRGlhZ25vc3RpY3NQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIERpc3BsYXllZFZhbHVlUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBEaXNwbGF5ZWRWYWx1ZVVwZGF0ZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBEb2N1bWVudE9wZW5lZFR5cGVcclxuICAgIHwgdHlwZW9mIEVycm9yUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBIb3ZlclRleHRQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIEluY29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZVxyXG4gICAgfCB0eXBlb2YgSW5wdXRQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIEtlcm5lbEV4dGVuc2lvbkxvYWRlZFR5cGVcclxuICAgIHwgdHlwZW9mIEtlcm5lbEluZm9Qcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIEtlcm5lbFJlYWR5VHlwZVxyXG4gICAgfCB0eXBlb2YgUGFja2FnZUFkZGVkVHlwZVxyXG4gICAgfCB0eXBlb2YgUHJvamVjdE9wZW5lZFR5cGVcclxuICAgIHwgdHlwZW9mIFJldHVyblZhbHVlUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBTaWduYXR1cmVIZWxwUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBTdGFuZGFyZEVycm9yVmFsdWVQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIFN0YW5kYXJkT3V0cHV0VmFsdWVQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIFZhbHVlSW5mb3NQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIFZhbHVlUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBXb3JraW5nRGlyZWN0b3J5Q2hhbmdlZFR5cGU7XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEFzc2VtYmx5UHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBhc3NlbWJseTogQmFzZTY0RW5jb2RlZEFzc2VtYmx5O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvZGVTdWJtaXNzaW9uUmVjZWl2ZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tbWFuZENhbmNlbGxlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb21tYW5kRmFpbGVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbW1hbmRTdWNjZWVkZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbXBsZXRpb25zUHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBsaW5lUG9zaXRpb25TcGFuPzogTGluZVBvc2l0aW9uU3BhbjtcclxuICAgIGNvbXBsZXRpb25zOiBBcnJheTxDb21wbGV0aW9uSXRlbT47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlhZ25vc3RpY0xvZ0VudHJ5UHJvZHVjZWQgZXh0ZW5kcyBEaWFnbm9zdGljRXZlbnQge1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpYWdub3N0aWNFdmVudCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaWFnbm9zdGljc1Byb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgZGlhZ25vc3RpY3M6IEFycmF5PERpYWdub3N0aWM+O1xyXG4gICAgZm9ybWF0dGVkRGlhZ25vc3RpY3M6IEFycmF5PEZvcm1hdHRlZFZhbHVlPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5RXZlbnQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBmb3JtYXR0ZWRWYWx1ZXM6IEFycmF5PEZvcm1hdHRlZFZhbHVlPjtcclxuICAgIHZhbHVlSWQ/OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlzcGxheWVkVmFsdWVVcGRhdGVkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEb2N1bWVudE9wZW5lZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHJlbGF0aXZlRmlsZVBhdGg6IHN0cmluZztcclxuICAgIHJlZ2lvbk5hbWU/OiBzdHJpbmc7XHJcbiAgICBjb250ZW50OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRXJyb3JQcm9kdWNlZCBleHRlbmRzIERpc3BsYXlFdmVudCB7XHJcbiAgICBtZXNzYWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSG92ZXJUZXh0UHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBjb250ZW50OiBBcnJheTxGb3JtYXR0ZWRWYWx1ZT47XHJcbiAgICBsaW5lUG9zaXRpb25TcGFuPzogTGluZVBvc2l0aW9uU3BhbjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJbmNvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJbnB1dFByb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgdmFsdWU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxFeHRlbnNpb25Mb2FkZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsSW5mb1Byb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAga2VybmVsSW5mbzogS2VybmVsSW5mbztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxSZWFkeSBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQYWNrYWdlQWRkZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBwYWNrYWdlUmVmZXJlbmNlOiBSZXNvbHZlZFBhY2thZ2VSZWZlcmVuY2U7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUHJvamVjdE9wZW5lZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHByb2plY3RJdGVtczogQXJyYXk8UHJvamVjdEl0ZW0+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJldHVyblZhbHVlUHJvZHVjZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFNpZ25hdHVyZUhlbHBQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHNpZ25hdHVyZXM6IEFycmF5PFNpZ25hdHVyZUluZm9ybWF0aW9uPjtcclxuICAgIGFjdGl2ZVNpZ25hdHVyZUluZGV4OiBudW1iZXI7XHJcbiAgICBhY3RpdmVQYXJhbWV0ZXJJbmRleDogbnVtYmVyO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFN0YW5kYXJkRXJyb3JWYWx1ZVByb2R1Y2VkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTdGFuZGFyZE91dHB1dFZhbHVlUHJvZHVjZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFZhbHVlSW5mb3NQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHZhbHVlSW5mb3M6IEFycmF5PEtlcm5lbFZhbHVlSW5mbz47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgVmFsdWVQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIG5hbWU6IHN0cmluZztcclxuICAgIGZvcm1hdHRlZFZhbHVlOiBGb3JtYXR0ZWRWYWx1ZTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBXb3JraW5nRGlyZWN0b3J5Q2hhbmdlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHdvcmtpbmdEaXJlY3Rvcnk6IHN0cmluZztcclxufVxyXG5cclxuLy8gLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIFJlcXVpcmVkIFR5cGVzXHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEJhc2U2NEVuY29kZWRBc3NlbWJseSB7XHJcbiAgICB2YWx1ZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbXBsZXRpb25JdGVtIHtcclxuICAgIGRpc3BsYXlUZXh0OiBzdHJpbmc7XHJcbiAgICBraW5kOiBzdHJpbmc7XHJcbiAgICBmaWx0ZXJUZXh0OiBzdHJpbmc7XHJcbiAgICBzb3J0VGV4dDogc3RyaW5nO1xyXG4gICAgaW5zZXJ0VGV4dDogc3RyaW5nO1xyXG4gICAgaW5zZXJ0VGV4dEZvcm1hdD86IEluc2VydFRleHRGb3JtYXQ7XHJcbiAgICBkb2N1bWVudGF0aW9uOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBlbnVtIEluc2VydFRleHRGb3JtYXQge1xyXG4gICAgUGxhaW5UZXh0ID0gXCJwbGFpbnRleHRcIixcclxuICAgIFNuaXBwZXQgPSBcInNuaXBwZXRcIixcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaWFnbm9zdGljIHtcclxuICAgIGxpbmVQb3NpdGlvblNwYW46IExpbmVQb3NpdGlvblNwYW47XHJcbiAgICBzZXZlcml0eTogRGlhZ25vc3RpY1NldmVyaXR5O1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgZW51bSBEaWFnbm9zdGljU2V2ZXJpdHkge1xyXG4gICAgSGlkZGVuID0gXCJoaWRkZW5cIixcclxuICAgIEluZm8gPSBcImluZm9cIixcclxuICAgIFdhcm5pbmcgPSBcIndhcm5pbmdcIixcclxuICAgIEVycm9yID0gXCJlcnJvclwiLFxyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIExpbmVQb3NpdGlvblNwYW4ge1xyXG4gICAgc3RhcnQ6IExpbmVQb3NpdGlvbjtcclxuICAgIGVuZDogTGluZVBvc2l0aW9uO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIExpbmVQb3NpdGlvbiB7XHJcbiAgICBsaW5lOiBudW1iZXI7XHJcbiAgICBjaGFyYWN0ZXI6IG51bWJlcjtcclxufVxyXG5cclxuZXhwb3J0IGVudW0gRG9jdW1lbnRTZXJpYWxpemF0aW9uVHlwZSB7XHJcbiAgICBEaWIgPSBcImRpYlwiLFxyXG4gICAgSXB5bmIgPSBcImlweW5iXCIsXHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRm9ybWF0dGVkVmFsdWUge1xyXG4gICAgbWltZVR5cGU6IHN0cmluZztcclxuICAgIHZhbHVlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSW50ZXJhY3RpdmVEb2N1bWVudCB7XHJcbiAgICBlbGVtZW50czogQXJyYXk8SW50ZXJhY3RpdmVEb2N1bWVudEVsZW1lbnQ+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEludGVyYWN0aXZlRG9jdW1lbnRFbGVtZW50IHtcclxuICAgIGxhbmd1YWdlOiBzdHJpbmc7XHJcbiAgICBjb250ZW50czogc3RyaW5nO1xyXG4gICAgb3V0cHV0czogQXJyYXk8SW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQ+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbEluZm8ge1xyXG4gICAgYWxpYXNlczogQXJyYXk8c3RyaW5nPjtcclxuICAgIGxhbmd1YWdlTmFtZT86IHN0cmluZztcclxuICAgIGxhbmd1YWdlVmVyc2lvbj86IHN0cmluZztcclxuICAgIGxvY2FsTmFtZTogc3RyaW5nO1xyXG4gICAgdXJpPzogc3RyaW5nO1xyXG4gICAgcmVtb3RlVXJpPzogc3RyaW5nO1xyXG4gICAgc3VwcG9ydGVkS2VybmVsQ29tbWFuZHM6IEFycmF5PEtlcm5lbENvbW1hbmRJbmZvPjtcclxuICAgIHN1cHBvcnRlZERpcmVjdGl2ZXM6IEFycmF5PEtlcm5lbERpcmVjdGl2ZUluZm8+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmRJbmZvIHtcclxuICAgIG5hbWU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxEaXJlY3RpdmVJbmZvIHtcclxuICAgIG5hbWU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxWYWx1ZUluZm8ge1xyXG4gICAgbmFtZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFBhY2thZ2VSZWZlcmVuY2Uge1xyXG4gICAgcGFja2FnZU5hbWU6IHN0cmluZztcclxuICAgIHBhY2thZ2VWZXJzaW9uOiBzdHJpbmc7XHJcbiAgICBpc1BhY2thZ2VWZXJzaW9uU3BlY2lmaWVkOiBib29sZWFuO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFByb2plY3Qge1xyXG4gICAgZmlsZXM6IEFycmF5PFByb2plY3RGaWxlPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQcm9qZWN0RmlsZSB7XHJcbiAgICByZWxhdGl2ZUZpbGVQYXRoOiBzdHJpbmc7XHJcbiAgICBjb250ZW50OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUHJvamVjdEl0ZW0ge1xyXG4gICAgcmVsYXRpdmVGaWxlUGF0aDogc3RyaW5nO1xyXG4gICAgcmVnaW9uTmFtZXM6IEFycmF5PHN0cmluZz47XHJcbiAgICByZWdpb25zQ29udGVudDogeyBba2V5OiBzdHJpbmddOiBzdHJpbmc7IH07XHJcbn1cclxuXHJcbmV4cG9ydCBlbnVtIFJlcXVlc3RUeXBlIHtcclxuICAgIFBhcnNlID0gXCJwYXJzZVwiLFxyXG4gICAgU2VyaWFsaXplID0gXCJzZXJpYWxpemVcIixcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBSZXNvbHZlZFBhY2thZ2VSZWZlcmVuY2UgZXh0ZW5kcyBQYWNrYWdlUmVmZXJlbmNlIHtcclxuICAgIGFzc2VtYmx5UGF0aHM6IEFycmF5PHN0cmluZz47XHJcbiAgICBwcm9iaW5nUGF0aHM6IEFycmF5PHN0cmluZz47XHJcbiAgICBwYWNrYWdlUm9vdDogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFNpZ25hdHVyZUluZm9ybWF0aW9uIHtcclxuICAgIGxhYmVsOiBzdHJpbmc7XHJcbiAgICBkb2N1bWVudGF0aW9uOiBGb3JtYXR0ZWRWYWx1ZTtcclxuICAgIHBhcmFtZXRlcnM6IEFycmF5PFBhcmFtZXRlckluZm9ybWF0aW9uPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQYXJhbWV0ZXJJbmZvcm1hdGlvbiB7XHJcbiAgICBsYWJlbDogc3RyaW5nO1xyXG4gICAgZG9jdW1lbnRhdGlvbjogRm9ybWF0dGVkVmFsdWU7XHJcbn1cclxuXHJcbmV4cG9ydCBlbnVtIFN1Ym1pc3Npb25UeXBlIHtcclxuICAgIFJ1biA9IFwicnVuXCIsXHJcbiAgICBEaWFnbm9zZSA9IFwiZGlhZ25vc2VcIixcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxFdmVudEVudmVsb3BlIHtcclxuICAgIGV2ZW50VHlwZTogS2VybmVsRXZlbnRUeXBlO1xyXG4gICAgZXZlbnQ6IEtlcm5lbEV2ZW50O1xyXG4gICAgY29tbWFuZD86IEtlcm5lbENvbW1hbmRFbnZlbG9wZTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kRW52ZWxvcGUge1xyXG4gICAgdG9rZW4/OiBzdHJpbmc7XHJcbiAgICBpZD86IHN0cmluZztcclxuICAgIGNvbW1hbmRUeXBlOiBLZXJuZWxDb21tYW5kVHlwZTtcclxuICAgIGNvbW1hbmQ6IEtlcm5lbENvbW1hbmQ7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsRXZlbnRFbnZlbG9wZU9ic2VydmVyIHtcclxuICAgIChldmVudEVudmVsb3BlOiBLZXJuZWxFdmVudEVudmVsb3BlKTogdm9pZDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kRW52ZWxvcGVIYW5kbGVyIHtcclxuICAgIChldmVudEVudmVsb3BlOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3Bvc2FibGUge1xyXG4gICAgZGlzcG9zZSgpOiB2b2lkO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3Bvc2FibGVTdWJzY3JpcHRpb24gZXh0ZW5kcyBEaXNwb3NhYmxlIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kQW5kRXZlbnRTZW5kZXIge1xyXG4gICAgc3VibWl0Q29tbWFuZChjb21tYW5kRW52ZWxvcGU6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD47XHJcbiAgICBwdWJsaXNoS2VybmVsRXZlbnQoZXZlbnRFbnZlbG9wZTogS2VybmVsRXZlbnRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsQ29tbWFuZEFuZEV2ZW50UmVjZWl2ZXIge1xyXG4gICAgc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMob2JzZXJ2ZXI6IEtlcm5lbEV2ZW50RW52ZWxvcGVPYnNlcnZlcik6IERpc3Bvc2FibGVTdWJzY3JpcHRpb247XHJcbiAgICBzZXRDb21tYW5kSGFuZGxlcihoYW5kbGVyOiBLZXJuZWxDb21tYW5kRW52ZWxvcGVIYW5kbGVyKTogdm9pZDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kQW5kRXZlbnRDaGFubmVsIGV4dGVuZHMgS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyLCBLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciwgRGlzcG9zYWJsZSB7XHJcbn1cclxuXHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSAnLi9jb250cmFjdHMnO1xyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGlzS2VybmVsRXZlbnRFbnZlbG9wZShvYmo6IGFueSk6IG9iaiBpcyBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSB7XHJcbiAgICByZXR1cm4gb2JqLmV2ZW50VHlwZVxyXG4gICAgICAgICYmIG9iai5ldmVudDtcclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGlzS2VybmVsQ29tbWFuZEVudmVsb3BlKG9iajogYW55KTogb2JqIGlzIGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUge1xyXG4gICAgcmV0dXJuIG9iai5jb21tYW5kVHlwZVxyXG4gICAgICAgICYmIG9iai5jb21tYW5kO1xyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5leHBvcnQgZW51bSBMb2dMZXZlbCB7XHJcbiAgICBJbmZvID0gMCxcclxuICAgIFdhcm4gPSAxLFxyXG4gICAgRXJyb3IgPSAyLFxyXG4gICAgTm9uZSA9IDMsXHJcbn1cclxuXHJcbmV4cG9ydCB0eXBlIExvZ0VudHJ5ID0ge1xyXG4gICAgbG9nTGV2ZWw6IExvZ0xldmVsO1xyXG4gICAgc291cmNlOiBzdHJpbmc7XHJcbiAgICBtZXNzYWdlOiBzdHJpbmc7XHJcbn07XHJcblxyXG5leHBvcnQgY2xhc3MgTG9nZ2VyIHtcclxuXHJcbiAgICBwcml2YXRlIHN0YXRpYyBfZGVmYXVsdDogTG9nZ2VyID0gbmV3IExvZ2dlcignZGVmYXVsdCcsIChfZW50cnk6IExvZ0VudHJ5KSA9PiB7IH0pO1xyXG5cclxuICAgIHByaXZhdGUgY29uc3RydWN0b3IocHJpdmF0ZSByZWFkb25seSBzb3VyY2U6IHN0cmluZywgcmVhZG9ubHkgd3JpdGU6IChlbnRyeTogTG9nRW50cnkpID0+IHZvaWQpIHtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgaW5mbyhtZXNzYWdlOiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLndyaXRlKHsgbG9nTGV2ZWw6IExvZ0xldmVsLkluZm8sIHNvdXJjZTogdGhpcy5zb3VyY2UsIG1lc3NhZ2UgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHdhcm4obWVzc2FnZTogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy53cml0ZSh7IGxvZ0xldmVsOiBMb2dMZXZlbC5XYXJuLCBzb3VyY2U6IHRoaXMuc291cmNlLCBtZXNzYWdlIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBlcnJvcihtZXNzYWdlOiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLndyaXRlKHsgbG9nTGV2ZWw6IExvZ0xldmVsLkVycm9yLCBzb3VyY2U6IHRoaXMuc291cmNlLCBtZXNzYWdlIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgY29uZmlndXJlKHNvdXJjZTogc3RyaW5nLCB3cml0ZXI6IChlbnRyeTogTG9nRW50cnkpID0+IHZvaWQpIHtcclxuICAgICAgICBjb25zdCBsb2dnZXIgPSBuZXcgTG9nZ2VyKHNvdXJjZSwgd3JpdGVyKTtcclxuICAgICAgICBMb2dnZXIuX2RlZmF1bHQgPSBsb2dnZXI7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBnZXQgZGVmYXVsdCgpOiBMb2dnZXIge1xyXG4gICAgICAgIGlmIChMb2dnZXIuX2RlZmF1bHQpIHtcclxuICAgICAgICAgICAgcmV0dXJuIExvZ2dlci5fZGVmYXVsdDtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHRocm93IG5ldyBFcnJvcignTm8gbG9nZ2VyIGhhcyBiZWVuIGNvbmZpZ3VyZWQgZm9yIHRoaXMgY29udGV4dCcpO1xyXG4gICAgfVxyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSBcIi4vY29udHJhY3RzXCI7XHJcbmltcG9ydCAqIGFzIHV0aWxpdGllcyBmcm9tIFwiLi91dGlsaXRpZXNcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gaXNQcm9taXNlQ29tcGxldGlvblNvdXJjZTxUPihvYmo6IGFueSk6IG9iaiBpcyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxUPiB7XHJcbiAgICByZXR1cm4gb2JqLnByb21pc2VcclxuICAgICAgICAmJiBvYmoucmVzb2x2ZVxyXG4gICAgICAgICYmIG9iai5yZWplY3Q7XHJcbn1cclxuXHJcbmV4cG9ydCBjbGFzcyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxUPiB7XHJcbiAgICBwcml2YXRlIF9yZXNvbHZlOiAodmFsdWU6IFQpID0+IHZvaWQgPSAoKSA9PiB7IH07XHJcbiAgICBwcml2YXRlIF9yZWplY3Q6IChyZWFzb246IGFueSkgPT4gdm9pZCA9ICgpID0+IHsgfTtcclxuICAgIHJlYWRvbmx5IHByb21pc2U6IFByb21pc2U8VD47XHJcblxyXG4gICAgY29uc3RydWN0b3IoKSB7XHJcbiAgICAgICAgdGhpcy5wcm9taXNlID0gbmV3IFByb21pc2U8VD4oKHJlc29sdmUsIHJlamVjdCkgPT4ge1xyXG4gICAgICAgICAgICB0aGlzLl9yZXNvbHZlID0gcmVzb2x2ZTtcclxuICAgICAgICAgICAgdGhpcy5fcmVqZWN0ID0gcmVqZWN0O1xyXG4gICAgICAgIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIHJlc29sdmUodmFsdWU6IFQpIHtcclxuICAgICAgICB0aGlzLl9yZXNvbHZlKHZhbHVlKTtcclxuICAgIH1cclxuXHJcbiAgICByZWplY3QocmVhc29uOiBhbnkpIHtcclxuICAgICAgICB0aGlzLl9yZWplY3QocmVhc29uKTtcclxuICAgIH1cclxufVxyXG5cclxuZXhwb3J0IGNsYXNzIEdlbmVyaWNDaGFubmVsIGltcGxlbWVudHMgY29udHJhY3RzLktlcm5lbENvbW1hbmRBbmRFdmVudENoYW5uZWwge1xyXG5cclxuICAgIHByaXZhdGUgc3RpbGxSdW5uaW5nOiBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxudW1iZXI+O1xyXG4gICAgcHJpdmF0ZSBjb21tYW5kSGFuZGxlcjogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZUhhbmRsZXIgPSAoKSA9PiBQcm9taXNlLnJlc29sdmUoKTtcclxuICAgIHByaXZhdGUgZXZlbnRTdWJzY3JpYmVyczogQXJyYXk8Y29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGVPYnNlcnZlcj4gPSBbXTtcclxuXHJcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIHJlYWRvbmx5IG1lc3NhZ2VTZW5kZXI6IChtZXNzYWdlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlIHwgY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpID0+IFByb21pc2U8dm9pZD4sIHByaXZhdGUgcmVhZG9ubHkgbWVzc2FnZVJlY2VpdmVyOiAoKSA9PiBQcm9taXNlPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4pIHtcclxuXHJcbiAgICAgICAgdGhpcy5zdGlsbFJ1bm5pbmcgPSBuZXcgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U8bnVtYmVyPigpO1xyXG4gICAgfVxyXG5cclxuICAgIGRpc3Bvc2UoKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5zdG9wKCk7XHJcbiAgICB9XHJcblxyXG4gICAgYXN5bmMgcnVuKCk6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIHdoaWxlICh0cnVlKSB7XHJcbiAgICAgICAgICAgIGxldCBtZXNzYWdlID0gYXdhaXQgUHJvbWlzZS5yYWNlKFt0aGlzLm1lc3NhZ2VSZWNlaXZlcigpLCB0aGlzLnN0aWxsUnVubmluZy5wcm9taXNlXSk7XHJcbiAgICAgICAgICAgIGlmICh0eXBlb2YgbWVzc2FnZSA9PT0gJ251bWJlcicpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybjtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICBpZiAodXRpbGl0aWVzLmlzS2VybmVsQ29tbWFuZEVudmVsb3BlKG1lc3NhZ2UpKSB7XHJcbiAgICAgICAgICAgICAgICB0aGlzLmNvbW1hbmRIYW5kbGVyKG1lc3NhZ2UpO1xyXG4gICAgICAgICAgICB9IGVsc2UgaWYgKHV0aWxpdGllcy5pc0tlcm5lbEV2ZW50RW52ZWxvcGUobWVzc2FnZSkpIHtcclxuICAgICAgICAgICAgICAgIGZvciAobGV0IGkgPSB0aGlzLmV2ZW50U3Vic2NyaWJlcnMubGVuZ3RoIC0gMTsgaSA+PSAwOyBpLS0pIHtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmV2ZW50U3Vic2NyaWJlcnNbaV0obWVzc2FnZSk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgc3RvcCgpIHtcclxuICAgICAgICB0aGlzLnN0aWxsUnVubmluZy5yZXNvbHZlKC0xKTtcclxuICAgIH1cclxuXHJcblxyXG4gICAgc3VibWl0Q29tbWFuZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICByZXR1cm4gdGhpcy5tZXNzYWdlU2VuZGVyKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGlzaEtlcm5lbEV2ZW50KGV2ZW50RW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMubWVzc2FnZVNlbmRlcihldmVudEVudmVsb3BlKTtcclxuICAgIH1cclxuXHJcbiAgICBzdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhvYnNlcnZlcjogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGVPYnNlcnZlcik6IGNvbnRyYWN0cy5EaXNwb3NhYmxlU3Vic2NyaXB0aW9uIHtcclxuICAgICAgICB0aGlzLmV2ZW50U3Vic2NyaWJlcnMucHVzaChvYnNlcnZlcik7XHJcbiAgICAgICAgcmV0dXJuIHtcclxuICAgICAgICAgICAgZGlzcG9zZTogKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgaSA9IHRoaXMuZXZlbnRTdWJzY3JpYmVycy5pbmRleE9mKG9ic2VydmVyKTtcclxuICAgICAgICAgICAgICAgIGlmIChpID49IDApIHtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmV2ZW50U3Vic2NyaWJlcnMuc3BsaWNlKGksIDEpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxuXHJcbiAgICBzZXRDb21tYW5kSGFuZGxlcihoYW5kbGVyOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlSGFuZGxlcikge1xyXG4gICAgICAgIHRoaXMuY29tbWFuZEhhbmRsZXIgPSBoYW5kbGVyO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgY2xhc3MgQ29tbWFuZEFuZEV2ZW50UmVjZWl2ZXIge1xyXG4gICAgcHJpdmF0ZSBfd2FpdGluZ09uTWVzc2FnZXM6IFByb21pc2VDb21wbGV0aW9uU291cmNlPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4gfCBudWxsID0gbnVsbDtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX2VudmVsb3BlUXVldWU6IChjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlIHwgY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpW10gPSBbXTtcclxuXHJcbiAgICBwdWJsaWMgZGVsZWdhdGUoY29tbWFuZE9yRXZlbnQ6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUgfCBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSkge1xyXG4gICAgICAgIGlmICh0aGlzLl93YWl0aW5nT25NZXNzYWdlcykge1xyXG4gICAgICAgICAgICBsZXQgY2FwdHVyZWRNZXNzYWdlV2FpdGVyID0gdGhpcy5fd2FpdGluZ09uTWVzc2FnZXM7XHJcbiAgICAgICAgICAgIHRoaXMuX3dhaXRpbmdPbk1lc3NhZ2VzID0gbnVsbDtcclxuXHJcbiAgICAgICAgICAgIGNhcHR1cmVkTWVzc2FnZVdhaXRlci5yZXNvbHZlKGNvbW1hbmRPckV2ZW50KTtcclxuICAgICAgICB9IGVsc2Uge1xyXG5cclxuICAgICAgICAgICAgdGhpcy5fZW52ZWxvcGVRdWV1ZS5wdXNoKGNvbW1hbmRPckV2ZW50KTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHJlYWQoKTogUHJvbWlzZTxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlIHwgY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGU+IHtcclxuICAgICAgICBsZXQgZW52ZWxvcGUgPSB0aGlzLl9lbnZlbG9wZVF1ZXVlLnNoaWZ0KCk7XHJcbiAgICAgICAgaWYgKGVudmVsb3BlKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBQcm9taXNlLnJlc29sdmU8Y29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB8IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPihlbnZlbG9wZSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGVsc2Uge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBjaGFubmVsIGJ1aWxkaW5nIHByb21pc2UgYXdhaXRlcmApO1xyXG4gICAgICAgICAgICB0aGlzLl93YWl0aW5nT25NZXNzYWdlcyA9IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlIHwgY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGU+KCk7XHJcbiAgICAgICAgICAgIHJldHVybiB0aGlzLl93YWl0aW5nT25NZXNzYWdlcy5wcm9taXNlO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgS2VybmVsQ29tbWFuZEVudmVsb3BlIH0gZnJvbSBcIi4vY29udHJhY3RzXCI7XHJcblxyXG5leHBvcnQgY2xhc3MgR3VpZCB7XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyB2YWxpZGF0b3IgPSBuZXcgUmVnRXhwKFwiXlthLXowLTldezh9LVthLXowLTldezR9LVthLXowLTldezR9LVthLXowLTldezR9LVthLXowLTldezEyfSRcIiwgXCJpXCIpO1xyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgRU1QVFkgPSBcIjAwMDAwMDAwLTAwMDAtMDAwMC0wMDAwLTAwMDAwMDAwMDAwMFwiO1xyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgaXNHdWlkKGd1aWQ6IGFueSkge1xyXG4gICAgICAgIGNvbnN0IHZhbHVlOiBzdHJpbmcgPSBndWlkLnRvU3RyaW5nKCk7XHJcbiAgICAgICAgcmV0dXJuIGd1aWQgJiYgKGd1aWQgaW5zdGFuY2VvZiBHdWlkIHx8IEd1aWQudmFsaWRhdG9yLnRlc3QodmFsdWUpKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIGNyZWF0ZSgpOiBHdWlkIHtcclxuICAgICAgICByZXR1cm4gbmV3IEd1aWQoW0d1aWQuZ2VuKDIpLCBHdWlkLmdlbigxKSwgR3VpZC5nZW4oMSksIEd1aWQuZ2VuKDEpLCBHdWlkLmdlbigzKV0uam9pbihcIi1cIikpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgY3JlYXRlRW1wdHkoKTogR3VpZCB7XHJcbiAgICAgICAgcmV0dXJuIG5ldyBHdWlkKFwiZW1wdHlndWlkXCIpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgcGFyc2UoZ3VpZDogc3RyaW5nKTogR3VpZCB7XHJcbiAgICAgICAgcmV0dXJuIG5ldyBHdWlkKGd1aWQpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgcmF3KCk6IHN0cmluZyB7XHJcbiAgICAgICAgcmV0dXJuIFtHdWlkLmdlbigyKSwgR3VpZC5nZW4oMSksIEd1aWQuZ2VuKDEpLCBHdWlkLmdlbigxKSwgR3VpZC5nZW4oMyldLmpvaW4oXCItXCIpO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgc3RhdGljIGdlbihjb3VudDogbnVtYmVyKSB7XHJcbiAgICAgICAgbGV0IG91dDogc3RyaW5nID0gXCJcIjtcclxuICAgICAgICBmb3IgKGxldCBpOiBudW1iZXIgPSAwOyBpIDwgY291bnQ7IGkrKykge1xyXG4gICAgICAgICAgICAvLyB0c2xpbnQ6ZGlzYWJsZS1uZXh0LWxpbmU6bm8tYml0d2lzZVxyXG4gICAgICAgICAgICBvdXQgKz0gKCgoMSArIE1hdGgucmFuZG9tKCkpICogMHgxMDAwMCkgfCAwKS50b1N0cmluZygxNikuc3Vic3RyaW5nKDEpO1xyXG4gICAgICAgIH1cclxuICAgICAgICByZXR1cm4gb3V0O1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgdmFsdWU6IHN0cmluZztcclxuXHJcbiAgICBwcml2YXRlIGNvbnN0cnVjdG9yKGd1aWQ6IHN0cmluZykge1xyXG4gICAgICAgIGlmICghZ3VpZCkgeyB0aHJvdyBuZXcgVHlwZUVycm9yKFwiSW52YWxpZCBhcmd1bWVudDsgYHZhbHVlYCBoYXMgbm8gdmFsdWUuXCIpOyB9XHJcblxyXG4gICAgICAgIHRoaXMudmFsdWUgPSBHdWlkLkVNUFRZO1xyXG5cclxuICAgICAgICBpZiAoZ3VpZCAmJiBHdWlkLmlzR3VpZChndWlkKSkge1xyXG4gICAgICAgICAgICB0aGlzLnZhbHVlID0gZ3VpZDtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGVxdWFscyhvdGhlcjogR3VpZCk6IGJvb2xlYW4ge1xyXG4gICAgICAgIC8vIENvbXBhcmluZyBzdHJpbmcgYHZhbHVlYCBhZ2FpbnN0IHByb3ZpZGVkIGBndWlkYCB3aWxsIGF1dG8tY2FsbFxyXG4gICAgICAgIC8vIHRvU3RyaW5nIG9uIGBndWlkYCBmb3IgY29tcGFyaXNvblxyXG4gICAgICAgIHJldHVybiBHdWlkLmlzR3VpZChvdGhlcikgJiYgdGhpcy52YWx1ZSA9PT0gb3RoZXIudG9TdHJpbmcoKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgaXNFbXB0eSgpOiBib29sZWFuIHtcclxuICAgICAgICByZXR1cm4gdGhpcy52YWx1ZSA9PT0gR3VpZC5FTVBUWTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdG9TdHJpbmcoKTogc3RyaW5nIHtcclxuICAgICAgICByZXR1cm4gdGhpcy52YWx1ZTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdG9KU09OKCk6IGFueSB7XHJcbiAgICAgICAgcmV0dXJuIHtcclxuICAgICAgICAgICAgdmFsdWU6IHRoaXMudmFsdWUsXHJcbiAgICAgICAgfTtcclxuICAgIH1cclxufVxyXG5cclxuZnVuY3Rpb24gc2V0VG9rZW4oY29tbWFuZEVudmVsb3BlOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpIHtcclxuICAgIGlmICghY29tbWFuZEVudmVsb3BlLnRva2VuKSB7XHJcbiAgICAgICAgY29tbWFuZEVudmVsb3BlLnRva2VuID0gR3VpZC5jcmVhdGUoKS50b1N0cmluZygpO1xyXG4gICAgfVxyXG5cclxuICAgIC8vXHJcbn1cclxuXHJcbmV4cG9ydCBjbGFzcyBUb2tlbkdlbmVyYXRvciB7XHJcbiAgICBwcml2YXRlIF9zZWVkOiBzdHJpbmc7XHJcbiAgICBwcml2YXRlIF9jb3VudGVyOiBudW1iZXI7XHJcblxyXG4gICAgY29uc3RydWN0b3IoKSB7XHJcbiAgICAgICAgdGhpcy5fc2VlZCA9IEd1aWQuY3JlYXRlKCkudG9TdHJpbmcoKTtcclxuICAgICAgICB0aGlzLl9jb3VudGVyID0gMDtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgR2V0TmV3VG9rZW4oKTogc3RyaW5nIHtcclxuICAgICAgICB0aGlzLl9jb3VudGVyKys7XHJcbiAgICAgICAgcmV0dXJuIGAke3RoaXMuX3NlZWR9Ojoke3RoaXMuX2NvdW50ZXJ9YDtcclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgQ29tbWFuZFN1Y2NlZWRlZCwgQ29tbWFuZFN1Y2NlZWRlZFR5cGUsIENvbW1hbmRGYWlsZWQsIENvbW1hbmRGYWlsZWRUeXBlLCBLZXJuZWxDb21tYW5kRW52ZWxvcGUsIEtlcm5lbENvbW1hbmQsIEtlcm5lbEV2ZW50RW52ZWxvcGUsIERpc3Bvc2FibGUgfSBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UgfSBmcm9tIFwiLi9nZW5lcmljQ2hhbm5lbFwiO1xyXG5pbXBvcnQgeyBJS2VybmVsRXZlbnRPYnNlcnZlciwgS2VybmVsIH0gZnJvbSBcIi4va2VybmVsXCI7XHJcbmltcG9ydCB7IFRva2VuR2VuZXJhdG9yIH0gZnJvbSBcIi4vdG9rZW5HZW5lcmF0b3JcIjtcclxuXHJcblxyXG5leHBvcnQgY2xhc3MgS2VybmVsSW52b2NhdGlvbkNvbnRleHQgaW1wbGVtZW50cyBEaXNwb3NhYmxlIHtcclxuICAgIHB1YmxpYyBnZXQgcHJvbWlzZSgpOiB2b2lkIHwgUHJvbWlzZUxpa2U8dm9pZD4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLmNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxuICAgIH1cclxuICAgIHByaXZhdGUgc3RhdGljIF9jdXJyZW50OiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB8IG51bGwgPSBudWxsO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfY29tbWFuZEVudmVsb3BlOiBLZXJuZWxDb21tYW5kRW52ZWxvcGU7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9jaGlsZENvbW1hbmRzOiBLZXJuZWxDb21tYW5kRW52ZWxvcGVbXSA9IFtdO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfdG9rZW5HZW5lcmF0b3I6IFRva2VuR2VuZXJhdG9yID0gbmV3IFRva2VuR2VuZXJhdG9yKCk7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9ldmVudE9ic2VydmVyczogTWFwPHN0cmluZywgSUtlcm5lbEV2ZW50T2JzZXJ2ZXI+ID0gbmV3IE1hcCgpO1xyXG4gICAgcHJpdmF0ZSBfaXNDb21wbGV0ZSA9IGZhbHNlO1xyXG4gICAgcHVibGljIGhhbmRsaW5nS2VybmVsOiBLZXJuZWwgfCBudWxsID0gbnVsbDtcclxuICAgIHByaXZhdGUgY29tcGxldGlvblNvdXJjZSA9IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTx2b2lkPigpO1xyXG4gICAgc3RhdGljIGVzdGFibGlzaChrZXJuZWxDb21tYW5kSW52b2NhdGlvbjogS2VybmVsQ29tbWFuZEVudmVsb3BlKTogS2VybmVsSW52b2NhdGlvbkNvbnRleHQge1xyXG4gICAgICAgIGxldCBjdXJyZW50ID0gS2VybmVsSW52b2NhdGlvbkNvbnRleHQuX2N1cnJlbnQ7XHJcbiAgICAgICAgaWYgKCFjdXJyZW50IHx8IGN1cnJlbnQuX2lzQ29tcGxldGUpIHtcclxuICAgICAgICAgICAgS2VybmVsSW52b2NhdGlvbkNvbnRleHQuX2N1cnJlbnQgPSBuZXcgS2VybmVsSW52b2NhdGlvbkNvbnRleHQoa2VybmVsQ29tbWFuZEludm9jYXRpb24pO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIGlmICghYXJlQ29tbWFuZHNUaGVTYW1lKGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uLCBjdXJyZW50Ll9jb21tYW5kRW52ZWxvcGUpKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBmb3VuZCA9IGN1cnJlbnQuX2NoaWxkQ29tbWFuZHMuaW5jbHVkZXMoa2VybmVsQ29tbWFuZEludm9jYXRpb24pO1xyXG4gICAgICAgICAgICAgICAgaWYgKCFmb3VuZCkge1xyXG4gICAgICAgICAgICAgICAgICAgIGN1cnJlbnQuX2NoaWxkQ29tbWFuZHMucHVzaChrZXJuZWxDb21tYW5kSW52b2NhdGlvbik7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5fY3VycmVudCE7XHJcbiAgICB9XHJcblxyXG4gICAgc3RhdGljIGdldCBjdXJyZW50KCk6IEtlcm5lbEludm9jYXRpb25Db250ZXh0IHwgbnVsbCB7IHJldHVybiB0aGlzLl9jdXJyZW50OyB9XHJcbiAgICBnZXQgY29tbWFuZCgpOiBLZXJuZWxDb21tYW5kIHsgcmV0dXJuIHRoaXMuX2NvbW1hbmRFbnZlbG9wZS5jb21tYW5kOyB9XHJcbiAgICBnZXQgY29tbWFuZEVudmVsb3BlKCk6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSB7IHJldHVybiB0aGlzLl9jb21tYW5kRW52ZWxvcGU7IH1cclxuICAgIGNvbnN0cnVjdG9yKGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpIHtcclxuICAgICAgICB0aGlzLl9jb21tYW5kRW52ZWxvcGUgPSBrZXJuZWxDb21tYW5kSW52b2NhdGlvbjtcclxuICAgIH1cclxuXHJcbiAgICBzdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhvYnNlcnZlcjogSUtlcm5lbEV2ZW50T2JzZXJ2ZXIpIHtcclxuICAgICAgICBsZXQgc3ViVG9rZW4gPSB0aGlzLl90b2tlbkdlbmVyYXRvci5HZXROZXdUb2tlbigpO1xyXG4gICAgICAgIHRoaXMuX2V2ZW50T2JzZXJ2ZXJzLnNldChzdWJUb2tlbiwgb2JzZXJ2ZXIpO1xyXG4gICAgICAgIHJldHVybiB7XHJcbiAgICAgICAgICAgIGRpc3Bvc2U6ICgpID0+IHtcclxuICAgICAgICAgICAgICAgIHRoaXMuX2V2ZW50T2JzZXJ2ZXJzLmRlbGV0ZShzdWJUb2tlbik7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9O1xyXG4gICAgfVxyXG4gICAgY29tcGxldGUoY29tbWFuZDogS2VybmVsQ29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgaWYgKGNvbW1hbmQgPT09IHRoaXMuX2NvbW1hbmRFbnZlbG9wZSkge1xyXG4gICAgICAgICAgICB0aGlzLl9pc0NvbXBsZXRlID0gdHJ1ZTtcclxuICAgICAgICAgICAgbGV0IHN1Y2NlZWRlZDogQ29tbWFuZFN1Y2NlZWRlZCA9IHt9O1xyXG4gICAgICAgICAgICBsZXQgZXZlbnRFbnZlbG9wZTogS2VybmVsRXZlbnRFbnZlbG9wZSA9IHtcclxuICAgICAgICAgICAgICAgIGNvbW1hbmQ6IHRoaXMuX2NvbW1hbmRFbnZlbG9wZSxcclxuICAgICAgICAgICAgICAgIGV2ZW50VHlwZTogQ29tbWFuZFN1Y2NlZWRlZFR5cGUsXHJcbiAgICAgICAgICAgICAgICBldmVudDogc3VjY2VlZGVkXHJcbiAgICAgICAgICAgIH07XHJcbiAgICAgICAgICAgIHRoaXMuaW50ZXJuYWxQdWJsaXNoKGV2ZW50RW52ZWxvcGUpO1xyXG4gICAgICAgICAgICB0aGlzLmNvbXBsZXRpb25Tb3VyY2UucmVzb2x2ZSgpO1xyXG4gICAgICAgICAgICAvLyBUT0RPOiBDIyB2ZXJzaW9uIGhhcyBjb21wbGV0aW9uIGNhbGxiYWNrcyAtIGRvIHdlIG5lZWQgdGhlc2U/XHJcbiAgICAgICAgICAgIC8vIGlmICghX2V2ZW50cy5Jc0Rpc3Bvc2VkKVxyXG4gICAgICAgICAgICAvLyB7XHJcbiAgICAgICAgICAgIC8vICAgICBfZXZlbnRzLk9uQ29tcGxldGVkKCk7XHJcbiAgICAgICAgICAgIC8vIH1cclxuXHJcbiAgICAgICAgfVxyXG4gICAgICAgIGVsc2Uge1xyXG4gICAgICAgICAgICBsZXQgcG9zID0gdGhpcy5fY2hpbGRDb21tYW5kcy5pbmRleE9mKGNvbW1hbmQpO1xyXG4gICAgICAgICAgICBkZWxldGUgdGhpcy5fY2hpbGRDb21tYW5kc1twb3NdO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBmYWlsKG1lc3NhZ2U/OiBzdHJpbmcpIHtcclxuICAgICAgICAvLyBUT0RPOlxyXG4gICAgICAgIC8vIFRoZSBDIyBjb2RlIGFjY2VwdHMgYSBtZXNzYWdlIGFuZC9vciBhbiBleGNlcHRpb24uIERvIHdlIG5lZWQgdG8gYWRkIHN1cHBvcnRcclxuICAgICAgICAvLyBmb3IgZXhjZXB0aW9ucz8gKFRoZSBUUyBDb21tYW5kRmFpbGVkIGludGVyZmFjZSBkb2Vzbid0IGhhdmUgYSBwbGFjZSBmb3IgaXQgcmlnaHQgbm93LilcclxuICAgICAgICB0aGlzLl9pc0NvbXBsZXRlID0gdHJ1ZTtcclxuICAgICAgICBsZXQgZmFpbGVkOiBDb21tYW5kRmFpbGVkID0geyBtZXNzYWdlOiBtZXNzYWdlID8/IFwiQ29tbWFuZCBGYWlsZWRcIiB9O1xyXG4gICAgICAgIGxldCBldmVudEVudmVsb3BlOiBLZXJuZWxFdmVudEVudmVsb3BlID0ge1xyXG4gICAgICAgICAgICBjb21tYW5kOiB0aGlzLl9jb21tYW5kRW52ZWxvcGUsXHJcbiAgICAgICAgICAgIGV2ZW50VHlwZTogQ29tbWFuZEZhaWxlZFR5cGUsXHJcbiAgICAgICAgICAgIGV2ZW50OiBmYWlsZWRcclxuICAgICAgICB9O1xyXG5cclxuICAgICAgICB0aGlzLmludGVybmFsUHVibGlzaChldmVudEVudmVsb3BlKTtcclxuICAgICAgICB0aGlzLmNvbXBsZXRpb25Tb3VyY2UucmVzb2x2ZSgpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1Ymxpc2goa2VybmVsRXZlbnQ6IEtlcm5lbEV2ZW50RW52ZWxvcGUpIHtcclxuICAgICAgICBpZiAoIXRoaXMuX2lzQ29tcGxldGUpIHtcclxuICAgICAgICAgICAgdGhpcy5pbnRlcm5hbFB1Ymxpc2goa2VybmVsRXZlbnQpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGludGVybmFsUHVibGlzaChrZXJuZWxFdmVudDogS2VybmVsRXZlbnRFbnZlbG9wZSkge1xyXG4gICAgICAgIGxldCBjb21tYW5kID0ga2VybmVsRXZlbnQuY29tbWFuZDtcclxuICAgICAgICBpZiAoY29tbWFuZCA9PT0gbnVsbCB8fFxyXG4gICAgICAgICAgICBhcmVDb21tYW5kc1RoZVNhbWUoY29tbWFuZCEsIHRoaXMuX2NvbW1hbmRFbnZlbG9wZSkgfHxcclxuICAgICAgICAgICAgdGhpcy5fY2hpbGRDb21tYW5kcy5pbmNsdWRlcyhjb21tYW5kISkpIHtcclxuICAgICAgICAgICAgdGhpcy5fZXZlbnRPYnNlcnZlcnMuZm9yRWFjaCgob2JzZXJ2ZXIpID0+IHtcclxuICAgICAgICAgICAgICAgIG9ic2VydmVyKGtlcm5lbEV2ZW50KTtcclxuICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGlzUGFyZW50T2ZDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogS2VybmVsQ29tbWFuZEVudmVsb3BlKTogYm9vbGVhbiB7XHJcbiAgICAgICAgY29uc3QgY2hpbGRGb3VuZCA9IHRoaXMuX2NoaWxkQ29tbWFuZHMuaW5jbHVkZXMoY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICByZXR1cm4gY2hpbGRGb3VuZDtcclxuICAgIH1cclxuXHJcbiAgICBkaXNwb3NlKCkge1xyXG4gICAgICAgIGlmICghdGhpcy5faXNDb21wbGV0ZSkge1xyXG4gICAgICAgICAgICB0aGlzLmNvbXBsZXRlKHRoaXMuX2NvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIEtlcm5lbEludm9jYXRpb25Db250ZXh0Ll9jdXJyZW50ID0gbnVsbDtcclxuICAgIH1cclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGFyZUNvbW1hbmRzVGhlU2FtZShlbnZlbG9wZTE6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSwgZW52ZWxvcGUyOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBib29sZWFuIHtcclxuICAgIHJldHVybiBlbnZlbG9wZTEgPT09IGVudmVsb3BlMlxyXG4gICAgICAgIHx8IChlbnZlbG9wZTEuY29tbWFuZFR5cGUgPT09IGVudmVsb3BlMi5jb21tYW5kVHlwZSAmJiBlbnZlbG9wZTEudG9rZW4gPT09IGVudmVsb3BlMi50b2tlbik7XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vZ2VuZXJpY0NoYW5uZWxcIjtcclxuXHJcbmludGVyZmFjZSBTY2hlZHVsZXJPcGVyYXRpb248VD4ge1xyXG4gICAgdmFsdWU6IFQ7XHJcbiAgICBleGVjdXRvcjogKHZhbHVlOiBUKSA9PiBQcm9taXNlPHZvaWQ+O1xyXG4gICAgcHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U6IFByb21pc2VDb21wbGV0aW9uU291cmNlPHZvaWQ+O1xyXG59XHJcblxyXG5leHBvcnQgY2xhc3MgS2VybmVsU2NoZWR1bGVyPFQ+IHtcclxuICAgIHByaXZhdGUgb3BlcmF0aW9uUXVldWU6IEFycmF5PFNjaGVkdWxlck9wZXJhdGlvbjxUPj4gPSBbXTtcclxuICAgIHByaXZhdGUgaW5GbGlnaHRPcGVyYXRpb24/OiBTY2hlZHVsZXJPcGVyYXRpb248VD47XHJcblxyXG4gICAgY29uc3RydWN0b3IoKSB7XHJcbiAgICB9XHJcblxyXG4gICAgcnVuQXN5bmModmFsdWU6IFQsIGV4ZWN1dG9yOiAodmFsdWU6IFQpID0+IFByb21pc2U8dm9pZD4pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCBvcGVyYXRpb24gPSB7XHJcbiAgICAgICAgICAgIHZhbHVlLFxyXG4gICAgICAgICAgICBleGVjdXRvcixcclxuICAgICAgICAgICAgcHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U6IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTx2b2lkPigpLFxyXG4gICAgICAgIH07XHJcblxyXG4gICAgICAgIGlmICh0aGlzLmluRmxpZ2h0T3BlcmF0aW9uKSB7XHJcbiAgICAgICAgICAgIC8vIGludm9rZSBpbW1lZGlhdGVseVxyXG4gICAgICAgICAgICByZXR1cm4gb3BlcmF0aW9uLmV4ZWN1dG9yKG9wZXJhdGlvbi52YWx1ZSlcclxuICAgICAgICAgICAgICAgIC50aGVuKCgpID0+IHtcclxuICAgICAgICAgICAgICAgICAgICBvcGVyYXRpb24ucHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UucmVzb2x2ZSgpO1xyXG4gICAgICAgICAgICAgICAgfSlcclxuICAgICAgICAgICAgICAgIC5jYXRjaChlID0+IHtcclxuICAgICAgICAgICAgICAgICAgICBvcGVyYXRpb24ucHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UucmVqZWN0KGUpO1xyXG4gICAgICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICB0aGlzLm9wZXJhdGlvblF1ZXVlLnB1c2gob3BlcmF0aW9uKTtcclxuICAgICAgICBpZiAodGhpcy5vcGVyYXRpb25RdWV1ZS5sZW5ndGggPT09IDEpIHtcclxuICAgICAgICAgICAgdGhpcy5leGVjdXRlTmV4dENvbW1hbmQoKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiBvcGVyYXRpb24ucHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGV4ZWN1dGVOZXh0Q29tbWFuZCgpOiB2b2lkIHtcclxuICAgICAgICBjb25zdCBuZXh0T3BlcmF0aW9uID0gdGhpcy5vcGVyYXRpb25RdWV1ZS5sZW5ndGggPiAwID8gdGhpcy5vcGVyYXRpb25RdWV1ZVswXSA6IHVuZGVmaW5lZDtcclxuICAgICAgICBpZiAobmV4dE9wZXJhdGlvbikge1xyXG4gICAgICAgICAgICB0aGlzLmluRmxpZ2h0T3BlcmF0aW9uID0gbmV4dE9wZXJhdGlvbjtcclxuICAgICAgICAgICAgbmV4dE9wZXJhdGlvbi5leGVjdXRvcihuZXh0T3BlcmF0aW9uLnZhbHVlKVxyXG4gICAgICAgICAgICAgICAgLnRoZW4oKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuaW5GbGlnaHRPcGVyYXRpb24gPSB1bmRlZmluZWQ7XHJcbiAgICAgICAgICAgICAgICAgICAgbmV4dE9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5yZXNvbHZlKCk7XHJcbiAgICAgICAgICAgICAgICB9KVxyXG4gICAgICAgICAgICAgICAgLmNhdGNoKGUgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuaW5GbGlnaHRPcGVyYXRpb24gPSB1bmRlZmluZWQ7XHJcbiAgICAgICAgICAgICAgICAgICAgbmV4dE9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5yZWplY3QoZSk7XHJcbiAgICAgICAgICAgICAgICB9KVxyXG4gICAgICAgICAgICAgICAgLmZpbmFsbHkoKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMub3BlcmF0aW9uUXVldWUuc2hpZnQoKTtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmV4ZWN1dGVOZXh0Q29tbWFuZCgpO1xyXG4gICAgICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgeyBhcmVDb21tYW5kc1RoZVNhbWUsIEtlcm5lbEludm9jYXRpb25Db250ZXh0IH0gZnJvbSBcIi4va2VybmVsSW52b2NhdGlvbkNvbnRleHRcIjtcclxuaW1wb3J0IHsgR3VpZCwgVG9rZW5HZW5lcmF0b3IgfSBmcm9tIFwiLi90b2tlbkdlbmVyYXRvclwiO1xyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSBcIi4vY29udHJhY3RzXCI7XHJcbmltcG9ydCB7IExvZ2dlciB9IGZyb20gXCIuL2xvZ2dlclwiO1xyXG5pbXBvcnQgeyBDb21wb3NpdGVLZXJuZWwgfSBmcm9tIFwiLi9jb21wb3NpdGVLZXJuZWxcIjtcclxuaW1wb3J0IHsgS2VybmVsU2NoZWR1bGVyIH0gZnJvbSBcIi4va2VybmVsU2NoZWR1bGVyXCI7XHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vZ2VuZXJpY0NoYW5uZWxcIjtcclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uIHtcclxuICAgIGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZTtcclxuICAgIGNvbnRleHQ6IEtlcm5lbEludm9jYXRpb25Db250ZXh0O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIElLZXJuZWxDb21tYW5kSGFuZGxlciB7XHJcbiAgICBjb21tYW5kVHlwZTogc3RyaW5nO1xyXG4gICAgaGFuZGxlOiAoY29tbWFuZEludm9jYXRpb246IElLZXJuZWxDb21tYW5kSW52b2NhdGlvbikgPT4gUHJvbWlzZTx2b2lkPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJS2VybmVsRXZlbnRPYnNlcnZlciB7XHJcbiAgICAoa2VybmVsRXZlbnQ6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlKTogdm9pZDtcclxufVxyXG5cclxuZXhwb3J0IGNsYXNzIEtlcm5lbCB7XHJcbiAgICBwcml2YXRlIF9rZXJuZWxJbmZvOiBjb250cmFjdHMuS2VybmVsSW5mbztcclxuXHJcbiAgICBwcml2YXRlIF9jb21tYW5kSGFuZGxlcnMgPSBuZXcgTWFwPHN0cmluZywgSUtlcm5lbENvbW1hbmRIYW5kbGVyPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfZXZlbnRPYnNlcnZlcnM6IHsgW3Rva2VuOiBzdHJpbmddOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZU9ic2VydmVyIH0gPSB7fTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX3Rva2VuR2VuZXJhdG9yOiBUb2tlbkdlbmVyYXRvciA9IG5ldyBUb2tlbkdlbmVyYXRvcigpO1xyXG4gICAgcHVibGljIHJvb3RLZXJuZWw6IEtlcm5lbCA9IHRoaXM7XHJcbiAgICBwdWJsaWMgcGFyZW50S2VybmVsOiBDb21wb3NpdGVLZXJuZWwgfCBudWxsID0gbnVsbDtcclxuICAgIHByaXZhdGUgX3NjaGVkdWxlcj86IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPiB8IG51bGwgPSBudWxsO1xyXG5cclxuICAgIHB1YmxpYyBnZXQga2VybmVsSW5mbygpOiBjb250cmFjdHMuS2VybmVsSW5mbyB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2tlcm5lbEluZm87XHJcbiAgICB9XHJcblxyXG4gICAgY29uc3RydWN0b3IocmVhZG9ubHkgbmFtZTogc3RyaW5nLCBsYW5ndWFnZU5hbWU/OiBzdHJpbmcsIGxhbmd1YWdlVmVyc2lvbj86IHN0cmluZykge1xyXG4gICAgICAgIHRoaXMuX2tlcm5lbEluZm8gPSB7XHJcbiAgICAgICAgICAgIGxvY2FsTmFtZTogbmFtZSxcclxuICAgICAgICAgICAgbGFuZ3VhZ2VOYW1lOiBsYW5ndWFnZU5hbWUsXHJcbiAgICAgICAgICAgIGFsaWFzZXM6IFtdLFxyXG4gICAgICAgICAgICBsYW5ndWFnZVZlcnNpb246IGxhbmd1YWdlVmVyc2lvbixcclxuICAgICAgICAgICAgc3VwcG9ydGVkRGlyZWN0aXZlczogW10sXHJcbiAgICAgICAgICAgIHN1cHBvcnRlZEtlcm5lbENvbW1hbmRzOiBbXVxyXG4gICAgICAgIH07XHJcblxyXG4gICAgICAgIHRoaXMucmVnaXN0ZXJDb21tYW5kSGFuZGxlcih7XHJcbiAgICAgICAgICAgIGNvbW1hbmRUeXBlOiBjb250cmFjdHMuUmVxdWVzdEtlcm5lbEluZm9UeXBlLCBoYW5kbGU6IGFzeW5jIGludm9jYXRpb24gPT4ge1xyXG4gICAgICAgICAgICAgICAgYXdhaXQgdGhpcy5oYW5kbGVSZXF1ZXN0S2VybmVsSW5mbyhpbnZvY2F0aW9uKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIHByb3RlY3RlZCBhc3luYyBoYW5kbGVSZXF1ZXN0S2VybmVsSW5mbyhpbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCBldmVudEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSA9IHtcclxuICAgICAgICAgICAgZXZlbnRUeXBlOiBjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSxcclxuICAgICAgICAgICAgY29tbWFuZDogaW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUsXHJcbiAgICAgICAgICAgIGV2ZW50OiA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD57IGtlcm5lbEluZm86IHRoaXMuX2tlcm5lbEluZm8gfVxyXG4gICAgICAgIH07Ly8/XHJcblxyXG4gICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKGV2ZW50RW52ZWxvcGUpO1xyXG4gICAgICAgIHJldHVybiBQcm9taXNlLnJlc29sdmUoKTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGdldFNjaGVkdWxlcigpOiBLZXJuZWxTY2hlZHVsZXI8Y29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZT4ge1xyXG4gICAgICAgIGlmICghdGhpcy5fc2NoZWR1bGVyKSB7XHJcbiAgICAgICAgICAgIHRoaXMuX3NjaGVkdWxlciA9IHRoaXMucGFyZW50S2VybmVsPy5nZXRTY2hlZHVsZXIoKSA/PyBuZXcgS2VybmVsU2NoZWR1bGVyPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGU+KCk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gdGhpcy5fc2NoZWR1bGVyO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgZW5zdXJlQ29tbWFuZFRva2VuQW5kSWQoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgY29tbWFuZEVudmVsb3BlOy8vP1xyXG4gICAgICAgIGlmICghY29tbWFuZEVudmVsb3BlLnRva2VuKSB7XHJcbiAgICAgICAgICAgIGxldCBuZXh0VG9rZW4gPSB0aGlzLl90b2tlbkdlbmVyYXRvci5HZXROZXdUb2tlbigpO1xyXG4gICAgICAgICAgICBpZiAoS2VybmVsSW52b2NhdGlvbkNvbnRleHQuY3VycmVudD8uY29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgICAgICAgICAvLyBhIHBhcmVudCBjb21tYW5kIGV4aXN0cywgY3JlYXRlIGEgdG9rZW4gaGllcmFyY2h5XHJcbiAgICAgICAgICAgICAgICBuZXh0VG9rZW4gPSBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5jdXJyZW50LmNvbW1hbmRFbnZlbG9wZS50b2tlbiE7XHJcbiAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgIGNvbW1hbmRFbnZlbG9wZS50b2tlbiA9IG5leHRUb2tlbjtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGlmICghY29tbWFuZEVudmVsb3BlLmlkKSB7XHJcbiAgICAgICAgICAgIGNvbW1hbmRFbnZlbG9wZS5pZCA9IEd1aWQuY3JlYXRlKCkudG9TdHJpbmcoKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgc3RhdGljIGdldCBjdXJyZW50KCk6IEtlcm5lbCB8IG51bGwge1xyXG4gICAgICAgIGlmIChLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5jdXJyZW50KSB7XHJcbiAgICAgICAgICAgIHJldHVybiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5jdXJyZW50LmhhbmRsaW5nS2VybmVsO1xyXG4gICAgICAgIH1cclxuICAgICAgICByZXR1cm4gbnVsbDtcclxuICAgIH1cclxuXHJcbiAgICBzdGF0aWMgZ2V0IHJvb3QoKTogS2VybmVsIHwgbnVsbCB7XHJcbiAgICAgICAgaWYgKEtlcm5lbC5jdXJyZW50KSB7XHJcbiAgICAgICAgICAgIHJldHVybiBLZXJuZWwuY3VycmVudC5yb290S2VybmVsO1xyXG4gICAgICAgIH1cclxuICAgICAgICByZXR1cm4gbnVsbDtcclxuICAgIH1cclxuXHJcbiAgICAvLyBJcyBpdCB3b3J0aCB1cyBnb2luZyB0byBlZmZvcnRzIHRvIGVuc3VyZSB0aGF0IHRoZSBQcm9taXNlIHJldHVybmVkIGhlcmUgYWNjdXJhdGVseSByZWZsZWN0c1xyXG4gICAgLy8gdGhlIGNvbW1hbmQncyBwcm9ncmVzcz8gVGhlIG9ubHkgdGhpbmcgdGhhdCBhY3R1YWxseSBjYWxscyB0aGlzIGlzIHRoZSBrZXJuZWwgY2hhbm5lbCwgdGhyb3VnaFxyXG4gICAgLy8gdGhlIGNhbGxiYWNrIHNldCB1cCBieSBhdHRhY2hLZXJuZWxUb0NoYW5uZWwsIGFuZCB0aGUgY2FsbGJhY2sgaXMgZXhwZWN0ZWQgdG8gcmV0dXJuIHZvaWQsIHNvXHJcbiAgICAvLyBub3RoaW5nIGlzIGV2ZXIgZ29pbmcgdG8gbG9vayBhdCB0aGUgcHJvbWlzZSB3ZSByZXR1cm4gaGVyZS5cclxuICAgIGFzeW5jIHNlbmQoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgdGhpcy5lbnN1cmVDb21tYW5kVG9rZW5BbmRJZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIGxldCBjb250ZXh0ID0gS2VybmVsSW52b2NhdGlvbkNvbnRleHQuZXN0YWJsaXNoKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgdGhpcy5nZXRTY2hlZHVsZXIoKS5ydW5Bc3luYyhjb21tYW5kRW52ZWxvcGUsICh2YWx1ZSkgPT4gdGhpcy5leGVjdXRlQ29tbWFuZCh2YWx1ZSkpO1xyXG4gICAgICAgIHJldHVybiBjb250ZXh0LnByb21pc2U7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBhc3luYyBleGVjdXRlQ29tbWFuZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBsZXQgY29udGV4dCA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmVzdGFibGlzaChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIGxldCBpc1Jvb3RDb21tYW5kID0gYXJlQ29tbWFuZHNUaGVTYW1lKGNvbnRleHQuY29tbWFuZEVudmVsb3BlLCBjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIGxldCBjb250ZXh0RXZlbnRzU3Vic2NyaXB0aW9uOiBjb250cmFjdHMuRGlzcG9zYWJsZSB8IG51bGwgPSBudWxsO1xyXG4gICAgICAgIGlmIChpc1Jvb3RDb21tYW5kKSB7XHJcbiAgICAgICAgICAgIGNvbnRleHRFdmVudHNTdWJzY3JpcHRpb24gPSBjb250ZXh0LnN1YnNjcmliZVRvS2VybmVsRXZlbnRzKGUgPT4ge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgbWVzc2FnZSA9IGBrZXJuZWwgJHt0aGlzLm5hbWV9IHNhdyBldmVudCAke2UuZXZlbnRUeXBlfSB3aXRoIHRva2VuICR7ZS5jb21tYW5kPy50b2tlbn1gO1xyXG4gICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhtZXNzYWdlKTtcclxuICAgICAgICAgICAgICAgIHJldHVybiB0aGlzLnB1Ymxpc2hFdmVudChlKTtcclxuICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICBhd2FpdCB0aGlzLmhhbmRsZUNvbW1hbmQoY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgY29udGV4dC5mYWlsKCg8YW55PmUpPy5tZXNzYWdlIHx8IEpTT04uc3RyaW5naWZ5KGUpKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgZmluYWxseSB7XHJcbiAgICAgICAgICAgIGlmIChjb250ZXh0RXZlbnRzU3Vic2NyaXB0aW9uKSB7XHJcbiAgICAgICAgICAgICAgICBjb250ZXh0RXZlbnRzU3Vic2NyaXB0aW9uLmRpc3Bvc2UoKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBnZXRDb21tYW5kSGFuZGxlcihjb21tYW5kVHlwZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRUeXBlKTogSUtlcm5lbENvbW1hbmRIYW5kbGVyIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5fY29tbWFuZEhhbmRsZXJzLmdldChjb21tYW5kVHlwZSk7XHJcbiAgICB9XHJcblxyXG4gICAgaGFuZGxlQ29tbWFuZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICByZXR1cm4gbmV3IFByb21pc2U8dm9pZD4oYXN5bmMgKHJlc29sdmUsIHJlamVjdCkgPT4ge1xyXG4gICAgICAgICAgICBsZXQgY29udGV4dCA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmVzdGFibGlzaChjb21tYW5kRW52ZWxvcGUpOy8vP1xyXG4gICAgICAgICAgICBjb250ZXh0LmhhbmRsaW5nS2VybmVsID0gdGhpcztcclxuICAgICAgICAgICAgbGV0IGlzUm9vdENvbW1hbmQgPSBhcmVDb21tYW5kc1RoZVNhbWUoY29udGV4dC5jb21tYW5kRW52ZWxvcGUsIGNvbW1hbmRFbnZlbG9wZSk7XHJcblxyXG4gICAgICAgICAgICBsZXQgaGFuZGxlciA9IHRoaXMuZ2V0Q29tbWFuZEhhbmRsZXIoY29tbWFuZEVudmVsb3BlLmNvbW1hbmRUeXBlKTtcclxuICAgICAgICAgICAgaWYgKGhhbmRsZXIpIHtcclxuICAgICAgICAgICAgICAgIHRyeSB7XHJcbiAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhga2VybmVsICR7dGhpcy5uYW1lfSBhYm91dCB0byBoYW5kbGUgY29tbWFuZDogJHtKU09OLnN0cmluZ2lmeShjb21tYW5kRW52ZWxvcGUpfWApO1xyXG4gICAgICAgICAgICAgICAgICAgIGF3YWl0IGhhbmRsZXIuaGFuZGxlKHsgY29tbWFuZEVudmVsb3BlOiBjb21tYW5kRW52ZWxvcGUsIGNvbnRleHQgfSk7XHJcblxyXG4gICAgICAgICAgICAgICAgICAgIGNvbnRleHQuY29tcGxldGUoY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICAgICAgICAgICAgICBpZiAoaXNSb290Q29tbWFuZCkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBjb250ZXh0LmRpc3Bvc2UoKTtcclxuICAgICAgICAgICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYGtlcm5lbCAke3RoaXMubmFtZX0gZG9uZSBoYW5kbGluZyBjb21tYW5kOiAke0pTT04uc3RyaW5naWZ5KGNvbW1hbmRFbnZlbG9wZSl9YCk7XHJcbiAgICAgICAgICAgICAgICAgICAgcmVzb2x2ZSgpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgICAgICAgICBjb250ZXh0LmZhaWwoKDxhbnk+ZSk/Lm1lc3NhZ2UgfHwgSlNPTi5zdHJpbmdpZnkoZSkpO1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChpc1Jvb3RDb21tYW5kKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbnRleHQuZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgICAgICAgICAgcmVqZWN0KGUpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgaWYgKGlzUm9vdENvbW1hbmQpIHtcclxuICAgICAgICAgICAgICAgICAgICBjb250ZXh0LmRpc3Bvc2UoKTtcclxuICAgICAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgICAgICByZWplY3QobmV3IEVycm9yKGBObyBoYW5kbGVyIGZvdW5kIGZvciBjb21tYW5kIHR5cGUgJHtjb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGV9YCkpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMob2JzZXJ2ZXI6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlT2JzZXJ2ZXIpOiBjb250cmFjdHMuRGlzcG9zYWJsZVN1YnNjcmlwdGlvbiB7XHJcbiAgICAgICAgbGV0IHN1YlRva2VuID0gdGhpcy5fdG9rZW5HZW5lcmF0b3IuR2V0TmV3VG9rZW4oKTtcclxuICAgICAgICB0aGlzLl9ldmVudE9ic2VydmVyc1tzdWJUb2tlbl0gPSBvYnNlcnZlcjtcclxuXHJcbiAgICAgICAgcmV0dXJuIHtcclxuICAgICAgICAgICAgZGlzcG9zZTogKCkgPT4geyBkZWxldGUgdGhpcy5fZXZlbnRPYnNlcnZlcnNbc3ViVG9rZW5dOyB9XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgY2FuSGFuZGxlKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSkge1xyXG4gICAgICAgIGlmIChjb21tYW5kRW52ZWxvcGUuY29tbWFuZC50YXJnZXRLZXJuZWxOYW1lICYmIGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUgIT09IHRoaXMubmFtZSkge1xyXG4gICAgICAgICAgICByZXR1cm4gZmFsc2U7XHJcblxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgIGlmICh0aGlzLmtlcm5lbEluZm8udXJpICE9PSBjb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gdGhpcy5zdXBwb3J0c0NvbW1hbmQoY29tbWFuZEVudmVsb3BlLmNvbW1hbmRUeXBlKTtcclxuICAgIH1cclxuXHJcbiAgICBzdXBwb3J0c0NvbW1hbmQoY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kVHlwZSk6IGJvb2xlYW4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9jb21tYW5kSGFuZGxlcnMuaGFzKGNvbW1hbmRUeXBlKTtcclxuICAgIH1cclxuXHJcbiAgICByZWdpc3RlckNvbW1hbmRIYW5kbGVyKGhhbmRsZXI6IElLZXJuZWxDb21tYW5kSGFuZGxlcik6IHZvaWQge1xyXG4gICAgICAgIC8vIFdoZW4gYSByZWdpc3RyYXRpb24gYWxyZWFkeSBleGlzdGVkLCB3ZSB3YW50IHRvIG92ZXJ3cml0ZSBpdCBiZWNhdXNlIHdlIHdhbnQgdXNlcnMgdG9cclxuICAgICAgICAvLyBiZSBhYmxlIHRvIGRldmVsb3AgaGFuZGxlcnMgaXRlcmF0aXZlbHksIGFuZCBpdCB3b3VsZCBiZSB1bmhlbHBmdWwgZm9yIGhhbmRsZXIgcmVnaXN0cmF0aW9uXHJcbiAgICAgICAgLy8gZm9yIGFueSBwYXJ0aWN1bGFyIGNvbW1hbmQgdG8gYmUgY3VtdWxhdGl2ZS5cclxuICAgICAgICB0aGlzLl9jb21tYW5kSGFuZGxlcnMuc2V0KGhhbmRsZXIuY29tbWFuZFR5cGUsIGhhbmRsZXIpO1xyXG4gICAgICAgIHRoaXMuX2tlcm5lbEluZm8uc3VwcG9ydGVkS2VybmVsQ29tbWFuZHMgPSBBcnJheS5mcm9tKHRoaXMuX2NvbW1hbmRIYW5kbGVycy5rZXlzKCkpLm1hcChjb21tYW5kTmFtZSA9PiAoeyBuYW1lOiBjb21tYW5kTmFtZSB9KSk7XHJcbiAgICB9XHJcblxyXG4gICAgZ2V0SGFuZGxpbmdLZXJuZWwoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKTogS2VybmVsIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICBsZXQgdGFyZ2V0S2VybmVsTmFtZSA9IGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUgPz8gdGhpcy5uYW1lO1xyXG4gICAgICAgIHJldHVybiB0YXJnZXRLZXJuZWxOYW1lID09PSB0aGlzLm5hbWUgPyB0aGlzIDogdW5kZWZpbmVkO1xyXG4gICAgfVxyXG5cclxuICAgIHByb3RlY3RlZCBwdWJsaXNoRXZlbnQoa2VybmVsRXZlbnQ6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlKSB7XHJcbiAgICAgICAgbGV0IGtleXMgPSBPYmplY3Qua2V5cyh0aGlzLl9ldmVudE9ic2VydmVycyk7XHJcbiAgICAgICAgZm9yIChsZXQgc3ViVG9rZW4gb2Yga2V5cykge1xyXG4gICAgICAgICAgICBsZXQgb2JzZXJ2ZXIgPSB0aGlzLl9ldmVudE9ic2VydmVyc1tzdWJUb2tlbl07XHJcbiAgICAgICAgICAgIG9ic2VydmVyKGtlcm5lbEV2ZW50KTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBhc3luYyBmdW5jdGlvbiBzdWJtaXRDb21tYW5kQW5kR2V0UmVzdWx0PFRFdmVudCBleHRlbmRzIGNvbnRyYWN0cy5LZXJuZWxFdmVudD4oa2VybmVsOiBLZXJuZWwsIGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSwgZXhwZWN0ZWRFdmVudFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxFdmVudFR5cGUpOiBQcm9taXNlPFRFdmVudD4ge1xyXG4gICAgbGV0IGNvbXBsZXRpb25Tb3VyY2UgPSBuZXcgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U8VEV2ZW50PigpO1xyXG4gICAgbGV0IGhhbmRsZWQgPSBmYWxzZTtcclxuICAgIGxldCBkaXNwb3NhYmxlID0ga2VybmVsLnN1YnNjcmliZVRvS2VybmVsRXZlbnRzKGV2ZW50RW52ZWxvcGUgPT4ge1xyXG4gICAgICAgIGlmIChldmVudEVudmVsb3BlLmNvbW1hbmQ/LnRva2VuID09PSBjb21tYW5kRW52ZWxvcGUudG9rZW4pIHtcclxuICAgICAgICAgICAgc3dpdGNoIChldmVudEVudmVsb3BlLmV2ZW50VHlwZSkge1xyXG4gICAgICAgICAgICAgICAgY2FzZSBjb250cmFjdHMuQ29tbWFuZEZhaWxlZFR5cGU6XHJcbiAgICAgICAgICAgICAgICAgICAgaWYgKCFoYW5kbGVkKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGhhbmRsZWQgPSB0cnVlO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBsZXQgZXJyID0gPGNvbnRyYWN0cy5Db21tYW5kRmFpbGVkPmV2ZW50RW52ZWxvcGUuZXZlbnQ7Ly8/XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbXBsZXRpb25Tb3VyY2UucmVqZWN0KGVycik7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIGJyZWFrO1xyXG4gICAgICAgICAgICAgICAgY2FzZSBjb250cmFjdHMuQ29tbWFuZFN1Y2NlZWRlZFR5cGU6XHJcbiAgICAgICAgICAgICAgICAgICAgaWYgKGFyZUNvbW1hbmRzVGhlU2FtZShldmVudEVudmVsb3BlLmNvbW1hbmQhLCBjb21tYW5kRW52ZWxvcGUpXHJcbiAgICAgICAgICAgICAgICAgICAgICAgICYmIChldmVudEVudmVsb3BlLmNvbW1hbmQ/LmlkID09PSBjb21tYW5kRW52ZWxvcGUuaWQpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGlmICghaGFuZGxlZCkgey8vPyAoJCA/IGV2ZW50RW52ZWxvcGUgOiB7fSlcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGhhbmRsZWQgPSB0cnVlO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY29tcGxldGlvblNvdXJjZS5yZWplY3QoJ0NvbW1hbmQgd2FzIGhhbmRsZWQgYmVmb3JlIHJlcG9ydGluZyBleHBlY3RlZCByZXN1bHQuJyk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgZGVmYXVsdDpcclxuICAgICAgICAgICAgICAgICAgICBpZiAoZXZlbnRFbnZlbG9wZS5ldmVudFR5cGUgPT09IGV4cGVjdGVkRXZlbnRUeXBlKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGhhbmRsZWQgPSB0cnVlO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBsZXQgZXZlbnQgPSA8VEV2ZW50PmV2ZW50RW52ZWxvcGUuZXZlbnQ7Ly8/ICgkID8gZXZlbnRFbnZlbG9wZSA6IHt9KVxyXG4gICAgICAgICAgICAgICAgICAgICAgICBjb21wbGV0aW9uU291cmNlLnJlc29sdmUoZXZlbnQpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH0pO1xyXG5cclxuICAgIHRyeSB7XHJcbiAgICAgICAgYXdhaXQga2VybmVsLnNlbmQoY29tbWFuZEVudmVsb3BlKTtcclxuICAgIH1cclxuICAgIGZpbmFsbHkge1xyXG4gICAgICAgIGRpc3Bvc2FibGUuZGlzcG9zZSgpO1xyXG4gICAgfVxyXG5cclxuICAgIHJldHVybiBjb21wbGV0aW9uU291cmNlLnByb21pc2U7XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uLCBLZXJuZWwgfSBmcm9tIFwiLi9rZXJuZWxcIjtcclxuaW1wb3J0IHsgS2VybmVsSG9zdCB9IGZyb20gXCIuL2tlcm5lbEhvc3RcIjtcclxuXHJcbmV4cG9ydCBjbGFzcyBDb21wb3NpdGVLZXJuZWwgZXh0ZW5kcyBLZXJuZWwge1xyXG5cclxuXHJcbiAgICBwcml2YXRlIF9ob3N0OiBLZXJuZWxIb3N0IHwgbnVsbCA9IG51bGw7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9uYW1lc1Rva2VybmVsTWFwOiBNYXA8c3RyaW5nLCBLZXJuZWw+ID0gbmV3IE1hcCgpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfa2VybmVsVG9OYW1lc01hcDogTWFwPEtlcm5lbCwgU2V0PHN0cmluZz4+ID0gbmV3IE1hcCgpO1xyXG5cclxuICAgIGRlZmF1bHRLZXJuZWxOYW1lOiBzdHJpbmcgfCB1bmRlZmluZWQ7XHJcblxyXG4gICAgY29uc3RydWN0b3IobmFtZTogc3RyaW5nKSB7XHJcbiAgICAgICAgc3VwZXIobmFtZSk7XHJcbiAgICB9XHJcblxyXG4gICAgZ2V0IGNoaWxkS2VybmVscygpIHtcclxuICAgICAgICByZXR1cm4gWy4uLnRoaXMuX2tlcm5lbFRvTmFtZXNNYXAua2V5cygpXTtcclxuICAgIH1cclxuXHJcbiAgICBnZXQgaG9zdCgpOiBLZXJuZWxIb3N0IHwgbnVsbCB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2hvc3Q7XHJcbiAgICB9XHJcblxyXG4gICAgc2V0IGhvc3QoaG9zdDogS2VybmVsSG9zdCB8IG51bGwpIHtcclxuICAgICAgICB0aGlzLl9ob3N0ID0gaG9zdDtcclxuICAgICAgICBpZiAodGhpcy5faG9zdCkge1xyXG4gICAgICAgICAgICB0aGlzLl9ob3N0LmFkZEtlcm5lbEluZm8odGhpcywgeyBsb2NhbE5hbWU6IHRoaXMubmFtZS50b0xvd2VyQ2FzZSgpLCBhbGlhc2VzOiBbXSwgc3VwcG9ydGVkRGlyZWN0aXZlczogW10sIHN1cHBvcnRlZEtlcm5lbENvbW1hbmRzOiBbXSB9KTtcclxuXHJcbiAgICAgICAgICAgIGZvciAobGV0IGtlcm5lbCBvZiB0aGlzLmNoaWxkS2VybmVscykge1xyXG4gICAgICAgICAgICAgICAgbGV0IGFsaWFzZXMgPSBbXTtcclxuICAgICAgICAgICAgICAgIGZvciAobGV0IG5hbWUgb2YgdGhpcy5fa2VybmVsVG9OYW1lc01hcC5nZXQoa2VybmVsKSEpIHtcclxuICAgICAgICAgICAgICAgICAgICBpZiAobmFtZSAhPT0ga2VybmVsLm5hbWUpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgYWxpYXNlcy5wdXNoKG5hbWUudG9Mb3dlckNhc2UoKSk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgdGhpcy5faG9zdC5hZGRLZXJuZWxJbmZvKGtlcm5lbCwgeyBsb2NhbE5hbWU6IGtlcm5lbC5uYW1lLnRvTG93ZXJDYXNlKCksIGFsaWFzZXM6IFsuLi5hbGlhc2VzXSwgc3VwcG9ydGVkRGlyZWN0aXZlczogW10sIHN1cHBvcnRlZEtlcm5lbENvbW1hbmRzOiBbXSB9KTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgb3ZlcnJpZGUgYXN5bmMgaGFuZGxlUmVxdWVzdEtlcm5lbEluZm8oaW52b2NhdGlvbjogSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgZm9yIChsZXQga2VybmVsIG9mIHRoaXMuY2hpbGRLZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIGlmIChrZXJuZWwuc3VwcG9ydHNDb21tYW5kKGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmRUeXBlKSkge1xyXG4gICAgICAgICAgICAgICAgYXdhaXQga2VybmVsLmhhbmRsZUNvbW1hbmQoeyBjb21tYW5kOiB7fSwgY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5SZXF1ZXN0S2VybmVsSW5mb1R5cGUgfSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgYWRkKGtlcm5lbDogS2VybmVsLCBhbGlhc2VzPzogc3RyaW5nW10pIHtcclxuICAgICAgICBpZiAoIWtlcm5lbCkge1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoXCJrZXJuZWwgY2Fubm90IGJlIG51bGwgb3IgdW5kZWZpbmVkXCIpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKCF0aGlzLmRlZmF1bHRLZXJuZWxOYW1lKSB7XHJcbiAgICAgICAgICAgIC8vIGRlZmF1bHQgdG8gZmlyc3Qga2VybmVsXHJcbiAgICAgICAgICAgIHRoaXMuZGVmYXVsdEtlcm5lbE5hbWUgPSBrZXJuZWwubmFtZTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGtlcm5lbC5wYXJlbnRLZXJuZWwgPSB0aGlzO1xyXG4gICAgICAgIGtlcm5lbC5yb290S2VybmVsID0gdGhpcy5yb290S2VybmVsO1xyXG4gICAgICAgIGtlcm5lbC5zdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhldmVudCA9PiB7XHJcbiAgICAgICAgICAgIHRoaXMucHVibGlzaEV2ZW50KGV2ZW50KTtcclxuICAgICAgICB9KTtcclxuICAgICAgICB0aGlzLl9uYW1lc1Rva2VybmVsTWFwLnNldChrZXJuZWwubmFtZS50b0xvd2VyQ2FzZSgpLCBrZXJuZWwpO1xyXG5cclxuICAgICAgICBsZXQga2VybmVsTmFtZXMgPSBuZXcgU2V0PHN0cmluZz4oKTtcclxuICAgICAgICBrZXJuZWxOYW1lcy5hZGQoa2VybmVsLm5hbWUpO1xyXG4gICAgICAgIGlmIChhbGlhc2VzKSB7XHJcbiAgICAgICAgICAgIGFsaWFzZXMuZm9yRWFjaChhbGlhcyA9PiB7XHJcbiAgICAgICAgICAgICAgICB0aGlzLl9uYW1lc1Rva2VybmVsTWFwLnNldChhbGlhcy50b0xvd2VyQ2FzZSgpLCBrZXJuZWwpO1xyXG4gICAgICAgICAgICAgICAga2VybmVsTmFtZXMuYWRkKGFsaWFzLnRvTG93ZXJDYXNlKCkpO1xyXG4gICAgICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgICAgIGtlcm5lbC5rZXJuZWxJbmZvLmFsaWFzZXMgPSBhbGlhc2VzO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgdGhpcy5fa2VybmVsVG9OYW1lc01hcC5zZXQoa2VybmVsLCBrZXJuZWxOYW1lcyk7XHJcblxyXG4gICAgICAgIHRoaXMuaG9zdD8uYWRkS2VybmVsSW5mbyhrZXJuZWwsIGtlcm5lbC5rZXJuZWxJbmZvKTtcclxuICAgIH1cclxuXHJcbiAgICBmaW5kS2VybmVsQnlOYW1lKGtlcm5lbE5hbWU6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgaWYgKGtlcm5lbE5hbWUudG9Mb3dlckNhc2UoKSA9PT0gdGhpcy5uYW1lLnRvTG93ZXJDYXNlKCkpIHtcclxuICAgICAgICAgICAgcmV0dXJuIHRoaXM7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gdGhpcy5fbmFtZXNUb2tlcm5lbE1hcC5nZXQoa2VybmVsTmFtZS50b0xvd2VyQ2FzZSgpKTtcclxuICAgIH1cclxuXHJcbiAgICBmaW5kS2VybmVsQnlVcmkodXJpOiBzdHJpbmcpOiBLZXJuZWwgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIGNvbnN0IGtlcm5lbHMgPSBBcnJheS5mcm9tKHRoaXMuX2tlcm5lbFRvTmFtZXNNYXAua2V5cygpKTtcclxuICAgICAgICBmb3IgKGxldCBrZXJuZWwgb2Yga2VybmVscykge1xyXG4gICAgICAgICAgICBpZiAoa2VybmVsLmtlcm5lbEluZm8udXJpID09PSB1cmkpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybiBrZXJuZWw7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGZvciAobGV0IGtlcm5lbCBvZiBrZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIGlmIChrZXJuZWwua2VybmVsSW5mby5yZW1vdGVVcmkgPT09IHVyaSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGtlcm5lbDtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIHVuZGVmaW5lZDtcclxuICAgIH1cclxuXHJcbiAgICBvdmVycmlkZSBoYW5kbGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD4ge1xyXG5cclxuICAgICAgICBsZXQga2VybmVsID0gY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSA9PT0gdGhpcy5uYW1lXHJcbiAgICAgICAgICAgID8gdGhpc1xyXG4gICAgICAgICAgICA6IHRoaXMuZ2V0SGFuZGxpbmdLZXJuZWwoY29tbWFuZEVudmVsb3BlKTtcclxuXHJcbiAgICAgICAgaWYgKGtlcm5lbCA9PT0gdGhpcykge1xyXG4gICAgICAgICAgICByZXR1cm4gc3VwZXIuaGFuZGxlQ29tbWFuZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIH0gZWxzZSBpZiAoa2VybmVsKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBrZXJuZWwuaGFuZGxlQ29tbWFuZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIFByb21pc2UucmVqZWN0KG5ldyBFcnJvcihcIktlcm5lbCBub3QgZm91bmQ6IFwiICsgY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSkpO1xyXG4gICAgfVxyXG5cclxuICAgIG92ZXJyaWRlIGdldEhhbmRsaW5nS2VybmVsKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcblxyXG4gICAgICAgIGlmIChjb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSkge1xyXG4gICAgICAgICAgICBsZXQga2VybmVsID0gdGhpcy5maW5kS2VybmVsQnlVcmkoY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25VcmkpO1xyXG4gICAgICAgICAgICBpZiAoa2VybmVsKSB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGlmICghY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSkge1xyXG4gICAgICAgICAgICBpZiAoc3VwZXIuY2FuSGFuZGxlKGNvbW1hbmRFbnZlbG9wZSkpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybiB0aGlzO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBsZXQgdGFyZ2V0S2VybmVsTmFtZSA9IGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUgPz8gdGhpcy5kZWZhdWx0S2VybmVsTmFtZSA/PyB0aGlzLm5hbWU7XHJcblxyXG4gICAgICAgIGxldCBrZXJuZWwgPSB0aGlzLmZpbmRLZXJuZWxCeU5hbWUodGFyZ2V0S2VybmVsTmFtZSk7XHJcbiAgICAgICAgcmV0dXJuIGtlcm5lbDtcclxuICAgIH1cclxufSIsImltcG9ydCB7IEluc3BlY3RPcHRpb25zIH0gZnJvbSBcInV0aWxcIjtcclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB9IGZyb20gXCIuL2tlcm5lbEludm9jYXRpb25Db250ZXh0XCI7XHJcblxyXG5leHBvcnQgY2xhc3MgQ29uc29sZUNhcHR1cmUgaW1wbGVtZW50cyBjb250cmFjdHMuRGlzcG9zYWJsZSB7XHJcbiAgICBwcml2YXRlIG9yaWdpbmFsQ29uc29sZTogQ29uc29sZTtcclxuICAgIGNvbnN0cnVjdG9yKHByaXZhdGUga2VybmVsSW52b2NhdGlvbkNvbnRleHQ6IEtlcm5lbEludm9jYXRpb25Db250ZXh0KSB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUgPSBjb25zb2xlO1xyXG4gICAgICAgIGNvbnNvbGUgPSA8Q29uc29sZT48YW55PnRoaXM7XHJcbiAgICB9XHJcblxyXG4gICAgYXNzZXJ0KHZhbHVlOiBhbnksIG1lc3NhZ2U/OiBzdHJpbmcsIC4uLm9wdGlvbmFsUGFyYW1zOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmFzc2VydCh2YWx1ZSwgbWVzc2FnZSwgb3B0aW9uYWxQYXJhbXMpO1xyXG4gICAgfVxyXG4gICAgY2xlYXIoKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuY2xlYXIoKTtcclxuICAgIH1cclxuICAgIGNvdW50KGxhYmVsPzogYW55KTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuY291bnQobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgY291bnRSZXNldChsYWJlbD86IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmNvdW50UmVzZXQobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgZGVidWcobWVzc2FnZT86IGFueSwgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuZGVidWcobWVzc2FnZSwgb3B0aW9uYWxQYXJhbXMpO1xyXG4gICAgfVxyXG4gICAgZGlyKG9iajogYW55LCBvcHRpb25zPzogSW5zcGVjdE9wdGlvbnMpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5kaXIob2JqLCBvcHRpb25zKTtcclxuICAgIH1cclxuICAgIGRpcnhtbCguLi5kYXRhOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLmRpcnhtbChkYXRhKTtcclxuICAgIH1cclxuICAgIGVycm9yKG1lc3NhZ2U/OiBhbnksIC4uLm9wdGlvbmFsUGFyYW1zOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMucmVkaXJlY3RBbmRQdWJsaXNoKHRoaXMub3JpZ2luYWxDb25zb2xlLmVycm9yLCAuLi5bbWVzc2FnZSwgLi4ub3B0aW9uYWxQYXJhbXNdKTtcclxuICAgIH1cclxuXHJcbiAgICBncm91cCguLi5sYWJlbDogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5ncm91cChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBncm91cENvbGxhcHNlZCguLi5sYWJlbDogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5ncm91cENvbGxhcHNlZChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBncm91cEVuZCgpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5ncm91cEVuZCgpO1xyXG4gICAgfVxyXG4gICAgaW5mbyhtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLnJlZGlyZWN0QW5kUHVibGlzaCh0aGlzLm9yaWdpbmFsQ29uc29sZS5pbmZvLCAuLi5bbWVzc2FnZSwgLi4ub3B0aW9uYWxQYXJhbXNdKTtcclxuICAgIH1cclxuICAgIGxvZyhtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLnJlZGlyZWN0QW5kUHVibGlzaCh0aGlzLm9yaWdpbmFsQ29uc29sZS5sb2csIC4uLlttZXNzYWdlLCAuLi5vcHRpb25hbFBhcmFtc10pO1xyXG4gICAgfVxyXG5cclxuICAgIHRhYmxlKHRhYnVsYXJEYXRhOiBhbnksIHByb3BlcnRpZXM/OiBzdHJpbmdbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnRhYmxlKHRhYnVsYXJEYXRhLCBwcm9wZXJ0aWVzKTtcclxuICAgIH1cclxuICAgIHRpbWUobGFiZWw/OiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS50aW1lKGxhYmVsKTtcclxuICAgIH1cclxuICAgIHRpbWVFbmQobGFiZWw/OiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS50aW1lRW5kKGxhYmVsKTtcclxuICAgIH1cclxuICAgIHRpbWVMb2cobGFiZWw/OiBzdHJpbmcsIC4uLmRhdGE6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUudGltZUxvZyhsYWJlbCwgZGF0YSk7XHJcbiAgICB9XHJcbiAgICB0aW1lU3RhbXAobGFiZWw/OiBzdHJpbmcpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS50aW1lU3RhbXAobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgdHJhY2UobWVzc2FnZT86IGFueSwgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5yZWRpcmVjdEFuZFB1Ymxpc2godGhpcy5vcmlnaW5hbENvbnNvbGUudHJhY2UsIC4uLlttZXNzYWdlLCAuLi5vcHRpb25hbFBhcmFtc10pO1xyXG4gICAgfVxyXG4gICAgd2FybihtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS53YXJuKG1lc3NhZ2UsIG9wdGlvbmFsUGFyYW1zKTtcclxuICAgIH1cclxuXHJcbiAgICBwcm9maWxlKGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUucHJvZmlsZShsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBwcm9maWxlRW5kKGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUucHJvZmlsZUVuZChsYWJlbCk7XHJcbiAgICB9XHJcblxyXG4gICAgZGlzcG9zZSgpOiB2b2lkIHtcclxuICAgICAgICBjb25zb2xlID0gdGhpcy5vcmlnaW5hbENvbnNvbGU7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSByZWRpcmVjdEFuZFB1Ymxpc2godGFyZ2V0OiAoLi4uYXJnczogYW55W10pID0+IHZvaWQsIC4uLmFyZ3M6IGFueVtdKSB7XHJcbiAgICAgICAgdGFyZ2V0KC4uLmFyZ3MpO1xyXG4gICAgICAgIHRoaXMucHVibGlzaEFyZ3NBc0V2ZW50cyguLi5hcmdzKTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIHB1Ymxpc2hBcmdzQXNFdmVudHMoLi4uYXJnczogYW55W10pIHtcclxuICAgICAgICBmb3IgKGNvbnN0IGFyZyBvZiBhcmdzKSB7XHJcbiAgICAgICAgICAgIGxldCBtaW1lVHlwZTogc3RyaW5nO1xyXG4gICAgICAgICAgICBsZXQgdmFsdWU6IHN0cmluZztcclxuICAgICAgICAgICAgaWYgKHR5cGVvZiBhcmcgIT09ICdvYmplY3QnICYmICFBcnJheS5pc0FycmF5KGFyZykpIHtcclxuICAgICAgICAgICAgICAgIG1pbWVUeXBlID0gJ3RleHQvcGxhaW4nO1xyXG4gICAgICAgICAgICAgICAgdmFsdWUgPSBhcmc/LnRvU3RyaW5nKCk7XHJcbiAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICBtaW1lVHlwZSA9ICdhcHBsaWNhdGlvbi9qc29uJztcclxuICAgICAgICAgICAgICAgIHZhbHVlID0gSlNPTi5zdHJpbmdpZnkoYXJnKTtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgY29uc3QgZGlzcGxheWVkVmFsdWU6IGNvbnRyYWN0cy5EaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICAgICAgZm9ybWF0dGVkVmFsdWVzOiBbXHJcbiAgICAgICAgICAgICAgICAgICAge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBtaW1lVHlwZSxcclxuICAgICAgICAgICAgICAgICAgICAgICAgdmFsdWUsXHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgXVxyXG4gICAgICAgICAgICB9O1xyXG4gICAgICAgICAgICBjb25zdCBldmVudEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSA9IHtcclxuICAgICAgICAgICAgICAgIGV2ZW50VHlwZTogY29udHJhY3RzLkRpc3BsYXllZFZhbHVlUHJvZHVjZWRUeXBlLFxyXG4gICAgICAgICAgICAgICAgZXZlbnQ6IGRpc3BsYXllZFZhbHVlLFxyXG4gICAgICAgICAgICAgICAgY29tbWFuZDogdGhpcy5rZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5jb21tYW5kRW52ZWxvcGVcclxuICAgICAgICAgICAgfTtcclxuXHJcbiAgICAgICAgICAgIHRoaXMua2VybmVsSW52b2NhdGlvbkNvbnRleHQucHVibGlzaChldmVudEVudmVsb3BlKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcbn0iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBDb25zb2xlQ2FwdHVyZSB9IGZyb20gXCIuL2NvbnNvbGVDYXB0dXJlXCI7XHJcbmltcG9ydCAqIGFzIGtlcm5lbCBmcm9tIFwiLi9rZXJuZWxcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcblxyXG5leHBvcnQgY2xhc3MgSmF2YXNjcmlwdEtlcm5lbCBleHRlbmRzIGtlcm5lbC5LZXJuZWwge1xyXG4gICAgcHJpdmF0ZSBzdXBwcmVzc2VkTG9jYWxzOiBTZXQ8c3RyaW5nPjtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcihuYW1lPzogc3RyaW5nKSB7XHJcbiAgICAgICAgc3VwZXIobmFtZSA/PyBcImphdmFzY3JpcHRcIiwgXCJKYXZhc2NyaXB0XCIpO1xyXG4gICAgICAgIHRoaXMuc3VwcHJlc3NlZExvY2FscyA9IG5ldyBTZXQ8c3RyaW5nPih0aGlzLmFsbExvY2FsVmFyaWFibGVOYW1lcygpKTtcclxuICAgICAgICB0aGlzLnJlZ2lzdGVyQ29tbWFuZEhhbmRsZXIoeyBjb21tYW5kVHlwZTogY29udHJhY3RzLlN1Ym1pdENvZGVUeXBlLCBoYW5kbGU6IGludm9jYXRpb24gPT4gdGhpcy5oYW5kbGVTdWJtaXRDb2RlKGludm9jYXRpb24pIH0pO1xyXG4gICAgICAgIHRoaXMucmVnaXN0ZXJDb21tYW5kSGFuZGxlcih7IGNvbW1hbmRUeXBlOiBjb250cmFjdHMuUmVxdWVzdFZhbHVlSW5mb3NUeXBlLCBoYW5kbGU6IGludm9jYXRpb24gPT4gdGhpcy5oYW5kbGVSZXF1ZXN0VmFsdWVJbmZvcyhpbnZvY2F0aW9uKSB9KTtcclxuICAgICAgICB0aGlzLnJlZ2lzdGVyQ29tbWFuZEhhbmRsZXIoeyBjb21tYW5kVHlwZTogY29udHJhY3RzLlJlcXVlc3RWYWx1ZVR5cGUsIGhhbmRsZTogaW52b2NhdGlvbiA9PiB0aGlzLmhhbmRsZVJlcXVlc3RWYWx1ZShpbnZvY2F0aW9uKSB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGFzeW5jIGhhbmRsZVN1Ym1pdENvZGUoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHN1Ym1pdENvZGUgPSA8Y29udHJhY3RzLlN1Ym1pdENvZGU+aW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZDtcclxuICAgICAgICBjb25zdCBjb2RlID0gc3VibWl0Q29kZS5jb2RlO1xyXG5cclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLkNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlLCBldmVudDogeyBjb2RlIH0sIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlIH0pO1xyXG5cclxuICAgICAgICBsZXQgY2FwdHVyZTogY29udHJhY3RzLkRpc3Bvc2FibGUgfCB1bmRlZmluZWQgPSBuZXcgQ29uc29sZUNhcHR1cmUoaW52b2NhdGlvbi5jb250ZXh0KTtcclxuICAgICAgICBsZXQgcmVzdWx0OiBhbnkgPSB1bmRlZmluZWQ7XHJcblxyXG4gICAgICAgIHRyeSB7XHJcbiAgICAgICAgICAgIGNvbnN0IEFzeW5jRnVuY3Rpb24gPSBldmFsKGBPYmplY3QuZ2V0UHJvdG90eXBlT2YoYXN5bmMgZnVuY3Rpb24oKXt9KS5jb25zdHJ1Y3RvcmApO1xyXG4gICAgICAgICAgICBjb25zdCBldmFsdWF0b3IgPSBBc3luY0Z1bmN0aW9uKFwiY29uc29sZVwiLCBjb2RlKTtcclxuICAgICAgICAgICAgcmVzdWx0ID0gYXdhaXQgZXZhbHVhdG9yKGNhcHR1cmUpO1xyXG4gICAgICAgICAgICBpZiAocmVzdWx0ICE9PSB1bmRlZmluZWQpIHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGZvcm1hdHRlZFZhbHVlID0gZm9ybWF0VmFsdWUocmVzdWx0LCAnYXBwbGljYXRpb24vanNvbicpO1xyXG4gICAgICAgICAgICAgICAgY29uc3QgZXZlbnQ6IGNvbnRyYWN0cy5SZXR1cm5WYWx1ZVByb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICAgICAgICAgIGZvcm1hdHRlZFZhbHVlczogW2Zvcm1hdHRlZFZhbHVlXVxyXG4gICAgICAgICAgICAgICAgfTtcclxuICAgICAgICAgICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKHsgZXZlbnRUeXBlOiBjb250cmFjdHMuUmV0dXJuVmFsdWVQcm9kdWNlZFR5cGUsIGV2ZW50LCBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSB9KTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0gY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgY2FwdHVyZS5kaXNwb3NlKCk7XHJcbiAgICAgICAgICAgIGNhcHR1cmUgPSB1bmRlZmluZWQ7XHJcblxyXG4gICAgICAgICAgICB0aHJvdyBlOy8vP1xyXG4gICAgICAgIH1cclxuICAgICAgICBmaW5hbGx5IHtcclxuICAgICAgICAgICAgaWYgKGNhcHR1cmUpIHtcclxuICAgICAgICAgICAgICAgIGNhcHR1cmUuZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgaGFuZGxlUmVxdWVzdFZhbHVlSW5mb3MoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHZhbHVlSW5mb3M6IGNvbnRyYWN0cy5LZXJuZWxWYWx1ZUluZm9bXSA9IHRoaXMuYWxsTG9jYWxWYXJpYWJsZU5hbWVzKCkuZmlsdGVyKHYgPT4gIXRoaXMuc3VwcHJlc3NlZExvY2Fscy5oYXModikpLm1hcCh2ID0+ICh7IG5hbWU6IHYgfSkpO1xyXG4gICAgICAgIGNvbnN0IGV2ZW50OiBjb250cmFjdHMuVmFsdWVJbmZvc1Byb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICB2YWx1ZUluZm9zXHJcbiAgICAgICAgfTtcclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLlZhbHVlSW5mb3NQcm9kdWNlZFR5cGUsIGV2ZW50LCBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSB9KTtcclxuICAgICAgICByZXR1cm4gUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBoYW5kbGVSZXF1ZXN0VmFsdWUoaW52b2NhdGlvbjoga2VybmVsLklLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHJlcXVlc3RWYWx1ZSA9IDxjb250cmFjdHMuUmVxdWVzdFZhbHVlPmludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQ7XHJcbiAgICAgICAgY29uc3QgcmF3VmFsdWUgPSB0aGlzLmdldExvY2FsVmFyaWFibGUocmVxdWVzdFZhbHVlLm5hbWUpO1xyXG4gICAgICAgIGNvbnN0IGZvcm1hdHRlZFZhbHVlID0gZm9ybWF0VmFsdWUocmF3VmFsdWUsIHJlcXVlc3RWYWx1ZS5taW1lVHlwZSB8fCAnYXBwbGljYXRpb24vanNvbicpO1xyXG4gICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHJldHVybmluZyAke0pTT04uc3RyaW5naWZ5KGZvcm1hdHRlZFZhbHVlKX0gZm9yICR7cmVxdWVzdFZhbHVlLm5hbWV9YCk7XHJcbiAgICAgICAgY29uc3QgZXZlbnQ6IGNvbnRyYWN0cy5WYWx1ZVByb2R1Y2VkID0ge1xyXG4gICAgICAgICAgICBuYW1lOiByZXF1ZXN0VmFsdWUubmFtZSxcclxuICAgICAgICAgICAgZm9ybWF0dGVkVmFsdWVcclxuICAgICAgICB9O1xyXG4gICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKHsgZXZlbnRUeXBlOiBjb250cmFjdHMuVmFsdWVQcm9kdWNlZFR5cGUsIGV2ZW50LCBjb21tYW5kOiBpbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSB9KTtcclxuICAgICAgICByZXR1cm4gUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBhbGxMb2NhbFZhcmlhYmxlTmFtZXMoKTogc3RyaW5nW10ge1xyXG4gICAgICAgIGNvbnN0IHJlc3VsdDogc3RyaW5nW10gPSBbXTtcclxuICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICBmb3IgKGNvbnN0IGtleSBpbiBnbG9iYWxUaGlzKSB7XHJcbiAgICAgICAgICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICAgICAgICAgIGlmICh0eXBlb2YgKDxhbnk+Z2xvYmFsVGhpcylba2V5XSAhPT0gJ2Z1bmN0aW9uJykge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICByZXN1bHQucHVzaChrZXkpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIH0gY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihgZXJyb3IgZ2V0dGluZyB2YWx1ZSBmb3IgJHtrZXl9IDogJHtlfWApO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihgZXJyb3Igc2Nhbm5pbmcgZ2xvYmxhIHZhcmlhYmxlcyA6ICR7ZX1gKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiByZXN1bHQ7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBnZXRMb2NhbFZhcmlhYmxlKG5hbWU6IHN0cmluZyk6IGFueSB7XHJcbiAgICAgICAgcmV0dXJuICg8YW55Pmdsb2JhbFRoaXMpW25hbWVdO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gZm9ybWF0VmFsdWUoYXJnOiBhbnksIG1pbWVUeXBlOiBzdHJpbmcpOiBjb250cmFjdHMuRm9ybWF0dGVkVmFsdWUge1xyXG4gICAgbGV0IHZhbHVlOiBzdHJpbmc7XHJcblxyXG4gICAgc3dpdGNoIChtaW1lVHlwZSkge1xyXG4gICAgICAgIGNhc2UgJ3RleHQvcGxhaW4nOlxyXG4gICAgICAgICAgICB2YWx1ZSA9IGFyZz8udG9TdHJpbmcoKSB8fCAndW5kZWZpbmVkJztcclxuICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgY2FzZSAnYXBwbGljYXRpb24vanNvbic6XHJcbiAgICAgICAgICAgIHZhbHVlID0gSlNPTi5zdHJpbmdpZnkoYXJnKTtcclxuICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgZGVmYXVsdDpcclxuICAgICAgICAgICAgdGhyb3cgbmV3IEVycm9yKGB1bnN1cHBvcnRlZCBtaW1lIHR5cGU6ICR7bWltZVR5cGV9YCk7XHJcbiAgICB9XHJcblxyXG4gICAgcmV0dXJuIHtcclxuICAgICAgICBtaW1lVHlwZSxcclxuICAgICAgICB2YWx1ZSxcclxuICAgIH07XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vZ2VuZXJpY0NoYW5uZWxcIjtcclxuaW1wb3J0IHsgSUtlcm5lbENvbW1hbmRIYW5kbGVyLCBJS2VybmVsQ29tbWFuZEludm9jYXRpb24sIEtlcm5lbCB9IGZyb20gXCIuL2tlcm5lbFwiO1xyXG5cclxuZXhwb3J0IGNsYXNzIFByb3h5S2VybmVsIGV4dGVuZHMgS2VybmVsIHtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcihvdmVycmlkZSByZWFkb25seSBuYW1lOiBzdHJpbmcsIHByaXZhdGUgcmVhZG9ubHkgY2hhbm5lbDogY29udHJhY3RzLktlcm5lbENvbW1hbmRBbmRFdmVudENoYW5uZWwpIHtcclxuICAgICAgICBzdXBlcihuYW1lKTtcclxuICAgIH1cclxuICAgIG92ZXJyaWRlIGdldENvbW1hbmRIYW5kbGVyKGNvbW1hbmRUeXBlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZFR5cGUpOiBJS2VybmVsQ29tbWFuZEhhbmRsZXIgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB7XHJcbiAgICAgICAgICAgIGNvbW1hbmRUeXBlLFxyXG4gICAgICAgICAgICBoYW5kbGU6IChpbnZvY2F0aW9uKSA9PiB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4gdGhpcy5fY29tbWFuZEhhbmRsZXIoaW52b2NhdGlvbik7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9O1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgYXN5bmMgX2NvbW1hbmRIYW5kbGVyKGNvbW1hbmRJbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCB0b2tlbiA9IGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS50b2tlbjtcclxuICAgICAgICBjb25zdCBjb21wbGV0aW9uU291cmNlID0gbmV3IFByb21pc2VDb21wbGV0aW9uU291cmNlPGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPigpO1xyXG4gICAgICAgIGxldCBzdWIgPSB0aGlzLmNoYW5uZWwuc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMoKGVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSkgPT4ge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBwcm94eSAke3RoaXMubmFtZX0gZ290IGV2ZW50ICR7SlNPTi5zdHJpbmdpZnkoZW52ZWxvcGUpfWApO1xyXG4gICAgICAgICAgICBpZiAoZW52ZWxvcGUuY29tbWFuZCEudG9rZW4gPT09IHRva2VuKSB7XHJcbiAgICAgICAgICAgICAgICBzd2l0Y2ggKGVudmVsb3BlLmV2ZW50VHlwZSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGNhc2UgY29udHJhY3RzLkNvbW1hbmRGYWlsZWRUeXBlOlxyXG4gICAgICAgICAgICAgICAgICAgIGNhc2UgY29udHJhY3RzLkNvbW1hbmRTdWNjZWVkZWRUeXBlOlxyXG4gICAgICAgICAgICAgICAgICAgICAgICBpZiAoZW52ZWxvcGUuY29tbWFuZCEuaWQgPT09IGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5pZCkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY29tcGxldGlvblNvdXJjZS5yZXNvbHZlKGVudmVsb3BlKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNvbW1hbmRJbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaChlbnZlbG9wZSk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICAgICAgZGVmYXVsdDpcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29tbWFuZEludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKGVudmVsb3BlKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgdHJ5IHtcclxuICAgICAgICAgICAgaWYgKCFjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSB8fCAhY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQub3JpZ2luVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBrZXJuZWxJbmZvID0gdGhpcy5wYXJlbnRLZXJuZWw/Lmhvc3Q/LnRyeUdldEtlcm5lbEluZm8odGhpcyk7XHJcbiAgICAgICAgICAgICAgICBpZiAoa2VybmVsSW5mbykge1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLm9yaWdpblVyaSA/Pz0ga2VybmVsSW5mby51cmk7XHJcbiAgICAgICAgICAgICAgICAgICAgY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25VcmkgPz89IGtlcm5lbEluZm8ucmVtb3RlVXJpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICB0aGlzLmNoYW5uZWwuc3VibWl0Q29tbWFuZChjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBwcm94eSAke3RoaXMubmFtZX0gYWJvdXQgdG8gYXdhaXQgd2l0aCB0b2tlbiAke3Rva2VufWApO1xyXG4gICAgICAgICAgICBjb25zdCBlbnZlbnRFbnZlbG9wZSA9IGF3YWl0IGNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxuICAgICAgICAgICAgaWYgKGVudmVudEVudmVsb3BlLmV2ZW50VHlwZSA9PT0gY29udHJhY3RzLkNvbW1hbmRGYWlsZWRUeXBlKSB7XHJcbiAgICAgICAgICAgICAgICBjb21tYW5kSW52b2NhdGlvbi5jb250ZXh0LmZhaWwoKDxjb250cmFjdHMuQ29tbWFuZEZhaWxlZD5lbnZlbnRFbnZlbG9wZS5ldmVudCkubWVzc2FnZSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgcHJveHkgJHt0aGlzLm5hbWV9IGRvbmUgYXdhaXRpbmcgd2l0aCB0b2tlbiAke3Rva2VufWApO1xyXG4gICAgICAgIH1cclxuICAgICAgICBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICBjb21tYW5kSW52b2NhdGlvbi5jb250ZXh0LmZhaWwoKDxhbnk+ZSkubWVzc2FnZSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGZpbmFsbHkge1xyXG4gICAgICAgICAgICBzdWIuZGlzcG9zZSgpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgQ29tcG9zaXRlS2VybmVsIH0gZnJvbSAnLi9jb21wb3NpdGVLZXJuZWwnO1xyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSAnLi9jb250cmFjdHMnO1xyXG5pbXBvcnQgeyBLZXJuZWwgfSBmcm9tICcuL2tlcm5lbCc7XHJcbmltcG9ydCB7IFByb3h5S2VybmVsIH0gZnJvbSAnLi9wcm94eUtlcm5lbCc7XHJcbmltcG9ydCB7IExvZ2dlciB9IGZyb20gJy4vbG9nZ2VyJztcclxuaW1wb3J0IHsgS2VybmVsU2NoZWR1bGVyIH0gZnJvbSAnLi9rZXJuZWxTY2hlZHVsZXInO1xyXG5cclxuZXhwb3J0IGNsYXNzIEtlcm5lbEhvc3Qge1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfcmVtb3RlVXJpVG9LZXJuZWwgPSBuZXcgTWFwPHN0cmluZywgS2VybmVsPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfdXJpVG9LZXJuZWwgPSBuZXcgTWFwPHN0cmluZywgS2VybmVsPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfa2VybmVsVG9LZXJuZWxJbmZvID0gbmV3IE1hcDxLZXJuZWwsIGNvbnRyYWN0cy5LZXJuZWxJbmZvPigpO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfdXJpOiBzdHJpbmc7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9zY2hlZHVsZXI6IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPjtcclxuXHJcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIHJlYWRvbmx5IF9rZXJuZWw6IENvbXBvc2l0ZUtlcm5lbCwgcHJpdmF0ZSByZWFkb25seSBfY2hhbm5lbDogY29udHJhY3RzLktlcm5lbENvbW1hbmRBbmRFdmVudENoYW5uZWwsIGhvc3RVcmk6IHN0cmluZykge1xyXG4gICAgICAgIHRoaXMuX3VyaSA9IGhvc3RVcmkgfHwgXCJrZXJuZWw6Ly92c2NvZGVcIjtcclxuICAgICAgICB0aGlzLl9rZXJuZWwuaG9zdCA9IHRoaXM7XHJcbiAgICAgICAgdGhpcy5fc2NoZWR1bGVyID0gbmV3IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPigpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlHZXRLZXJuZWxCeVJlbW90ZVVyaShyZW1vdGVVcmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX3JlbW90ZVVyaVRvS2VybmVsLmdldChyZW1vdGVVcmkpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlnZXRLZXJuZWxCeU9yaWdpblVyaShvcmlnaW5Vcmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX3VyaVRvS2VybmVsLmdldChvcmlnaW5VcmkpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlHZXRLZXJuZWxJbmZvKGtlcm5lbDogS2VybmVsKTogY29udHJhY3RzLktlcm5lbEluZm8gfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9rZXJuZWxUb0tlcm5lbEluZm8uZ2V0KGtlcm5lbCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGFkZEtlcm5lbEluZm8oa2VybmVsOiBLZXJuZWwsIGtlcm5lbEluZm86IGNvbnRyYWN0cy5LZXJuZWxJbmZvKSB7XHJcblxyXG4gICAgICAgIGtlcm5lbEluZm8udXJpID0gYCR7dGhpcy5fdXJpfS8ke2tlcm5lbC5uYW1lfWA7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsVG9LZXJuZWxJbmZvLnNldChrZXJuZWwsIGtlcm5lbEluZm8pO1xyXG4gICAgICAgIHRoaXMuX3VyaVRvS2VybmVsLnNldChrZXJuZWxJbmZvLnVyaSwga2VybmVsKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZ2V0S2VybmVsKGtlcm5lbENvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IEtlcm5lbCB7XHJcblxyXG4gICAgICAgIGlmIChrZXJuZWxDb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSkge1xyXG4gICAgICAgICAgICBsZXQgZnJvbURlc3RpbmF0aW9uVXJpID0gdGhpcy5fdXJpVG9LZXJuZWwuZ2V0KGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpLnRvTG93ZXJDYXNlKCkpO1xyXG4gICAgICAgICAgICBpZiAoZnJvbURlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBLZXJuZWwgJHtmcm9tRGVzdGluYXRpb25VcmkubmFtZX0gZm91bmQgZm9yIGRlc3RpbmF0aW9uIHVyaSAke2tlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpfWApO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZyb21EZXN0aW5hdGlvblVyaTtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgZnJvbURlc3RpbmF0aW9uVXJpID0gdGhpcy5fcmVtb3RlVXJpVG9LZXJuZWwuZ2V0KGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpLnRvTG93ZXJDYXNlKCkpO1xyXG4gICAgICAgICAgICBpZiAoZnJvbURlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBLZXJuZWwgJHtmcm9tRGVzdGluYXRpb25VcmkubmFtZX0gZm91bmQgZm9yIGRlc3RpbmF0aW9uIHVyaSAke2tlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpfWApO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZyb21EZXN0aW5hdGlvblVyaTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLm9yaWdpblVyaSkge1xyXG4gICAgICAgICAgICBsZXQgZnJvbU9yaWdpblVyaSA9IHRoaXMuX3VyaVRvS2VybmVsLmdldChrZXJuZWxDb21tYW5kRW52ZWxvcGUuY29tbWFuZC5vcmlnaW5VcmkudG9Mb3dlckNhc2UoKSk7XHJcbiAgICAgICAgICAgIGlmIChmcm9tT3JpZ2luVXJpKSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBLZXJuZWwgJHtmcm9tT3JpZ2luVXJpLm5hbWV9IGZvdW5kIGZvciBvcmlnaW4gdXJpICR7a2VybmVsQ29tbWFuZEVudmVsb3BlLmNvbW1hbmQub3JpZ2luVXJpfWApO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZyb21PcmlnaW5Vcmk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYFVzaW5nIEtlcm5lbCAke3RoaXMuX2tlcm5lbC5uYW1lfWApO1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9rZXJuZWw7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHJlZ2lzdGVyUmVtb3RlVXJpRm9yUHJveHkocHJveHlMb2NhbEtlcm5lbE5hbWU6IHN0cmluZywgcmVtb3RlVXJpOiBzdHJpbmcpIHtcclxuICAgICAgICBjb25zdCBrZXJuZWwgPSB0aGlzLl9rZXJuZWwuZmluZEtlcm5lbEJ5TmFtZShwcm94eUxvY2FsS2VybmVsTmFtZSk7XHJcbiAgICAgICAgaWYgKCEoa2VybmVsIGFzIFByb3h5S2VybmVsKSkge1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoYEtlcm5lbCAke3Byb3h5TG9jYWxLZXJuZWxOYW1lfSBpcyBub3QgYSBwcm94eSBrZXJuZWxgKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGNvbnN0IGtlcm5lbGluZm8gPSB0aGlzLl9rZXJuZWxUb0tlcm5lbEluZm8uZ2V0KGtlcm5lbCEpO1xyXG5cclxuICAgICAgICBpZiAoIWtlcm5lbGluZm8pIHtcclxuICAgICAgICAgICAgdGhyb3cgbmV3IEVycm9yKFwia2VybmVsaW5mbyBub3QgZm91bmRcIik7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGlmIChrZXJuZWxpbmZvPy5yZW1vdGVVcmkpIHtcclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgUmVtb3ZpbmcgcmVtb3RlIHVyaSAke2tlcm5lbGluZm8ucmVtb3RlVXJpfSBmb3IgcHJveHkga2VybmVsICR7a2VybmVsaW5mby5sb2NhbE5hbWV9YCk7XHJcbiAgICAgICAgICAgIHRoaXMuX3JlbW90ZVVyaVRvS2VybmVsLmRlbGV0ZShrZXJuZWxpbmZvLnJlbW90ZVVyaS50b0xvd2VyQ2FzZSgpKTtcclxuICAgICAgICB9XHJcbiAgICAgICAga2VybmVsaW5mby5yZW1vdGVVcmkgPSByZW1vdGVVcmk7XHJcblxyXG4gICAgICAgIGlmIChrZXJuZWwpIHtcclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgUmVnaXN0ZXJpbmcgcmVtb3RlIHVyaSAke3JlbW90ZVVyaX0gZm9yIHByb3h5IGtlcm5lbCAke2tlcm5lbGluZm8ubG9jYWxOYW1lfWApO1xyXG4gICAgICAgICAgICB0aGlzLl9yZW1vdGVVcmlUb0tlcm5lbC5zZXQocmVtb3RlVXJpLnRvTG93ZXJDYXNlKCksIGtlcm5lbCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjcmVhdGVQcm94eUtlcm5lbE9uRGVmYXVsdENvbm5lY3RvcihrZXJuZWxJbmZvOiBjb250cmFjdHMuS2VybmVsSW5mbyk6IFByb3h5S2VybmVsIHtcclxuICAgICAgICBjb25zdCBwcm94eUtlcm5lbCA9IG5ldyBQcm94eUtlcm5lbChrZXJuZWxJbmZvLmxvY2FsTmFtZSwgdGhpcy5fY2hhbm5lbCk7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsLmFkZChwcm94eUtlcm5lbCwga2VybmVsSW5mby5hbGlhc2VzKTtcclxuICAgICAgICBpZiAoa2VybmVsSW5mby5yZW1vdGVVcmkpIHtcclxuICAgICAgICAgICAgdGhpcy5yZWdpc3RlclJlbW90ZVVyaUZvclByb3h5KHByb3h5S2VybmVsLm5hbWUsIGtlcm5lbEluZm8ucmVtb3RlVXJpKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIHByb3h5S2VybmVsO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjb25uZWN0KCkge1xyXG4gICAgICAgIHRoaXMuX2NoYW5uZWwuc2V0Q29tbWFuZEhhbmRsZXIoKGtlcm5lbENvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSkgPT4ge1xyXG4gICAgICAgICAgICAvLyBmaXJlIGFuZCBmb3JnZXQgdGhpcyBvbmVcclxuICAgICAgICAgICAgdGhpcy5fc2NoZWR1bGVyLnJ1bkFzeW5jKGtlcm5lbENvbW1hbmRFbnZlbG9wZSwgY29tbWFuZEVudmVsb3BlID0+IHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGtlcm5lbCA9IHRoaXMuZ2V0S2VybmVsKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4ga2VybmVsLnNlbmQoY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgICAgIHJldHVybiBQcm9taXNlLnJlc29sdmUoKTtcclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgdGhpcy5fa2VybmVsLnN1YnNjcmliZVRvS2VybmVsRXZlbnRzKGUgPT4ge1xyXG4gICAgICAgICAgICB0aGlzLl9jaGFubmVsLnB1Ymxpc2hLZXJuZWxFdmVudChlKTtcclxuICAgICAgICB9KTtcclxuICAgIH1cclxufSIsImltcG9ydCB7IENvbXBvc2l0ZUtlcm5lbCB9IGZyb20gXCIuL2NvbXBvc2l0ZUtlcm5lbFwiO1xyXG5pbXBvcnQgeyBKYXZhc2NyaXB0S2VybmVsIH0gZnJvbSBcIi4vamF2YXNjcmlwdEtlcm5lbFwiO1xyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIHNldHVwKCkge1xyXG4gICAgbGV0IGNvbXBvc2l0ZUtlcm5lbCA9IG5ldyBDb21wb3NpdGVLZXJuZWwoXCJicm93c2VyXCIpO1xyXG5cclxuICAgIGNvbnN0IGpzS2VybmVsID0gbmV3IEphdmFzY3JpcHRLZXJuZWwoKTtcclxuXHJcbiAgICBjb21wb3NpdGVLZXJuZWwuYWRkKGpzS2VybmVsLCBbIFwianNcIiBdKTtcclxuXHJcbiAgICAvLyBAdHMtaWdub3JlXHJcbiAgICBpZiAocHVibGlzaENvbW1hbmRPckV2ZW50KSB7XHJcbiAgICAgICAgY29tcG9zaXRlS2VybmVsLnN1YnNjcmliZVRvS2VybmVsRXZlbnRzKGVudmVsb3BlID0+IHtcclxuICAgICAgICAgICAgLy8gQHRzLWlnbm9yZVxyXG4gICAgICAgICAgICBwdWJsaXNoQ29tbWFuZE9yRXZlbnQoZW52ZWxvcGUpO1xyXG4gICAgICAgIH0pO1xyXG4gICAgfVxyXG59Il0sIm5hbWVzIjpbIkluc2VydFRleHRGb3JtYXQiLCJEaWFnbm9zdGljU2V2ZXJpdHkiLCJEb2N1bWVudFNlcmlhbGl6YXRpb25UeXBlIiwiUmVxdWVzdFR5cGUiLCJTdWJtaXNzaW9uVHlwZSIsIkxvZ0xldmVsIiwidXRpbGl0aWVzLmlzS2VybmVsQ29tbWFuZEVudmVsb3BlIiwidXRpbGl0aWVzLmlzS2VybmVsRXZlbnRFbnZlbG9wZSIsImNvbnRyYWN0cy5SZXF1ZXN0S2VybmVsSW5mb1R5cGUiLCJjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSIsImNvbnRyYWN0cy5Db21tYW5kRmFpbGVkVHlwZSIsImNvbnRyYWN0cy5Db21tYW5kU3VjY2VlZGVkVHlwZSIsImNvbnRyYWN0cy5EaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkVHlwZSIsImtlcm5lbC5LZXJuZWwiLCJjb250cmFjdHMuU3VibWl0Q29kZVR5cGUiLCJjb250cmFjdHMuUmVxdWVzdFZhbHVlSW5mb3NUeXBlIiwiY29udHJhY3RzLlJlcXVlc3RWYWx1ZVR5cGUiLCJjb250cmFjdHMuQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGUiLCJjb250cmFjdHMuUmV0dXJuVmFsdWVQcm9kdWNlZFR5cGUiLCJjb250cmFjdHMuVmFsdWVJbmZvc1Byb2R1Y2VkVHlwZSIsImNvbnRyYWN0cy5WYWx1ZVByb2R1Y2VkVHlwZSJdLCJtYXBwaW5ncyI6Ijs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7OztJQUFBO0lBQ0E7SUFFQTtJQUVBO0FBRU8sVUFBTSxjQUFjLEdBQUcsYUFBYTtBQUNwQyxVQUFNLFVBQVUsR0FBRyxTQUFTO0FBQzVCLFVBQU0sMEJBQTBCLEdBQUcseUJBQXlCO0FBQzVELFVBQU0sa0JBQWtCLEdBQUcsaUJBQWlCO0FBQzVDLFVBQU0sZ0JBQWdCLEdBQUcsZUFBZTtBQUN4QyxVQUFNLGdCQUFnQixHQUFHLGVBQWU7QUFDeEMsVUFBTSxnQkFBZ0IsR0FBRyxlQUFlO0FBQ3hDLFVBQU0sZUFBZSxHQUFHLGNBQWM7QUFDdEMsVUFBTSxRQUFRLEdBQUcsT0FBTztBQUN4QixVQUFNLHNCQUFzQixHQUFHLHFCQUFxQjtBQUNwRCxVQUFNLHNCQUFzQixHQUFHLHFCQUFxQjtBQUNwRCxVQUFNLG9CQUFvQixHQUFHLG1CQUFtQjtBQUNoRCxVQUFNLGdCQUFnQixHQUFHLGVBQWU7QUFDeEMsVUFBTSxxQkFBcUIsR0FBRyxvQkFBb0I7QUFDbEQsVUFBTSx3QkFBd0IsR0FBRyx1QkFBdUI7QUFDeEQsVUFBTSxnQkFBZ0IsR0FBRyxlQUFlO0FBQ3hDLFVBQU0scUJBQXFCLEdBQUcsb0JBQW9CO0FBQ2xELFVBQU0sb0JBQW9CLEdBQUcsbUJBQW1CO0FBQ2hELFVBQU0sY0FBYyxHQUFHLGFBQWE7QUFDcEMsVUFBTSx3QkFBd0IsR0FBRyx1QkFBdUI7SUF3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

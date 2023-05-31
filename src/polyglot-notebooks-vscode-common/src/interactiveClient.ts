// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandFailed,
    CommandFailedType,
    CommandSucceededType,
    CompletionsProduced,
    CompletionsProducedType,
    Diagnostic,
    DiagnosticsProduced,
    DiagnosticsProducedType,
    DisplayEvent,
    DisplayedValueProducedType,
    DisplayedValueUpdatedType,
    HoverTextProduced,
    HoverTextProducedType,
    KernelCommand,
    KernelCommandType,
    KernelEvent,
    KernelEventEnvelope,
    KernelEventEnvelopeObserver,
    KernelEventType,
    KernelInfoProduced,
    KernelInfoProducedType,
    Quit,
    QuitType,
    RequestCompletions,
    RequestCompletionsType,
    RequestDiagnostics,
    RequestDiagnosticsType,
    RequestHoverText,
    RequestHoverTextType,
    RequestSignatureHelp,
    RequestSignatureHelpType,
    RequestValue,
    RequestValueType,
    RequestValueInfos,
    RequestValueInfosType,
    ReturnValueProducedType,
    SignatureHelpProduced,
    SignatureHelpProducedType,
    StandardErrorValueProducedType,
    StandardOutputValueProducedType,
    ValueInfosProduced,
    ValueInfosProducedType,
    ValueProduced,
    ValueProducedType,
    SubmissionType,
    SubmitCode,
    SubmitCodeType,
    CancelType,
    Cancel,
    ErrorProducedType,
    ErrorProduced,
    KernelInfo
} from './polyglot-notebooks/commandsAndEvents';
import { clearDebounce, createOutput } from './utilities';

import * as vscodeLike from './interfaces/vscode-like';
import { CompositeKernel } from './polyglot-notebooks/compositeKernel';
import { KernelHost } from './polyglot-notebooks/kernelHost';
import { KernelCommandAndEventChannel } from './DotnetInteractiveChannel';
import * as connection from './polyglot-notebooks/connection';
import { DisposableSubscription } from './polyglot-notebooks/disposables';
import { TokenGenerator } from './polyglot-notebooks/tokenGenerator';

export interface ErrorOutputCreator {
    (message: string, outputId?: string): vscodeLike.NotebookCellOutput;
}

export interface InteractiveClientConfiguration {
    readonly channel: KernelCommandAndEventChannel,
    readonly createErrorOutput: ErrorOutputCreator,
    readonly kernelInfos: Array<KernelInfo>
}

export class InteractiveClient {
    private _tokenGenerator = new TokenGenerator();
    private disposables: (() => void)[] = [];
    private nextExecutionCount = 1;
    private nextOutputId: number = 1;
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEnvelopeObserver>> = new Map<string, Array<KernelEventEnvelopeObserver>>();
    private deferredOutput: Array<vscodeLike.NotebookCellOutput> = [];
    private _kernel: CompositeKernel;
    private _kernelHost: KernelHost;
    constructor(readonly config: InteractiveClientConfiguration) {
        this._kernel = new CompositeKernel("vscode");
        this._kernelHost = new KernelHost(this._kernel, config.channel.sender, config.channel.receiver, "kernel://vscode");

        config.channel.receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    this.eventListener(envelope);

                    if (envelope.eventType === KernelInfoProducedType) {
                        const kernelInfoProduced = <KernelInfoProduced>envelope.event;
                        connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced.kernelInfo, this._kernel);
                    }
                }
            }
        });

        for (const kernelInfo of config.kernelInfos) {
            const remoteHostUri = connection.extractHostAndNomalize(kernelInfo.isProxy ? kernelInfo.remoteUri! : kernelInfo.uri);
            this._kernelHost.defaultConnector.addRemoteHostUri(remoteHostUri);
            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, this._kernel);
        }

        this._kernelHost.connect();
    }

    get kernel(): CompositeKernel {
        return this._kernel;
    }

    get kernelHost(): KernelHost {
        return this._kernelHost;
    }

    get channel(): KernelCommandAndEventChannel {
        return this.config.channel;
    }

    public tryGetProperty<T>(propertyName: string): T | null {
        try {
            return <T>((<any>this.config.channel)[propertyName]);
        }
        catch {
            return null;
        }
    }

    private clearExistingLanguageServiceRequests(requestId: string) {
        clearDebounce(requestId);
        clearDebounce(`completion-${requestId}`);
        clearDebounce(`diagnostics-${requestId}`);
        clearDebounce(`hover-${requestId}`);
        clearDebounce(`sighelp-${requestId}`);
    }

    execute(source: string, language: string, outputReporter: { (output: vscodeLike.NotebookCellOutput): void }, diagnosticObserver: (diags: Array<Diagnostic>) => void, configuration: { token?: string | undefined, id?: string | undefined } | undefined): Promise<boolean> {
        if (configuration !== undefined && configuration.id !== undefined) {
            this.clearExistingLanguageServiceRequests(configuration.id);
        }
        return new Promise((resolve, reject) => {
            let diagnostics: Array<Diagnostic> = [];

            let reportDiagnostics = () => {
                diagnosticObserver(diagnostics);
            };

            let failureReported = false;
            const commandToken = configuration?.token ? configuration.token : this._tokenGenerator.createToken();
            const commandId = this._tokenGenerator.createId();
            try {
                return this.submitCode(source, language, eventEnvelope => {
                    if (this.deferredOutput.length > 0) {
                        for (const output of this.deferredOutput) {
                            outputReporter(output);
                        }
                        this.deferredOutput = [];
                    }

                    switch (eventEnvelope.eventType) {
                        // if kernel languages were added, handle those events here
                        case CommandSucceededType:
                            if (eventEnvelope.command?.id === commandId) {
                                // only complete this promise if it's the root command
                                resolve(!failureReported);
                            }
                            break;
                        case CommandFailedType:
                            {
                                const err = <CommandFailed>eventEnvelope.event;
                                const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                                outputReporter(errorOutput);
                                failureReported = true;
                                if (eventEnvelope.command?.id === commandId) {
                                    // only complete this promise if it's the root command
                                    reject(err);
                                }
                            }
                            break;
                        case ErrorProducedType: {
                            const err = <ErrorProduced>eventEnvelope.event;
                            const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                            outputReporter(errorOutput);
                            failureReported = true;
                        }
                        case DiagnosticsProducedType:
                            {
                                const diags = <DiagnosticsProduced>eventEnvelope.event;
                                diagnostics.push(...(diags.diagnostics ?? []));
                                reportDiagnostics();
                            }
                            break;
                        case StandardErrorValueProducedType:
                        case StandardOutputValueProducedType:
                            {
                                const disp = <DisplayEvent>eventEnvelope.event;
                                const stream = eventEnvelope.eventType === StandardErrorValueProducedType ? 'stderr' : 'stdout';
                                const output = this.displayEventToCellOutput(disp, stream);
                                outputReporter(output);
                            }
                            break;
                        case DisplayedValueProducedType:
                        case DisplayedValueUpdatedType:
                        case ReturnValueProducedType:
                            {
                                const disp = <DisplayEvent>eventEnvelope.event;
                                const output = this.displayEventToCellOutput(disp);
                                outputReporter(output);
                            }
                            break;
                    }
                }, commandToken, commandId).catch(e => {
                    // only report a failure if it's not a `CommandFailed` event from above (which has already called `reject()`)
                    if (!failureReported) {
                        const errorMessage = typeof e?.message === 'string' ? <string>e.message : '' + e;
                        const errorOutput = this.config.createErrorOutput(errorMessage, this.getNextOutputId());
                        outputReporter(errorOutput);
                        reject(e);
                    }
                });
            }
            catch (e) {
                reject(e);
            }
        });

    }

    completion(kernelName: string, code: string, line: number, character: number, token?: string | undefined): Promise<CompletionsProduced> {
        let command: RequestCompletions = {
            code: code,
            linePosition: {
                line,
                character
            },
            targetKernelName: kernelName
        };
        return this.submitCommandAndGetResult<CompletionsProduced>(command, RequestCompletionsType, CompletionsProducedType, token);
    }

    hover(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<HoverTextProduced> {
        let command: RequestHoverText = {
            code: code,
            linePosition: {
                line: line,
                character: character,
            },
            targetKernelName: language
        };
        return this.submitCommandAndGetResult<HoverTextProduced>(command, RequestHoverTextType, HoverTextProducedType, token);
    }

    signatureHelp(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<SignatureHelpProduced> {
        let command: RequestSignatureHelp = {
            code,
            linePosition: {
                line,
                character
            },
            targetKernelName: language
        };
        return this.submitCommandAndGetResult<SignatureHelpProduced>(command, RequestSignatureHelpType, SignatureHelpProducedType, token);
    }

    async getDiagnostics(kernelName: string, code: string, token?: string | undefined): Promise<Array<Diagnostic>> {
        const command: RequestDiagnostics = {
            code,
            targetKernelName: kernelName
        };
        const diagsProduced = await this.submitCommandAndGetResult<DiagnosticsProduced>(command, RequestDiagnosticsType, DiagnosticsProducedType, token);
        return diagsProduced.diagnostics;
    }

    async submitCode(code: string, language: string, observer: KernelEventEnvelopeObserver, token?: string | undefined, id?: string | undefined): Promise<DisposableSubscription> {
        let command: SubmitCode = {
            code: code,
            submissionType: SubmissionType.Run,
            targetKernelName: language
        };
        token = token || this._tokenGenerator.createToken();
        id = id || this._tokenGenerator.createId();

        let disposable = this.subscribeToKernelTokenEvents(token, observer);
        try {
            await this.submitCommand(command, SubmitCodeType, token, id);
        }
        catch (error) {
            return Promise.reject(error);

        }
        return disposable;
    }

    requestValueInfos(kernelName: string): Promise<ValueInfosProduced> {
        const command: RequestValueInfos = {
            targetKernelName: kernelName,
            mimeType: "text/plain+summary"
        };
        return this.submitCommandAndGetResult(command, RequestValueInfosType, ValueInfosProducedType, undefined);
    }

    requestValue(valueName: string, kernelName: string): Promise<ValueProduced> {
        const command: RequestValue = {
            name: valueName,
            mimeType: 'text/plain',
            targetKernelName: kernelName,
        };
        return this.submitCommandAndGetResult(command, RequestValueType, ValueProducedType, undefined);
    }

    cancel(token?: string | undefined): Promise<void> {
        let command: Cancel = {};
        token = token || this._tokenGenerator.createToken();
        return this.submitCommand(command, CancelType, token, undefined);
    }

    dispose() {
        const command: Quit = {};
        this.config.channel.sender.send({
            commandType: QuitType,
            command,
        });
        this.config.channel.dispose();
        for (let disposable of this.disposables) {
            disposable();
        }
    }

    public registerForDisposal(disposable: () => void) {
        this.disposables.push(disposable);
    }

    private submitCommandAndGetResult<TEvent extends KernelEvent>(command: KernelCommand, commandType: KernelCommandType, expectedEventType: KernelEventType, token: string | undefined): Promise<TEvent> {
        return new Promise<TEvent>(async (resolve, reject) => {
            let handled = false;
            token = token || this._tokenGenerator.createToken();
            const id = this._tokenGenerator.createId();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                if (eventEnvelope.command?.token === token && eventEnvelope.eventType === expectedEventType) {
                    switch (eventEnvelope.eventType) {
                        case CommandFailedType:
                            if (!handled) {
                                handled = true;
                                disposable.dispose();
                                let err = <CommandFailed>eventEnvelope.event;
                                reject(err);
                            }
                            break;
                        case CommandSucceededType:
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
                                let event = <TEvent>eventEnvelope.event;
                                resolve(event);
                            }
                            break;
                    }
                }
            });
            await this.config.channel.sender.send({ command, commandType, token, id });
        });
    }

    private submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string | undefined, id: string | undefined): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            let failureReported = false;
            token = token || this._tokenGenerator.createToken();
            id = id || this._tokenGenerator.createId();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case CommandFailedType:
                        let err = <CommandFailed>eventEnvelope.event;
                        failureReported = true;
                        if (eventEnvelope.command?.id === id) {
                            disposable.dispose();
                            reject(err);
                        }
                        break;
                    case CommandSucceededType:
                        if (eventEnvelope.command?.id === id) {
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
            } catch (error) {
                reject(error);
            }
        });
    }

    private subscribeToKernelTokenEvents(token: string, observer: KernelEventEnvelopeObserver): DisposableSubscription {
        if (!this.tokenEventObservers.get(token)) {
            this.tokenEventObservers.set(token, []);
        }

        this.tokenEventObservers.get(token)?.push(observer);
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

    private eventListener(eventEnvelope: KernelEventEnvelope) {
        let token = eventEnvelope.command?.token;
        if (token) {
            if (token.startsWith("deferredCommand::")) {
                switch (eventEnvelope.eventType) {
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        let disp = <DisplayEvent>eventEnvelope.event;
                        let output = this.displayEventToCellOutput(disp);
                        this.deferredOutput.push(output);
                        break;
                }
            } else {
                const tokenParts = token.split('.');
                for (let i = tokenParts.length; i >= 1; i--) {
                    const candidateToken = tokenParts.slice(0, i).join('.');
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

    private displayEventToCellOutput(disp: DisplayEvent, stream?: 'stdout' | 'stderr'): vscodeLike.NotebookCellOutput {

        const encoder = new TextEncoder();
        const outputItems: Array<vscodeLike.NotebookCellOutputItem> = [];
        if (disp.formattedValues && disp.formattedValues.length > 0) {
            for (let formatted of disp.formattedValues) {
                let data = this.IsEncodedMimeType(formatted.mimeType)
                    ? Buffer.from(formatted.value, 'base64')
                    : encoder.encode(formatted.value);
                const outputItem: vscodeLike.NotebookCellOutputItem = {
                    mime: formatted.mimeType,
                    data
                };
                if (stream) {
                    outputItem.stream = stream;
                }
                outputItems.push(outputItem);
            }
        }

        const outputId = disp.valueId ?? this.getNextOutputId();
        const output = createOutput(outputItems, outputId);
        return output;
    }

    private IsEncodedMimeType(mimeType: string): boolean {
        const encdodedMimetypes = new Set<string>(["image/png", "image/jpeg", "image/gif"]);
        return encdodedMimetypes.has(mimeType);
    }

    resetExecutionCount() {
        this.nextExecutionCount = 1;
    }

    getNextExecutionCount(): number {
        const next = this.nextExecutionCount;
        this.nextExecutionCount++;
        return next;
    }

    private getNextOutputId(): string {
        return (this.nextOutputId++).toString();
    }


}

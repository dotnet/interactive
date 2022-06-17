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
    DisposableSubscription,
    HoverTextProduced,
    HoverTextProducedType,
    KernelCommand,
    KernelCommandType,
    KernelEvent,
    KernelEventEnvelope,
    KernelEventEnvelopeObserver,
    KernelEventType,
    KernelCommandAndEventChannel,
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
    Cancel
} from './dotnet-interactive/contracts';
import { clearDebounce, createOutput } from './utilities';

import * as vscodeLike from './interfaces/vscode-like';
import { CompositeKernel } from './dotnet-interactive/compositeKernel';
import { Guid } from './dotnet-interactive/tokenGenerator';
import { KernelHost } from './dotnet-interactive/kernelHost';

export interface ErrorOutputCreator {
    (message: string, outputId?: string): vscodeLike.NotebookCellOutput;
}

export interface InteractiveClientConfiguration {
    readonly channel: KernelCommandAndEventChannel,
    readonly createErrorOutput: ErrorOutputCreator,
}

export class InteractiveClient {
    private nextOutputId: number = 1;
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEnvelopeObserver>> = new Map<string, Array<KernelEventEnvelopeObserver>>();
    private deferredOutput: Array<vscodeLike.NotebookCellOutput> = [];
    private valueIdMap: Map<string, { idx: number, outputs: Array<vscodeLike.NotebookCellOutput>, observer: { (outputs: Array<vscodeLike.NotebookCellOutput>): void } }> = new Map<string, { idx: number, outputs: Array<vscodeLike.NotebookCellOutput>, observer: { (outputs: Array<vscodeLike.NotebookCellOutput>): void } }>();
    private _kernel: CompositeKernel;
    private _kernelHost: KernelHost;
    constructor(readonly config: InteractiveClientConfiguration) {
        config.channel.subscribeToKernelEvents(eventEnvelope => this.eventListener(eventEnvelope));

        this._kernel = new CompositeKernel("vscode");
        this._kernelHost = new KernelHost(this._kernel, config.channel, "kernel://vscode");

        this._kernelHost.createProxyKernelOnDefaultConnector({ localName: 'csharp', aliases: ['c#', 'C#'], supportedDirectives: [], supportedKernelCommands: [] });
        this._kernelHost.createProxyKernelOnDefaultConnector({ localName: 'fsharp', aliases: ['fs', 'F#'], supportedDirectives: [], supportedKernelCommands: [] });
        this._kernelHost.createProxyKernelOnDefaultConnector({ localName: 'pwsh', aliases: ['powershell'], supportedDirectives: [], supportedKernelCommands: [] });
        this._kernelHost.createProxyKernelOnDefaultConnector({ localName: 'mermaid', aliases: [], supportedDirectives: [], supportedKernelCommands: [] });

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

    execute(source: string, language: string, outputObserver: { (outputs: Array<vscodeLike.NotebookCellOutput>): void }, diagnosticObserver: (diags: Array<Diagnostic>) => void, configuration: { token?: string | undefined, id?: string | undefined } | undefined): Promise<void> {
        if (configuration !== undefined && configuration.id !== undefined) {
            this.clearExistingLanguageServiceRequests(configuration.id);
        }
        return new Promise((resolve, reject) => {
            let diagnostics: Array<Diagnostic> = [];
            let outputs: Array<vscodeLike.NotebookCellOutput> = [];

            let reportDiagnostics = () => {
                diagnosticObserver(diagnostics);
            };

            let reportOutputs = () => {
                outputObserver(outputs);
            };

            let failureReported = false;
            const commandToken = configuration?.token ? configuration.token : this.getNextToken();
            const commandId = Guid.create().toString();

            return this.submitCode(source, language, eventEnvelope => {
                if (this.deferredOutput.length > 0) {
                    outputs.push(...this.deferredOutput);
                    this.deferredOutput = [];
                }

                switch (eventEnvelope.eventType) {
                    // if kernel languages were added, handle those events here
                    case CommandSucceededType:
                        if (eventEnvelope.command?.id === commandId) {
                            // only complete this promise if it's the root command
                            resolve();
                        }
                        break;
                    case CommandFailedType:
                        {
                            const err = <CommandFailed>eventEnvelope.event;
                            const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                            outputs.push(errorOutput);
                            reportOutputs();
                            failureReported = true;
                            if (eventEnvelope.command?.id === commandId) {
                                // only complete this promise if it's the root command
                                reject(err);
                            }
                        }
                        break;
                    case DiagnosticsProducedType:
                        {
                            const diags = <DiagnosticsProduced>eventEnvelope.event;
                            diagnostics.push(...diags.diagnostics);
                            reportDiagnostics();
                        }
                        break;
                    case StandardErrorValueProducedType:
                    case StandardOutputValueProducedType:
                        {
                            let disp = <DisplayEvent>eventEnvelope.event;
                            const stream = eventEnvelope.eventType === StandardErrorValueProducedType ? 'stderr' : 'stdout';
                            let output = this.displayEventToCellOutput(disp, stream);
                            outputs.push(output);
                            reportOutputs();
                        }
                        break;
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        {
                            let disp = <DisplayEvent>eventEnvelope.event;
                            let output = this.displayEventToCellOutput(disp);

                            if (disp.valueId) {
                                let valueId = this.valueIdMap.get(disp.valueId);
                                if (valueId !== undefined) {
                                    // update existing value
                                    valueId.outputs[valueId.idx] = output;
                                    valueId.observer(valueId.outputs);
                                    // don't report through regular channels
                                    break;
                                } else {
                                    // add new tracked value
                                    this.valueIdMap.set(disp.valueId, {
                                        idx: outputs.length,
                                        outputs,
                                        observer: outputObserver
                                    });
                                    outputs.push(output);
                                }
                            } else {
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
                    const errorMessage = typeof e?.message === 'string' ? <string>e.message : '' + e;
                    const errorOutput = this.config.createErrorOutput(errorMessage, this.getNextOutputId());
                    outputs.push(errorOutput);
                    reportOutputs();
                    reject(e);
                }
            });
        });
    }

    completion(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<CompletionsProduced> {
        let command: RequestCompletions = {
            code: code,
            linePosition: {
                line,
                character
            },
            targetKernelName: language
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

    async getDiagnostics(language: string, code: string, token?: string | undefined): Promise<Array<Diagnostic>> {
        const command: RequestDiagnostics = {
            code,
            targetKernelName: language
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
        token = token || this.getNextToken();
        id = id || Guid.create().toString();

        let disposable = this.subscribeToKernelTokenEvents(token, observer);
        await this.submitCommand(command, SubmitCodeType, token, id);
        return disposable;
    }

    requestValueInfos(kernelName: string): Promise<ValueInfosProduced> {
        const command: RequestValueInfos = {
            targetKernelName: kernelName,
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
        token = token || this.getNextToken();
        return this.submitCommand(command, CancelType, token, undefined);
    }

    dispose() {
        this.config.channel.dispose();
    }

    private submitCommandAndGetResult<TEvent extends KernelEvent>(command: KernelCommand, commandType: KernelCommandType, expectedEventType: KernelEventType, token: string | undefined): Promise<TEvent> {
        return new Promise<TEvent>(async (resolve, reject) => {
            let handled = false;
            token = token || this.getNextToken();
            const id = Guid.create().toString();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                if (eventEnvelope.command?.token === token) {
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
            await this.config.channel.submitCommand({ command, commandType, token, id });
        });
    }

    private submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string | undefined, id: string | undefined): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            let failureReported = false;
            token = token || this.getNextToken();
            id = id || Guid.create().toString();
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
            this.config.channel.submitCommand({ command, commandType, token, id }).catch(e => {
                // only report a failure if it's not a `CommandFailed` event from above (which has already called `reject()`)
                if (!failureReported) {
                    reject(e);
                }
            });
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

    private displayEventToCellOutput(disp: DisplayEvent, stream?: 'stdout' | 'stderr'): vscodeLike.NotebookCellOutput {
        const encoder = new TextEncoder();
        let outputItems: Array<vscodeLike.NotebookCellOutputItem> = [];
        if (disp.formattedValues && disp.formattedValues.length > 0) {
            for (let formatted of disp.formattedValues) {
                let data = encoder.encode(formatted.value);
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

        const output = createOutput(outputItems, this.getNextOutputId());
        return output;
    }

    private getNextOutputId(): string {
        return (this.nextOutputId++).toString();
    }

    private getNextToken(): string {
        return (this.nextToken++).toString();
    }
}

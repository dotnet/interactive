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
    KernelTransport,
    NotebookDocument,
    NotebookParsed,
    NotebookParsedType,
    NotebookSerialized,
    NotebookSerializedType,
    ParseNotebook,
    ParseNotebookType,
    SerializeNotebook,
    SerializeNotebookType,
    RequestCompletions,
    RequestCompletionsType,
    RequestDiagnostics,
    RequestDiagnosticsType,
    RequestHoverText,
    RequestHoverTextType,
    RequestSignatureHelp,
    RequestSignatureHelpType,
    ReturnValueProducedType,
    SignatureHelpProduced,
    SignatureHelpProducedType,
    StandardErrorValueProducedType,
    StandardOutputValueProducedType,
    SubmissionType,
    SubmitCode,
    SubmitCodeType,
    CancelType,
    Cancel
} from './interfaces/contracts';
import { Eol } from './interfaces';
import { clearDebounce, createOutput } from './utilities';

import * as vscodeLike from './interfaces/vscode-like';

export interface ErrorOutputCreator {
    (message: string, outputId?: string): vscodeLike.NotebookCellOutput;
}

export interface InteractiveClientConfiguration {
    readonly transport: KernelTransport,
    readonly createErrorOutput: ErrorOutputCreator,
}

export class InteractiveClient {
    private nextOutputId: number = 1;
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEnvelopeObserver>> = new Map<string, Array<KernelEventEnvelopeObserver>>();
    private deferredOutput: Array<vscodeLike.NotebookCellOutput> = [];
    private valueIdMap: Map<string, { idx: number, outputs: Array<vscodeLike.NotebookCellOutput>, observer: { (outputs: Array<vscodeLike.NotebookCellOutput>): void } }> = new Map<string, { idx: number, outputs: Array<vscodeLike.NotebookCellOutput>, observer: { (outputs: Array<vscodeLike.NotebookCellOutput>): void } }>();

    constructor(readonly config: InteractiveClientConfiguration) {
        config.transport.subscribeToKernelEvents(eventEnvelope => this.eventListener(eventEnvelope));
    }

    public tryGetProperty<T>(propertyName: string): T | null {
        try {
            return <T>((<any>this.config.transport)[propertyName]);
        }
        catch {
            return null;
        }
    }
    async parseNotebook(fileName: string, rawData: Uint8Array, token?: string | undefined): Promise<NotebookDocument> {
        const command: ParseNotebook = {
            fileName,
            rawData,
            targetKernelName: '.NET' // this command MUST be handled by the composite kernel
        };
        const notebookParsed = await this.submitCommandAndGetResult<NotebookParsed>(command, ParseNotebookType, NotebookParsedType, token);
        return notebookParsed.notebook;
    }

    async serializeNotebook(fileName: string, notebook: NotebookDocument, eol: Eol, token?: string | undefined): Promise<Uint8Array> {
        const command: SerializeNotebook = {
            fileName,
            notebook,
            newLine: eol,
            targetKernelName: '.NET' // this command MUST be handled by the composite kernel
        };
        const serializedNotebook = await this.submitCommandAndGetResult<NotebookSerialized>(command, SerializeNotebookType, NotebookSerializedType, token);
        return serializedNotebook.rawData;
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

            return this.submitCode(source, language, eventEnvelope => {
                if (this.deferredOutput.length > 0) {
                    outputs.push(...this.deferredOutput);
                    this.deferredOutput = [];
                }

                switch (eventEnvelope.eventType) {
                    // if kernel languages were added, handle those events here
                    case CommandSucceededType:
                        resolve();
                        break;
                    case CommandFailedType:
                        {
                            const err = <CommandFailed>eventEnvelope.event;
                            const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                            outputs.push(errorOutput);
                            reportOutputs();
                            failureReported = true;
                            reject(err);
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
                            let output = this.displayEventToCellOutput(disp);
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
            }, configuration?.token).catch(e => {
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

    async submitCode(code: string, language: string, observer: KernelEventEnvelopeObserver, token?: string | undefined): Promise<DisposableSubscription> {
        let command: SubmitCode = {
            code: code,
            submissionType: SubmissionType.Run,
            targetKernelName: language
        };
        token = token || this.getNextToken();
        let disposable = this.subscribeToKernelTokenEvents(token, observer);
        await this.submitCommand(command, SubmitCodeType, token);
        return disposable;
    }

    cancel(token?: string | undefined): Promise<void> {
        let command: Cancel = {};
        token = token || this.getNextToken();
        return this.submitCommand(command, CancelType, token);
    }

    dispose() {
        this.config.transport.dispose();
    }

    private submitCommandAndGetResult<TEvent extends KernelEvent>(command: KernelCommand, commandType: KernelCommandType, expectedEventType: KernelEventType, token: string | undefined): Promise<TEvent> {
        return new Promise<TEvent>(async (resolve, reject) => {
            let handled = false;
            token = token || this.getNextToken();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
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
            });
            await this.config.transport.submitCommand(command, commandType, token);
        });
    }

    private submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string | undefined): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            token = token || this.getNextToken();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case CommandFailedType:
                        let err = <CommandFailed>eventEnvelope.event;
                        disposable.dispose();
                        reject(err);
                        break;
                    case CommandSucceededType:
                        disposable.dispose();
                        resolve();
                        break;
                    default:
                        break;
                }
            });
            await this.config.transport.submitCommand(command, commandType, token);
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
            let listeners = this.tokenEventObservers.get(token);
            if (listeners) {
                for (let listener of listeners) {
                    listener(eventEnvelope);
                }
            } else if (token.startsWith("deferredCommand::")) {
                switch (eventEnvelope.eventType) {
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        let disp = <DisplayEvent>eventEnvelope.event;
                        let output = this.displayEventToCellOutput(disp);
                        this.deferredOutput.push(output);
                        break;
                }
            }
        }
    }

    private displayEventToCellOutput(disp: DisplayEvent): vscodeLike.NotebookCellOutput {
        const encoder = new TextEncoder();
        let outputItems: Array<vscodeLike.NotebookCellOutputItem> = [];
        if (disp.formattedValues && disp.formattedValues.length > 0) {
            for (let formatted of disp.formattedValues) {
                let data = encoder.encode(formatted.value);
                outputItems.push({
                    mime: formatted.mimeType,
                    data,
                });
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

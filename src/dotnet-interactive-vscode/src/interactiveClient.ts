// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandFailed,
    CommandFailedType,
    CommandHandledType,
    CompletionRequestCompleted,
    CompletionRequestCompletedType,
    DisplayEventBase,
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
    RequestCompletion,
    RequestCompletionType,
    RequestHoverText,
    RequestHoverTextType,
    ReturnValueProducedType,
    StandardErrorValueProducedType,
    StandardOutputValueProducedType,
    SubmissionType,
    SubmitCode,
    SubmitCodeType,
} from './contracts';
import { CellOutput, CellErrorOutput, CellOutputKind, CellStreamOutput, CellDisplayOutput } from './interfaces/vscode';

export class InteractiveClient {
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEnvelopeObserver>> = new Map<string, Array<KernelEventEnvelopeObserver>>();
    private deferredOutput: Array<CellOutput> = [];
    private valueIdMap: Map<string, { idx: number, outputs: Array<CellOutput>, observer: { (outputs: Array<CellOutput>): void }}> = new Map<string, { idx: number, outputs: Array<CellOutput>, observer: { (outputs: Array<CellOutput>): void }}>();

    constructor(readonly kernelTransport: KernelTransport) {
        kernelTransport.subscribeToKernelEvents(eventEnvelope => this.eventListener(eventEnvelope));
    }

    async execute(source: string, language: string, observer: { (outputs: Array<CellOutput>): void }, token?: string | undefined): Promise<void> {
        return new Promise(async (resolve, reject) => {
            let outputs: Array<CellOutput> = [];

            let reportOutputs = () => {
                observer(outputs);
            };

            let disposable = await this.submitCode(source, language, eventEnvelope => {
                if (this.deferredOutput.length > 0) {
                    outputs.push(...this.deferredOutput);
                    this.deferredOutput = [];
                }
    
                switch (eventEnvelope.eventType) {
                    case CommandHandledType:
                        resolve();
                        break;
                    case CommandFailedType:
                        {
                            let err = <CommandFailed>eventEnvelope.event;
                            let output: CellErrorOutput = {
                                outputKind: CellOutputKind.Error,
                                ename: 'Error',
                                evalue: err.message,
                                traceback: [],
                            };
                            outputs.push(output);
                            reportOutputs();
                            disposable.dispose(); // is this correct?
                            reject(err);
                        }
                        break;
                    case StandardErrorValueProducedType:
                    case StandardOutputValueProducedType:
                        {
                            let disp = <DisplayEventBase>eventEnvelope.event;
                            let output: CellStreamOutput = {
                                outputKind: CellOutputKind.Text,
                                text: disp.value.toString(),
                            };
                            outputs.push(output);
                            reportOutputs();
                        }
                        break;
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        {
                            let disp = <DisplayEventBase>eventEnvelope.event;
                            let output = displayEventToCellOutput(disp);

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
                                        observer
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
            }, token);
        });
    }

    completion(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<CompletionRequestCompleted> {
        let command: RequestCompletion = {
            code: code,
            position: {
                line,
                character
            },
            targetKernelName: language
        };
        return this.submitCommandAndGetResult<CompletionRequestCompleted>(command, RequestCompletionType, CompletionRequestCompletedType, token);
    }

    hover(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<HoverTextProduced> {
        let command: RequestHoverText = {
            code: code,
            position: {
                line: line,
                character: character,
            },
            targetKernelName: language
        };
        return this.submitCommandAndGetResult<HoverTextProduced>(command, RequestHoverTextType, HoverTextProducedType, token);
    }

    async submitCode(code: string, language: string, observer: KernelEventEnvelopeObserver, token?: string | undefined): Promise<DisposableSubscription> {
        let command: SubmitCode = {
            code: code,
            submissionType: SubmissionType.Run,
            targetKernelName: language
        };
        token = token || this.getNextToken();
        let disposable = this.subscribeToKernelTokenEvents(token, observer);
        await this.kernelTransport.submitCommand(command, SubmitCodeType, token);
        return disposable;
    }

    dispose() {
        this.kernelTransport.dispose();
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
                    case CommandHandledType:
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
            await this.kernelTransport.submitCommand(command, commandType, token);
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
            } else {
                switch (eventEnvelope.eventType) {
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        let disp = <DisplayEventBase>eventEnvelope.event;
                        let output = displayEventToCellOutput(disp);
                        this.deferredOutput.push(output);
                        break;
                }
            }
        }
    }

    private getNextToken(): string {
        return (this.nextToken++).toString();
    }
}

export function displayEventToCellOutput(disp: DisplayEventBase): CellDisplayOutput {
    let data: { [key: string]: any; } = {};
    if (disp.formattedValues && disp.formattedValues.length > 0) {
        for (let formatted of disp.formattedValues) {
            let value: any = formatted.mimeType === 'application/json'
                ? JSON.parse(formatted.value)
                : formatted.value;
            data[formatted.mimeType] = value;
        }
    } else if (disp.value) {
        // no formatted values returned, this is the best we can do
        data['text/plain'] = disp.value.toString();
    }

    let output: CellDisplayOutput = {
        outputKind: CellOutputKind.Rich,
        data: data,
    };

    return output;
}

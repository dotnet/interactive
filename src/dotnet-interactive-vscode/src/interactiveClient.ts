import {
    ChangeWorkingDirectory,
    ChangeWorkingDirectoryType,
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
    WorkingDirectoryChanged,
    WorkingDirectoryChangedType,
} from './contracts';
import { CellOutput, CellErrorOutput, CellOutputKind, CellStreamOutput, CellDisplayOutput } from './interfaces/vscode';
import { editorLanguagesToKernelNames } from './interactiveNotebook';

export class InteractiveClient {
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEnvelopeObserver>> = new Map<string, Array<KernelEventEnvelopeObserver>>();
    private deferredCommandEventListener: KernelEventEnvelopeObserver;

    constructor(readonly kernelTransport: KernelTransport) {
        kernelTransport.subscribeToKernelEvents(eventEnvelope => this.eventListener(eventEnvelope));
        this.deferredCommandEventListener = (_) => {

        };
    }

    public setDeferredCommandEventsListener(listener: KernelEventEnvelopeObserver) {
        this.deferredCommandEventListener = listener;
    }

    async execute(source: string, language: string, observer: { (outputs: Array<CellOutput>): void }, token?: string | undefined): Promise<void> {
        return new Promise(async (resolve, reject) => {
            let outputs: Array<CellOutput> = [];
            let valueIdToIndex: Map<string, number> = new Map<string, number>();
            let mappedLanguage = editorLanguagesToKernelNames[language];
            if (!mappedLanguage) {
                mappedLanguage = language;
            }
            let disposable = await this.submitCode(source, mappedLanguage, eventEnvelope => {
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
                            observer(outputs);
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
                            observer(outputs);
                        }
                        break;
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        {
                            let disp = <DisplayEventBase>eventEnvelope.event;
                            let output = displayEventToCellOutput(disp);

                            if (disp.valueId) {
                                let idx = valueIdToIndex.get(disp.valueId);
                                if (idx !== undefined) {
                                    // udpate existing value
                                    outputs[idx] = output;
                                } else {
                                    // add new tracked value
                                    valueIdToIndex.set(disp.valueId, outputs.length);
                                    outputs.push(output);
                                }
                            } else {
                                // raw value, just push it
                                outputs.push(output);
                            }

                            observer(outputs);
                        }
                        break;
                }
            }, token);
        });
    }

    changeWorkingDirectory(workingDirectory: string, token?: string | undefined): Promise<WorkingDirectoryChanged> {
        let command: ChangeWorkingDirectory = {
            workingDirectory
        };
        return this.submitCommandAndGetResult<WorkingDirectoryChanged>(command, ChangeWorkingDirectoryType, WorkingDirectoryChangedType, token);
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
                this.deferredCommandEventListener(eventEnvelope);
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
